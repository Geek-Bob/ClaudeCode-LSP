# ClaudeCode-LSP

让 Claude Code 在 Windows 上拥有 C# / Java / Python / TypeScript 的完整 LSP 支持——开箱即用。

## 这是什么？

[Claude Code](https://docs.anthropic.com/en/docs/claude-code) 通过 `marketplace.json` 注册外部 LSP 服务器来实现代码导航、符号搜索、引用查找等 IDE 级功能。但在 Windows 上，Bun 的进程管理（`uv_spawn` / `CreateProcess`）与 .NET PE 文件、Java 生态之间存在一系列兼容性问题。

本项目提供经过验证的脚本和配置模板，覆盖四种主流语言，让你不需要改系统环境变量、不需要切换 JDK 版本、不需要为每个项目手动配置 LSP 路径。

**最终效果：** `documentSymbol`、`goToDefinition`、`findReferences`、`hover` 全线可用。

| 语言 | LSP 服务器 | 方案 | 复杂度 |
|------|-----------|------|--------|
| TypeScript | typescript-language-server | 官方支持，`cmd /c` 中转 | 低 |
| Python | Pyright | 原生 exe，直接配置路径 | 低 |
| Java | Eclipse JDT LS | `cmd /c` + bash 脚本 + JDK 隔离 | 中 |
| C# | csharp-ls（推荐）/ OmniSharp | `cmd /c` 直连 / Node.js 代理 | 低 / 高 |

> 想了解完整的踩坑过程和技术原理？详见 [lsp-blog.md](./lsp-blog.md) —— 包含 Bun spawn 限制、cmd /c pipe 断裂、OmniSharp LSP 协议细节等深度分析。

## 快速开始

### 1. 安装 LSP 服务器

**TypeScript**
```bash
npm install -g typescript typescript-language-server
```

**Python**
```bash
pip install pyright
# 或者
npm install -g pyright
```

**Java**
1. 下载 [Eclipse JDT Language Server](https://download.eclipse.org/jdtls/milestones/)，解压到 `D:\jdtls\`
2. 安装 [JDK 21](https://adoptium.net/download/)（可与系统默认 JDK 版本共存）

**C#**（两种方案任选其一）

方案 A — csharp-ls（推荐，无需代理脚本）：
```bash
dotnet tool install -g csharp-ls
```
前置条件：`dotnet restore` 必须先成功（NuGet 包未恢复会导致符号分析返回空）。

方案 B — OmniSharp（自包含版本，需要代理脚本）：
1. 下载 [OmniSharp 自包含版本](https://github.com/OmniSharp/omnisharp-roslyn/releases)（`omnisharp-win-x64-net6.0.zip`）
2. 解压到 `D:\OmniSharp\`

### 2. 配置脚本

**C# — csharp-ls（推荐）或 OmniSharp**

csharp-ls 方案（推荐，无需代理脚本）：
```bash
dotnet tool install -g csharp-ls
```

OmniSharp 方案（需要代理脚本）：
1. 编辑 `omnisharp-proxy.js` 中的 `OMNISHARP_EXE` 常量，或设置环境变量：
   ```bash
   setx OMNISHARP_EXE "D:\OmniSharp\omnisharp-win-x64-net6.0\OmniSharp.exe"
   ```
2. 将 `omnisharp-proxy.js` 放入 `%USERPROFILE%\.local\bin\`

**Java — jdtls / jdtls.cmd**

编辑脚本中的 `JDTLS_HOME` 和 `JAVA21` 变量，或设置环境变量：

```bash
setx JDTLS_HOME "D:\jdtls\jdt-language-server-latest"
setx JAVA_HOME_21 "D:\jdk-21"
```

将两个文件放入 `%USERPROFILE%\.local\bin\` 并确保该目录在 PATH 中。

### 3. 配置 marketplace.json

打开 Claude Code 的 marketplace.json，添加 `lspServers` 段：

```json
{
  "lspServers": {
    "typescript-ls": {
      "command": "cmd",
      "args": ["/c", "typescript-language-server", "--stdio"],
      "extensionToLanguage": {
        ".ts": "typescript",
        ".tsx": "typescriptreact"
      }
    },
    "python-ls": {
      "command": "cmd",
      "args": ["/c", "pyright-langserver", "--stdio"],
      "extensionToLanguage": { ".py": "python" }
    },
    "jdtls": {
      "command": "cmd",
      "args": ["/c", "jdtls"],
      "extensionToLanguage": { ".java": "java" },
      "startupTimeout": 120000
    },
    "csharp-ls": {
      "command": "cmd",
      "args": ["/c", "csharp-ls"],
      "extensionToLanguage": { ".cs": "csharp" },
      "startupTimeout": 60000
    }
  }
}
```

> **提示：** 将 `%USERPROFILE%` 替换为实际路径（如 `C:\Users\<你的用户名>`），或直接使用绝对路径。

### 4. 重启 Claude Code

打开任意 `.cs` / `.java` / `.py` / `.ts` 文件，LSP 功能自动生效。

## csharp-ls LSP 功能验证

使用 Claude Code 内置 LSP 工具对 csharp-ls v0.20.0 进行了完整测试（测试项目：`lsp-test/csharp/`）：

| 功能 | 状态 | 验证内容 |
|------|------|----------|
| `documentSymbol` | ✅ | 3 个文件全部正确解析（类、方法、属性、字段、接口） |
| `hover` | ✅ | 显示方法签名 + XML 文档注释（如 `int Calculator.Add(int a, int b)`） |
| `goToDefinition` | ✅ | 跨文件跳转：类型定义、方法定义、构造函数 |
| `findReferences` | ✅ | 跨文件引用查找（接口实现、类实例化、方法调用） |
| `goToImplementation` | ✅ | 接口 → 所有实现类（`ILogger` → `ConsoleLogger` + `FileLogger`） |
| `workspaceSymbol` | ✅ | 工作区级别符号搜索（类名、方法名） |
| `callHierarchy` | ❌ | csharp-ls v0.20.0 不支持 `prepareCallHierarchy` |

**调用链路探索验证：**

成功追踪 `Calculator.Add` 的完整调用链：
```
Main → TestBasicOperations → Add → CalculationRecord / _history / _logger.Log
Main → TestBatchCalculation → BatchCalculate → Add → (同上)
```

**已知限制：**
- `dotnet restore` 必须先成功，否则符号分析返回空
- 不支持 `callHierarchy`（incoming/outgoing calls）
- 多 target framework 项目可能只分析其中一个 target

> csharp-ls 不需要代理脚本，直接通过 `cmd /c csharp-ls` 即可运行。OmniSharp 仍需要 Node.js 代理。

## 核心原理：OmniSharp 为什么要用 Node.js 代理？

### 问题链

1. **Bun 只能直接 spawn .exe 文件** — `uv_spawn` 在 Windows 上底层调用 `CreateProcess`，不能是 `.cmd`、`.bat`、`.sh`
2. **Bun 直接 spawn .NET exe 会崩溃** — 触发 `MozartBreathCore.dll` 段错误，这是 Bun 的 libuv 与 .NET PE 文件之间的兼容性 bug
3. **`cmd /c` 中转破坏 .NET LSP 的 stdio** — `Bun → cmd → .NET` 这条链路上 pipe 通信被截断

### 解法

用 `node.exe` 作为中间代理：

```
Bun → node.exe → omnisharp-proxy.js → OmniSharp.exe (.NET)
 ✅         ✅              ✅                   ✅
```

- Bun 能正常 spawn `node.exe`（它是原生 exe）
- Node.js 通过 `child_process.spawn` 启动 .NET 进程，管道通信完全正常
- 代理在中间拦截 Claude Code 不支持的 `client/registerCapability`、`workspace/configuration`、`window/workDoneProgress/create` 请求并伪造响应

### 代理的工作流程

```
Claude Code (Bun)         代理 (Node.js)          OmniSharp (.NET)
      │                       │                       │
      │── initialize ────────→│── 搜索 .csproj/.sln   │
      │   rootUri=E:/code     │   找到后 spawn ───────→│ -lsp -s <dir>
      │                       │── initialize(改写) ──→│
      │                       │←── capabilities ──────│
      │←── capabilities ──────│                       │
      │── documentSymbol ────→│── documentSymbol ────→│
      │                       │←── symbols ───────────│
      │←── symbols ──────────│                       │
```

### C# 项目自动发现

代理收到 `initialize` 请求后，从 `rootUri` 提取工作区路径，递归搜索 C# 项目文件：

1. **优先：** 递归搜索 `.csproj`（最大深度 4 层：root + 3 级子目录）
2. **兜底：** 根目录查找 `.sln` 文件
3. **无项目：** OmniSharp 以 CWD 运行，闲置待命

这意味着你可以在 TypeScript 项目的子目录里放一个 C# 项目，代理会自动找到它。

### Java JDK 版本隔离

`jdtls` 脚本通过 `--java-executable` 参数指定 JDK 21，系统默认可以是 Java 8，互不干扰。

链路：`Bun → cmd /c → jdtls (bash) → py → jdtls.py → java (JDK 21)`

## 文件说明

```
├── omnisharp-proxy.js   # C# LSP 代理（仅 OmniSharp 需要）
├── jdtls                # Java LSP bash 启动脚本
├── jdtls.cmd            # Java LSP cmd 启动脚本
└── README.md
```

## 踩坑记录

| 现象 | 根因 | 解决 |
|------|------|------|
| Bun 崩溃 `MozartBreathCore` | Bat To Exe 生成的是 .NET PE 文件 | 用 `cmd /c` + bash 脚本替代 |
| OmniSharp 启动但无响应 | `cmd → .NET` 破坏 stdio pipe | 用 Node.js 代理 |
| 符号返回空 | 搜索深度只有 1 层，找不到子目录项目 | 递归搜 4 层 |
| 代理模式下启动走 HTTP | 未传 `-lsp` 参数 | 确保 `-lsp` flag |
| 初始化卡住 | Claude Code 不响应 `client/registerCapability` | 代理伪造响应 |
| OmniSharp 路径错误 | `.sln` 路径多嵌套了一层目录 | 核实 `-s` 参数路径 |

## 调试

**C# LSP：** 查看 `%TEMP%\omnisharp-debug.log`，包含所有 LSP 消息的收发记录和代理决策日志。

**Java LSP：** jdtls 的日志由 JDTLS 自身输出到 stderr，可在 Claude Code 的开发者工具中查看。

## 系统要求

- Windows 10 / 11
- [Node.js](https://nodejs.org/)（任意版本，仅用于运行代理脚本）
- C# LSP（二选一）：
  - [csharp-ls](https://github.com/razzmatazz/csharp-language-server)（推荐，需要 .NET SDK 6.0+）
  - [OmniSharp](https://github.com/OmniSharp/omnisharp-roslyn/releases)（自包含版本，无需 .NET runtime）
- [JDK 21](https://adoptium.net/download/)（仅 Java，可与系统默认 JDK 共存）

## 许可证

MIT

## 致谢

- [csharp-ls](https://github.com/razzmatazz/csharp-language-server) — C# LSP 服务器（推荐）
- [OmniSharp](https://github.com/OmniSharp/omnisharp-roslyn) — C# LSP 服务器
- [Eclipse JDT LS](https://github.com/eclipse-jdtls/eclipse.jdt.ls) — Java LSP 服务器
- [Pyright](https://github.com/microsoft/pyright) — Python 类型检查器 / LSP
- Claude Code 团队 — 开放的 LSP 扩展机制
