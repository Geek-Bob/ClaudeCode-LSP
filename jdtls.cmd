@echo off
REM =============================================================================
REM jdtls LSP 启动脚本（Windows 批处理版本）— Eclipse JDT Language Server (Java)
REM
REM 使用方式：
REM   1. 修改下方 JDTLS_HOME 和 JAVA21 变量，指向你的安装路径
REM   2. 或者设置环境变量 JDTLS_HOME / JAVA_HOME_21
REM   3. 确保 jdtls.cmd 在 PATH 中，Claude Code 通过 cmd /c 调用
REM
REM 环境变量优先级高于脚本内默认值。
REM =============================================================================

if not defined JDTLS_HOME set "JDTLS_HOME=D:\jdtls\jdt-language-server-latest"
if not defined JAVA21 (
    if defined JAVA_HOME_21 (
        set "JAVA21=%JAVA_HOME_21%\bin\java.exe"
    ) else (
        set "JAVA21=D:\jdk-21\bin\java.exe"
    )
)

py "%JDTLS_HOME%\bin\jdtls" --java-executable "%JAVA21%" %*
