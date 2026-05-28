from dataclasses import dataclass
from typing import Optional


@dataclass
class Person:
    name: str
    age: int
    email: Optional[str] = None

    def greet(self) -> str:
        return f"Hello, I'm {self.name}, {self.age} years old."

    def have_birthday(self) -> None:
        self.age += 1

    def set_email(self, email: str) -> None:
        self.email = email


class Calculator:
    """A simple calculator with history tracking."""

    def __init__(self) -> None:
        self._history: list[str] = []

    def add(self, a: float, b: float) -> float:
        result = a + b
        self._history.append(f"{a} + {b} = {result}")
        return result

    def subtract(self, a: float, b: float) -> float:
        result = a - b
        self._history.append(f"{a} - {b} = {result}")
        return result

    def multiply(self, a: float, b: float) -> float:
        result = a * b
        self._history.append(f"{a} * {b} = {result}")
        return result

    def divide(self, a: float, b: float) -> float:
        if b == 0:
            raise ValueError("Cannot divide by zero")
        result = a / b
        self._history.append(f"{a} / {b} = {result}")
        return result

    @property
    def history(self) -> list[str]:
        return self._history.copy()

    def clear_history(self) -> None:
        self._history.clear()


if __name__ == "__main__":
    alice = Person("Alice", 30)
    print(alice.greet())
    alice.have_birthday()
    print(alice.greet())

    calc = Calculator()
    calc.add(10, 5)
    calc.multiply(3, 4)
    print(calc.history)
