# Claude Code 多语言 LSP 完全指南

## 在 Windows 上让 C# / Java / Python LSP 开箱即用

---

Claude Code 内置了 LSP 支持，但官方文档只给了最基础的命令配置。在 Windows 上实际落地时，你会撞上一系列坑：Bun 的进程 spawn 限制、.NET LSP 的通信协议、JDK 多版本隔离……这篇文章把我们的踩坑经历和最终方案完整分享出来，脚本全部开源，复制粘贴即可用。

**最终效果：** TypeScript、Python、Java、C# 四种语言的代码导航、引用查找、符号大纲全部正常工作——不需要改系统环境变量，不需要换 JDK 版本，不需要为每个项目手动配置 LSP 路径。

---

## 一、背景：Claude Code LSP 的运作方式

Claude Code 通过 marketplace.json 注册 LSP 服务器：

```json
{
  "lspServers": {
    "csharp-ls": {
      "command": "cmd",
      "args": ["/c", "csharp-ls"],
      "extensionToLanguage": { ".cs": "csharp" },
      "startupTimeout": 60000
    }
  }
}
```

当你在 Claude Code 中打开一个 `.cs` 文件，Claude Code 会：
1. 用 Bun 执行 `command` + `args`
2. 通过 stdin/stdout 与 LSP 服务器通信（Content-Length 分帧的 JSON-RPC）
3. 第一个发送的是 `initialize` 请求，包含工作区 `rootUri`

这就是全部机制。简单，但足够通用。

---

## 二、核心挑战：Bun 的 uv_spawn 与 Windows

### 2.1 Bun 只能直接 spawn .exe 文件

Bun 内部使用 libuv 的 `uv_spawn`，在 Windows 上底层调用 `CreateProcess`。这意味着 `command` 必须是 `.exe`——不能是 `.cmd`、`.bat`、`.sh`。

官方 TypeScript LSP 的解决方式是 `"command": "cmd"` + `"args": ["/c", "typescript-language-server", "--stdio"]`，用 `cmd /c` 做中转。

### 2.2 .NET 可执行文件导致 Bun 崩溃

我们尝试过 `csharp-ls.exe`（.NET 全局工具）和 `OmniSharp.exe`（自包含 .NET 应用），Bun 直接 spawn 时都会触发 `MozartBreathCore.dll` 段错误。这是 Bun 的 libuv 与 .NET PE 文件之间的兼容性 bug。

### 2.3 cmd /c 破坏 .NET LSP 的 stdio 通信

用 `cmd /c` 中转，jdtls（Java LSP）可以正常工作——因为链路是 `Bun → cmd → bash → py → java`，java.exe 不是 .NET 进程。

但同样的模式用在 OmniSharp 上就卡住：`Bun → cmd → .NET` 这条链路上 stdin/stdout 的 pipe 通信被破坏了，OmniSharp 收不到初始化请求，或者响应传不回来。

### 解决：Node.js 代理模式

最终的通用解法：用 `node` 作为中间代理。Bun 能正常 spawn `node.exe`（它是原生 exe），Node.js 再通过 `child_process.spawn` 启动 .NET LSP 进程，管道通信完全正常。

```
Bun → node.exe → omnisharp-proxy.js → OmniSharp.exe (.NET)
         ✅              ✅                   ✅
```

---

## 三、各语言方案详解

### TypeScript

最简单，Claude Code 官方支持：

```json
{
  "command": "cmd",
  "args": ["/c", "typescript-language-server", "--stdio"],
  "extensionToLanguage": { ".ts": "typescript", ".tsx": "typescriptreact" }
}
```

### Python

Pyright 是原生 exe，直接把路径写进配置就行：

```json
{
  "command": "C:/Users/<you>/.local/bin/pyright-langserver.exe",
  "args": ["--stdio"],
  "extensionToLanguage": { ".py": "python" }
}
```

### Java — jdtls

Java LSP 使用 Eclipse JDT Language Server。两个需求：
1. 系统默认 Java 8（项目需要），LSP 用 Java 21
2. jdtls 没有 exe，入口是一个 Python 脚本

**方案：** 用 bash 脚本指定 `--java-executable`，通过 `cmd /c` 调用。链路：

```
Bun → cmd /c → jdtls (bash) → py → jdtls.py → java (JDK21)
```

**`jdtls` bash 脚本：**

```bash
#!/bin/sh
JDTLS_HOME="D:/Program Files/Java-ls/jdt-language-server-1.59.0-202605111959"
JAVA21="D:/Program Files/Java/jdk-21.0.11+10/bin/java.exe"
exec py "$JDTLS_HOME/bin/jdtls" --java-executable "$JAVA21" "$@"
```

**marketplace.json 配置：**

