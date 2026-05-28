/**
 * OmniSharp LSP 代理 — 桥接 Claude Code (Bun) 和 OmniSharp.exe (.NET)
 *
 * 解决的问题：
 *   1. Bun 在 Windows 上无法直接 spawn .NET PE 文件（MozartBreathCore.dll 崩溃）
 *   2. cmd /c 中转会破坏 .NET 进程的 stdio pipe 通信
 *   3. Claude Code 不支持 client/registerCapability 等 server→client 请求
 *   4. 工作区可能没有 .sln/.csproj，需要自动发现 C# 项目子目录
 *
 * 架构链路：Claude Code (Bun) → node.exe → OmniSharp.exe (.NET)
 *
 * 使用方式：
 *   直接修改下方 CONFIG 区域的 OMNISHARP_EXE 路径，或在系统环境变量中设置 OMNISHARP_EXE。
 *   环境变量优先级高于配置文件中的默认值。
 */

const { spawn } = require('child_process');
const fs = require('fs');
const path = require('path');

// =============================================================================
// CONFIG — 修改此处的路径指向你的 OmniSharp 安装目录
// 也可通过环境变量 OMNISHARP_EXE 覆盖（优先级更高）
// =============================================================================
const OMNISHARP_EXE = process.env.OMNISHARP_EXE
    || 'D:/OmniSharp/omnisharp-win-x64-net6.0/OmniSharp.exe';

// =============================================================================
// 调试日志，写入 %TEMP%/omnisharp-debug.log
// =============================================================================
const LOG_PATH = process.env.TEMP
    ? process.env.TEMP + '\\omnisharp-debug.log'
    : './omnisharp-debug.log';
fs.appendFileSync(LOG_PATH, '\n=== OmniSharp Proxy started ' + new Date().toISOString() + ' ===\n');

// =============================================================================
// Claude Code 不支持的 LSP server→client 请求，代理直接返回假成功
// =============================================================================
const FAKE_RESPONSES = {
    'client/registerCapability': null,
    'workspace/configuration': [],
    'window/workDoneProgress/create': null,
};

// =============================================================================
// 状态变量
// =============================================================================
let proc = null;            // OmniSharp 进程句柄（initialize 后才 spawn）
let sBuf = Buffer.alloc(0); // Server→Client 方向的接收缓冲
let cBuf = Buffer.alloc(0); // Client→Server 方向的接收缓冲
let msgCount = 0;           // S→C 方向消息计数器（仅用于日志）

// =============================================================================
// 工具函数
// =============================================================================

/** 将 JS 对象编码为 LSP 协议格式：Content-Length 头 + CRLF + JSON 体 */
function buildLspMessage(obj) {
    const body = JSON.stringify(obj);
    return 'Content-Length: ' + Buffer.byteLength(body) + '\r\n\r\n' + body;
}

/**
 * 从缓冲区中读取一条完整的 LSP 消息。
 * @returns {{ json: string, consumed: number } | null}
 */
function readMessage(buf) {
    const idx = buf.indexOf('\r\n\r\n');
    if (idx === -1) return null;
    const header = buf.slice(0, idx).toString();
    const m = header.match(/Content-Length: (\d+)/i);
    if (!m) return null;
    const len = parseInt(m[1], 10);
    const start = idx + 4;
    if (buf.length < start + len) return null;
    return { json: buf.slice(start, start + len).toString(), consumed: start + len };
}

