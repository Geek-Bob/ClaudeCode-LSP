namespace LspTest;

public class Calculator
{
    private readonly List<string> _history = new();

    public int Add(int a, int b)
    {
        int result = a + b;
        _history.Add($"{a} + {b} = {result}");
        return result;
    }

    public int Subtract(int a, int b)
    {
        int result = a - b;
        _history.Add($"{a} - {b} = {result}");
        return result;
    }

    public int Multiply(int a, int b)
    {
        int result = a * b;
        _history.Add($"{a} * {b} = {result}");
        return result;
    }

    public double Divide(int a, int b)
    {
        if (b == 0)
            throw new ArgumentException("Cannot divide by zero");
        double result = (double)a / b;
        _history.Add($"{a} / {b} = {result}");
        return result;
    }

    public IReadOnlyList<string> History => _history.AsReadOnly();

    public void ClearHistory()
    {
        _history.Clear();
    }
}
