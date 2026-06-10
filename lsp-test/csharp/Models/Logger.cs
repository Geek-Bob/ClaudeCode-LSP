namespace LspTest.Models;

/// <summary>
/// 日志记录器接口
/// </summary>
public interface ILogger
{
    void Log(string message);
    void LogError(Exception ex);
    void LogWarning(string message);
}

/// <summary>
/// 控制台日志记录器实现
/// </summary>
public class ConsoleLogger : ILogger
{
    private readonly string _prefix;

    public ConsoleLogger(string prefix = "LspTest")
    {
        _prefix = prefix;
    }

    public void Log(string message)
    {
        Console.WriteLine($"[{_prefix}] {DateTime.Now:HH:mm:ss} INFO: {message}");
    }

    public void LogError(Exception ex)
    {
        Console.Error.WriteLine($"[{_prefix}] {DateTime.Now:HH:mm:ss} ERROR: {ex.Message}");
    }

    public void LogWarning(string message)
    {
        Console.WriteLine($"[{_prefix}] {DateTime.Now:HH:mm:ss} WARN: {message}");
    }
}

/// <summary>
/// 文件日志记录器实现（演示依赖注入）
/// </summary>
public class FileLogger : ILogger
{
    private readonly string _filePath;

    public FileLogger(string filePath)
    {
        _filePath = filePath;
    }

    public void Log(string message)
    {
        File.AppendAllText(_filePath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] INFO: {message}\n");
    }

    public void LogError(Exception ex)
    {
        File.AppendAllText(_filePath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ERROR: {ex.Message}\n{ex.StackTrace}\n");
    }

    public void LogWarning(string message)
    {
        File.AppendAllText(_filePath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] WARN: {message}\n");
    }
}
