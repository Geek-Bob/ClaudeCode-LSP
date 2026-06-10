namespace LspTest.Models;

/// <summary>
/// 计算器类 - 支持基本数学运算和历史记录
/// </summary>
public class Calculator
{
    private readonly List<CalculationRecord> _history = new();
    private readonly ILogger _logger;

    /// <summary>
    /// 初始化计算器
    /// </summary>
    /// <param name="logger">日志记录器</param>
    public Calculator(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger.Log("Calculator initialized");
    }

    /// <summary>
    /// 加法运算
    /// </summary>
    /// <param name="a">第一个操作数</param>
    /// <param name="b">第二个操作数</param>
    /// <returns>两数之和</returns>
    public int Add(int a, int b)
    {
        int result = a + b;
        var record = new CalculationRecord("Add", a, b, result);
        _history.Add(record);
        _logger.Log($"Add: {a} + {b} = {result}");
        return result;
    }

    /// <summary>
    /// 减法运算
    /// </summary>
    public int Subtract(int a, int b)
    {
        int result = a - b;
        var record = new CalculationRecord("Subtract", a, b, result);
        _history.Add(record);
        _logger.Log($"Subtract: {a} - {b} = {result}");
        return result;
    }

    /// <summary>
    /// 乘法运算
    /// </summary>
    public int Multiply(int a, int b)
    {
        int result = a * b;
        var record = new CalculationRecord("Multiply", a, b, result);
        _history.Add(record);
        _logger.Log($"Multiply: {a} * {b} = {result}");
        return result;
    }

    /// <summary>
    /// 除法运算
    /// </summary>
    /// <exception cref="DivideByZeroException">当除数为零时抛出</exception>
    public double Divide(int a, int b)
    {
        if (b == 0)
            throw new DivideByZeroException("Cannot divide by zero");

        double result = (double)a / b;
        var record = new CalculationRecord("Divide", a, b, result);
        _history.Add(record);
        _logger.Log($"Divide: {a} / {b} = {result}");
        return result;
    }

    /// <summary>
    /// 批量计算 - 演示方法调用链
    /// </summary>
    public CalculationResult BatchCalculate(IEnumerable<CalculationRequest> requests)
    {
        var results = new List<double>();
        var errors = new List<string>();

        foreach (var request in requests)
        {
            try
            {
                double result = request.Operation switch
                {
                    "Add" => Add(request.A, request.B),
                    "Subtract" => Subtract(request.A, request.B),
                    "Multiply" => Multiply(request.A, request.B),
                    "Divide" => Divide(request.A, request.B),
                    _ => throw new ArgumentException($"Unknown operation: {request.Operation}")
                };
                results.Add(result);
            }
            catch (Exception ex)
            {
                errors.Add($"{request.Operation}({request.A}, {request.B}): {ex.Message}");
                _logger.LogError(ex);
            }
        }

        return new CalculationResult
        {
            Results = results,
            Errors = errors,
            TotalCount = requests.Count(),
            SuccessCount = results.Count
        };
    }

    /// <summary>
    /// 获取计算历史
    /// </summary>
    public IReadOnlyList<CalculationRecord> GetHistory() => _history.AsReadOnly();

    /// <summary>
    /// 清空历史记录
    /// </summary>
    public void ClearHistory()
    {
        _history.Clear();
        _logger.Log("History cleared");
    }
}

/// <summary>
/// 计算记录
/// </summary>
public record CalculationRecord(
    string Operation,
    double Operand1,
    double Operand2,
    double Result,
    DateTime Timestamp = default)
{
    public DateTime Timestamp { get; init; } = Timestamp == default ? DateTime.UtcNow : Timestamp;

    public override string ToString() =>
        $"{Operation}: {Operand1} {GetOperator()} {Operand2} = {Result}";

    private string GetOperator() => Operation switch
    {
        "Add" => "+",
        "Subtract" => "-",
        "Multiply" => "*",
        "Divide" => "/",
        _ => "?"
    };
}

/// <summary>
/// 计算请求
/// </summary>
public class CalculationRequest
{
    public string Operation { get; set; } = string.Empty;
    public int A { get; set; }
    public int B { get; set; }
}

/// <summary>
/// 计算结果
/// </summary>
public class CalculationResult
{
    public List<double> Results { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public int TotalCount { get; set; }
    public int SuccessCount { get; set; }
    public bool HasErrors => Errors.Count > 0;
}
