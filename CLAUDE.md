# ClaudeCode-LSP 项目指南

## 项目概述

让 Claude Code 在 Windows 上拥有 C# / Java / Python / TypeScript 的完整 LSP 支持。

## 核心架构

### C# LSP 方案
```
方案 A（推荐）：Claude Code (Bun) → cmd /c → csharp-ls.exe
方案 B（OmniSharp）：Claude Code (Bun) → node.exe → omnisharp-proxy.js → OmniSharp.exe
```

**为什么 csharp-ls 不需要代理？**
- csharp-ls 是 .NET 工具，可以通过 `cmd /c` 直接运行
- OmniSharp 需要代理是因为 cmd /c 中转会破坏 stdio pipe 通信

### csharp-ls LSP 功能验证（v0.20.0）

| 功能 | 状态 | 备注 |
|------|------|------|
| `documentSymbol` | ✅ | 正确解析类、方法、属性、字段、接口 |
| `hover` | ✅ | 显示方法签名 + XML 文档注释 |
| `goToDefinition` | ✅ | 跨文件跳转（类型、方法、属性） |
| `findReferences` | ✅ | 跨文件引用查找（接口实现、类实例化） |
| `goToImplementation` | ✅ | 接口 → 所有实现类 |
| `workspaceSymbol` | ✅ | 工作区级别符号搜索 |
| `callHierarchy` | ❌ | csharp-ls 不支持 |

**调用链路探索验证：** 成功追踪 `Calculator.Add` 的完整调用链——`Main → TestBasicOperations → Add → CalculationRecord / _history / _logger.Log`，以及 `Main → TestBatchCalculation → BatchCalculate → Add`。

**前置条件：** `dotnet restore` 必须先成功，否则符号分析返回空。

### 关键文件
- `omnisharp-proxy.js` — C# LSP 代理（仅 OmniSharp 需要）
- `jdtls` / `jdtls.cmd` — Java LSP 启动脚本

## 开发规范

### 配置文件位置
- 全局指令：`C:\Users\Admin\.claude\CLAUDE.md`
- 市场配置：`C:\Users\Admin\.claude\marketplace.json`
- 代理脚本：`C:\Users\Admin\.local\bin\`

### 调试日志
- csharp-ls: `%TEMP%\csharp-ls-debug.log`
- OmniSharp: `%TEMP%\omnisharp-debug.log`

## 系统要求

- Windows 10 / 11
- Node.js（任意版本）
- .NET SDK 6.0+（csharp-ls 需要）
- JDK 21（仅 Java）

## 常见问题

| 现象 | 根因 | 解决 |
|------|------|------|
| Bun 崩溃 `MozartBreathCore` | .NET PE 文件兼容性问题 | 使用 Node.js 代理 |
| 符号返回空 | 搜索深度不足 | 代理自动递归搜索 4 层 |
| 初始化卡住 | Claude Code 不响应 server→client 请求 | 代理伪造响应 |