```json
{
  "command": "cmd",
  "args": ["/c", "jdtls"],
  "extensionToLanguage": { ".java": "java" },
  "startupTimeout": 120000
}
```

### C# — csharp-ls（推荐，无需代理）

csharp-ls 是 .NET 社区的轻量级 LSP 服务器，基于 Roslyn API 但不需要完整 MSBuild 工具链。关键优势：**可以直接通过 `cmd /c` 运行，不需要 Node.js 代理。**

```json
{
  "csharp-ls": {
    "command": "cmd",
    "args": ["/c", "csharp-ls"],
    "extensionToLanguage": { ".cs": "csharp" },
    "startupTimeout": 60000
  }
}
```

**前置条件：** `dotnet restore` 必须先成功。NuGet 包没恢复的话，csharp-ls 找不到依赖的类型定义，符号分析会返回空。

**LSP 功能验证（v0.20.0）：**

| 功能 | 状态 | 验证内容 |
|------|------|----------|
| `documentSymbol` | ✅ | 正确解析类、方法、属性、字段、接口 |
| `hover` | ✅ | 方法签名 + XML 文档注释 |
| `goToDefinition` | ✅ | 跨文件跳转 |
| `findReferences` | ✅ | 跨文件引用查找 |
| `goToImplementation` | ✅ | 接口 → 实现类 |
| `workspaceSymbol` | ✅ | 工作区符号搜索 |
| `callHierarchy` | ❌ | 不支持 |

**调用链路探索验证：** 成功追踪 `Calculator.Add` 的完整调用链——`Main → TestBasicOperations → Add → CalculationRecord / _history / _logger.Log`，以及 `Main → TestBatchCalculation → BatchCalculate → Add`。

**已知限制：**
- 多 target framework 项目可能只分析其中一个 target
- 对 source generators 支持有限
- 不支持 `callHierarchy`（incoming/outgoing calls）

### C# — OmniSharp（需要代理）

如果你的项目结构复杂（多 target framework、自定义 MSBuild 属性），csharp-ls 分析不准时可以考虑 OmniSharp。但 OmniSharp 需要 Node.js 代理来解决以下问题：

**代理需要解决的三个问题：**

#### 问题 1：Claude Code 不支持 server→client 请求

OmniSharp 初始化时会发三种 server→client 请求：

| 请求 | 用途 | Claude Code 支持？ |
|------|------|-------------------|
| `client/registerCapability` | 注册动态能力 | 不支持 |
| `workspace/configuration` | 请求配置项 | 不支持 |
| `window/workDoneProgress/create` | 创建进度条 | 不支持 |

代理直接伪造响应：

```javascript
const fakeResponses = {
    'client/registerCapability': null,
    'workspace/configuration': [],
    'window/workDoneProgress/create': null,
};

// 拦截逻辑
if (parsed.method && parsed.id && fakeResponses.hasOwnProperty(parsed.method)) {
    proc.stdin.write(buildLspMessage({
        jsonrpc: '2.0',
        id: parsed.id,
        result: fakeResponses[parsed.method],
    }));
}
```

#### 问题 2：工作区路径不一定是 C# 项目

Claude Code 会在 `E:/code/llm-gateway`（一个 TypeScript 项目）里打开 `.cs` 文件。`rootUri` 指向 TS 项目，但如果工作区子目录里有 C# 项目（如 `lsp-test/csharp/`），代理需要自动发现。

**解决方案：** 收到 `initialize` 请求后，从 `rootUri` 提取工作区路径，递归搜索 `.csproj` 文件：

```javascript
function findCsproj(dir, depth) {
    if (depth <= 0) return null;
    const items = fs.readdirSync(dir, { withFileTypes: true });
    // 本层优先
    for (const f of items)
        if (f.isFile() && f.name.endsWith('.csproj')) return dir;
    // 再递归子目录
    for (const f of items) {
        if (f.isDirectory()) {
            const found = findCsproj(path.join(dir, f.name), depth - 1);
            if (found) return found;
        }
    }
    return null;
}
```

搜索优先级：`.csproj`（4 层深度） → `.sln`（根目录兜底）

#### 问题 3：OmniSharp 必须延迟启动

因为需要 `rootUri` 才知道去哪里找项目，代理不能启动时就 spawn OmniSharp，必须等第一个 `initialize` 消息：

```javascript
if (parsed.method === 'initialize' && !proc) {
    const wsPath = uriToPath(parsed.params.rootUri);
    const solution = findSolution(wsPath);
    startOmniSharp(solution);  // 此时才 spawn
}
```

---

## 四、完整代理代码

完整代码 ~220 行，已逐行注释。开源在 [GitHub 仓库地址]。

核心架构：

