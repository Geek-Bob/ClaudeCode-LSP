namespace LspTest;

class Program
{
    static void Main(string[] args)
    {
        var calc = new Calculator();
        int sum = calc.Add(10, 20);
        int product = calc.Multiply(5, 6);
        double quotient = calc.Divide(15, 4);
        Console.WriteLine($"Sum: {sum}, Product: {product}, Quotient: {quotient}");

        var person = new Person("Alice", 30);
        person.Greet();
        person.HaveBirthday();
        person.Greet();
        person.SetEmail("alice@example.com");
        Console.WriteLine(person.GetInfo());
    }
}

public class Person
{
    public string Name { get; private set; }
    public int Age { get; private set; }
    public string? Email { get; private set; }

    public Person(string name, int age)
    {
        Name = name;
        Age = age;
    }

    public void Greet()
    {
        Console.WriteLine($"Hello, I'm {Name}, {Age} years old.");
    }

    public void HaveBirthday()
    {
        Age++;
    }

    public void SetEmail(string email)
    {
        Email = email;
    }

    public string GetInfo()
    {
        return $"{Name} ({Age}) - {Email ?? "no email"}";
    }
}