/** file:///D:/some/path → D:\some\path */
function uriToPath(uri) {
    let p = decodeURIComponent(uri.replace(/^file:\/\/\//, ''));
    return p.replace(/\//g, path.sep);
}

// =============================================================================
// C# 项目发现
// =============================================================================

/**
 * 在工作区目录树中递归搜索 .csproj 或 .sln 文件。
 *
 * 优先级：
 *   1. 递归搜索 .csproj（最大深度 4 层：root + 3 级子目录）
 *   2. 根目录查找 .sln（兜底）
 *   3. 返回 null（OmniSharp 以 CWD 运行，闲置待命）
 */
function findSolution(workspacePath) {
    function findCsproj(dir, depth) {
        if (depth <= 0) return null;
        try {
            const items = fs.readdirSync(dir, { withFileTypes: true });
            for (const f of items) {
                if (f.isFile() && f.name.endsWith('.csproj')) return dir;
            }
            for (const f of items) {
                if (f.isDirectory()) {
                    const found = findCsproj(path.join(dir, f.name), depth - 1);
                    if (found) return found;
                }
            }
        } catch (_) { /* 权限不足等异常静默跳过 */ }
        return null;
    }

    // 1. 优先：递归搜索 .csproj
    try {
        const csprojDir = findCsproj(workspacePath, 4);
        if (csprojDir) return { type: 'csproj', path: csprojDir, dir: workspacePath };
    } catch (_) { /* ignore */ }

    // 2. 兜底：根目录查找 .sln
    try {
        const entries = fs.readdirSync(workspacePath, { withFileTypes: true });
        for (const e of entries) {
            if (e.isFile() && e.name.endsWith('.sln')) {
                return { type: 'sln', path: path.join(workspacePath, e.name), dir: workspacePath };
            }
        }
    } catch (_) { /* ignore */ }

    return null;
}

// =============================================================================
// OmniSharp 生命周期
// =============================================================================

/** 启动 OmniSharp.exe，绑定 stdout/stderr 的 LSP 消息转发 */
function startOmniSharp(solution) {
    const args = ['-lsp'];
    const opts = { stdio: ['pipe', 'pipe', 'pipe'] };

    if (solution) {
        args.push('-s', solution.path);
        opts.cwd = solution.dir;
        fs.appendFileSync(LOG_PATH,
            'OmniSharp -s ' + solution.path + ' (type=' + solution.type + ')\n');
    } else {
        fs.appendFileSync(LOG_PATH, 'OmniSharp -lsp (no solution found, using CWD)\n');
    }

    proc = spawn(OMNISHARP_EXE, args, opts);

    // S→C 方向：OmniSharp → 代理 → Claude Code
    proc.stdout.on('data', function (chunk) {
        sBuf = Buffer.concat([sBuf, chunk]);
        var msg;
        while ((msg = readMessage(sBuf))) {
            msgCount++;
            try {
                var parsed = JSON.parse(msg.json);
                var method = parsed.method || ('(response id=' + (parsed.id || '?') + ')');
                fs.appendFileSync(LOG_PATH, '[' + msgCount + '] S→C: ' + method + '\n');

                // 拦截 Claude Code 不支持的 server→client 请求
                if (parsed.method && parsed.id && FAKE_RESPONSES.hasOwnProperty(parsed.method)) {
                    fs.appendFileSync(LOG_PATH, '[' + msgCount + '] INTERCEPTED: ' + method + '\n');
                    proc.stdin.write(buildLspMessage({
                        jsonrpc: '2.0',
                        id: parsed.id,
                        result: FAKE_RESPONSES[parsed.method],
                    }));
                } else {
                    process.stdout.write(buildLspMessage(parsed));
                }
            } catch (e) {
                fs.appendFileSync(LOG_PATH,
                    '[' + msgCount + '] PARSE ERROR: ' + e.message + '\n');
                process.stdout.write(msg.raw || msg.json || '');
            }
            sBuf = sBuf.slice(msg.consumed);
        }
    });

    proc.stderr.on('data', function (chunk) {
        fs.appendFileSync(LOG_PATH, 'STDERR: ' + chunk.toString() + '\n');
    });

    proc.on('exit', function (code) {
        fs.appendFileSync(LOG_PATH, '=== OmniSharp exit: ' + code + ' ===\n');
        process.exit(code || 0);
    });

    proc.on('error', function (err) {
        fs.appendFileSync(LOG_PATH, '=== OmniSharp spawn error: ' + err.message + ' ===\n');
        process.exit(1);
    });
}

/** 将一条 LSP 消息转发给 OmniSharp（如果已启动） */
function forwardToServer(parsed) {
    if (proc) {
        proc.stdin.write(buildLspMessage(parsed));
    }
}

// =============================================================================
// C→S 方向：Claude Code → 代理 → OmniSharp
// =============================================================================
process.stdin.on('data', function (chunk) {
    cBuf = Buffer.concat([cBuf, chunk]);
    var msg;
    while ((msg = readMessage(cBuf))) {
        try {
            var parsed = JSON.parse(msg.json);
            var method = parsed.method || '(response)';
            fs.appendFileSync(LOG_PATH, 'C→S: ' + method + '\n');

            // 首个 initialize 请求：从 rootUri 提取工作区路径，搜索 C# 项目，启动 OmniSharp
            if (parsed.method === 'initialize' && !proc) {
                var wsPath = parsed.params?.rootUri
                    ? uriToPath(parsed.params.rootUri)
                    : (parsed.params?.rootPath || process.cwd());

                fs.appendFileSync(LOG_PATH, 'Workspace from rootUri: ' + wsPath + '\n');
                var solution = findSolution(wsPath);

                if (solution) {
                    var solUri = 'file:///' + solution.dir.replace(/\\/g, '/');
                    parsed.params.rootUri = solUri;
                    parsed.params.rootPath = solution.dir;
                    parsed.params.workspaceFolders = [{ uri: solUri, name: 'csharp' }];
                    fs.appendFileSync(LOG_PATH,
                        'C→S: [FIXED] workspace -> ' + solution.dir + '\n');
                }

                startOmniSharp(solution);
                forwardToServer(parsed);
            } else {
                forwardToServer(parsed);
            }
        } catch (e) {
            forwardToServer({ json: msg.json });
        }
        cBuf = cBuf.slice(msg.consumed);
    }
});

// Claude Code 关闭 stdin → 通知 OmniSharp 也关闭
process.stdin.on('end', function () {
    if (proc) proc.stdin.end();
});