```
Claude Code (Bun)         代理 (Node.js)          OmniSharp (.NET)
      │                       │                       │
      │── initialize ────────→│── 搜索 .csproj        │
      │   rootUri=E:/code     │   找到后 spawn ───────→│ -lsp -s <dir>
      │                       │── initialize(改写) ──→│
      │                       │←── logMessage ×70 ────│
      │                       │←── capabilities ──────│
      │←── capabilities ──────│                       │
      │── documentSymbol ────→│── documentSymbol ────→│
      │                       │←── symbols ───────────│
      │←── symbols ──────────│                       │
```

**关键设计决策：**
- LSP 消息手动分帧：TCP 流式传输，一次 `data` 可能收到半条或多条消息，必须用 Content-Length 头切分
- 使用 Node.js 而非 Python 代理：`child_process.spawn` 的 pipe 模式对 .NET 进程更友好
- C# 项目动态发现：不做"全局索引"，只在收到 `initialize` 时搜索当前工作区

---

## 五、安装与使用

### 1. 前提条件

- Windows 10/11
- Node.js（任何版本，仅 OmniSharp 代理需要）
- .NET SDK 6.0+（csharp-ls 需要）

### 2. 安装 csharp-ls（推荐）

```bash
dotnet tool install -g csharp-ls
```

前置条件：`dotnet restore` 必须先成功。

### 2b. 下载 OmniSharp（备选）

```bash
# 下载自包含版本（无需安装 .NET 6 runtime）
# https://github.com/OmniSharp/omnisharp-roslyn/releases
# 解压到任意目录，记住路径
```

### 3. 下载 jdtls（可选，Java 支持）

```bash
# https://download.eclipse.org/jdtls/milestones/
# 解压到任意目录
# 下载 JDK 21: https://adoptium.net/download/
```

### 4. 放置脚本文件

将以下文件放到 `C:\Users\<你>\.local\bin\`：

```
omnisharp-proxy.js   ← C# LSP 代理（核心）
jdtls                 ← Java LSP bash 启动器
jdtls.cmd             ← Java LSP cmd 启动器
```

修改脚本中的路径常量（OmniSharp、JDTLS、JDK21 的安装路径）。

### 5. 配置 marketplace.json

在 Claude Code 的 marketplace.json 中添加：

```json
{
  "lspServers": {
    "csharp-ls": {
      "command": "cmd",
      "args": ["/c", "csharp-ls"],
      "extensionToLanguage": { ".cs": "csharp" },
      "startupTimeout": 60000
    },
    "jdtls": {
      "command": "cmd",
      "args": ["/c", "jdtls"],
      "extensionToLanguage": { ".java": "java" },
      "startupTimeout": 120000
    }
  }
}
```

### 6. 重启 Claude Code，开始使用

打开任意 `.cs` / `.java` / `.py` / `.ts` 文件，LSP 自动生效。

---

## 六、踩坑记录

| 坑 | 现象 | 根因 | 解决 |
|----|------|------|------|
| Bat To Exe 转换 jdtls | Bun 崩溃 `MozartBreathCore` | Bat To Exe 生成的是 .NET PE 文件 | 用 `cmd /c` 替代 |
| `cmd /c` + OmniSharp | 进程启动但无响应 | `cmd → .NET` 破坏 stdio pipe | 用 Node.js 代理 |
| OmniSharp 路径错误 | `ArgumentException: path` | `.sln` 路径多嵌套了一层目录 | 核实路径 |
| 搜索不到 C# 项目 | 符号返回空 | `findSolution` 只搜 1 层深 | 递归搜 4 层 |
| OmniSharp 没传 `-lsp` | 启动后走 HTTP 协议无响应 | Python 代理未传 `--lsp` | 补上 flag |
| `client/registerCapability` | 初始化卡住 | Claude Code 不响应此请求 | 代理伪造响应 |

---

## 七、小结

Claude Code 的 LSP 机制是开放的，但 Windows + Bun 的组合确实有不少坑。

**核心发现：** C# 的 csharp-ls 可以通过 `cmd /c` 直接运行，完全不需要 Node.js 代理——这与我们最初的认知不同。csharp-ls 虽然也是 .NET 应用，但它的 stdio 通信在 `cmd /c` 链路上没有被破坏，所有核心 LSP 功能（documentSymbol、hover、definition、references、implementation、workspaceSymbol）均正常工作。

OmniSharp 仍然需要 Node.js 代理来解决 `cmd /c` 破坏 stdio pipe 的问题，但如果你的项目不是特别复杂（单 target framework、标准 MSBuild 配置），csharp-ls 是更好的选择——零配置、零代理、直接可用。

所有的脚本都放在 [GitHub 链接]，欢迎 PR 和 issue。

---

*作者：Claude & Admin | 2026-05-27 · 更新于 2026-06-10（新增 csharp-ls 方案验证）*
