using LspTest.Models;

namespace LspTest;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== C# LSP Test Program ===\n");

        // 创建依赖
        ILogger logger = new ConsoleLogger("Main");
        Calculator calculator = new(logger);

        // 测试基本运算
        TestBasicOperations(calculator);

        // 测试批量计算
        TestBatchCalculation(calculator);

        // 测试错误处理
        TestErrorHandling(calculator);

        // 显示历史记录
        DisplayHistory(calculator);

        Console.WriteLine("\n=== Test Complete ===");
    }

    /// <summary>
    /// 测试基本运算 - 这里测试 Add 调用链
    /// </summary>
    static void TestBasicOperations(Calculator calc)
    {
        Console.WriteLine("\n--- Basic Operations ---");

        // 测试 Add 方法
        int sum = calc.Add(10, 20);
        Console.WriteLine($"10 + 20 = {sum}");

        // 测试 Subtract 方法
        int diff = calc.Subtract(50, 30);
        Console.WriteLine($"50 - 30 = {diff}");

        // 测试 Multiply 方法
        int product = calc.Multiply(6, 7);
        Console.WriteLine($"6 * 7 = {product}");

        // 测试 Divide 方法
        double quotient = calc.Divide(100, 3);
        Console.WriteLine($"100 / 3 = {quotient:F2}");
    }

    /// <summary>
    /// 测试批量计算 - 演示复杂方法调用
    /// </summary>
    static void TestBatchCalculation(Calculator calc)
    {
        Console.WriteLine("\n--- Batch Calculation ---");

        var requests = new List<CalculationRequest>
        {
            new() { Operation = "Add", A = 1, B = 2 },
            new() { Operation = "Multiply", A = 3, B = 4 },
            new() { Operation = "Divide", A = 10, B = 0 },  // 故意的错误
            new() { Operation = "Subtract", A = 100, B = 50 },
            new() { Operation = "Invalid", A = 1, B = 1 }   // 无效操作
        };

        var result = calc.BatchCalculate(requests);

        Console.WriteLine($"Total: {result.TotalCount}, Success: {result.SuccessCount}");
        Console.WriteLine($"Results: [{string.Join(", ", result.Results)}]");

        if (result.HasErrors)
        {
            Console.WriteLine("Errors:");
            foreach (var error in result.Errors)
            {
                Console.WriteLine($"  - {error}");
            }
        }
    }

    /// <summary>
    /// 测试错误处理
    /// </summary>
    static void TestErrorHandling(Calculator calc)
    {
        Console.WriteLine("\n--- Error Handling ---");

        try
        {
            calc.Divide(10, 0);
        }
        catch (DivideByZeroException ex)
        {
            Console.WriteLine($"Caught expected error: {ex.Message}");
        }
    }

    /// <summary>
    /// 显示历史记录
    /// </summary>
    static void DisplayHistory(Calculator calc)
    {
        Console.WriteLine("\n--- Calculation History ---");

        var history = calc.GetHistory();
        foreach (var record in history)
        {
            Console.WriteLine($"  {record}");
        }

        Console.WriteLine($"\nTotal records: {history.Count}");
    }
}
