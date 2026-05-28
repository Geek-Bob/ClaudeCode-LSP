public class Hello {
    public static void main(String[] args) {
        String greeting = "Hello, World!";
        System.out.println(greeting);

        Hello app = new Hello();
        System.out.println(app.greet("Alice"));

        Calculator calc = new Calculator();
        System.out.println("5 + 3 = " + calc.add(5, 3));
        System.out.println("10 - 4 = " + calc.subtract(10, 4));
        System.out.println("6 * 7 = " + calc.multiply(6, 7));
        System.out.println("15 / 4 = " + calc.divide(15, 4));
    }

    public String greet(String name) {
        return "Hello, " + name;
    }
}

class Calculator {
    public int add(int a, int b) {
        return a + b;
    }

    public int subtract(int a, int b) {
        return a - b;
    }

    public int multiply(int a, int b) {
        return a * b;
    }

    public double divide(int a, int b) {
        if (b == 0) {
            throw new IllegalArgumentException("Cannot divide by zero");
        }
        return (double) a / b;
    }
}
