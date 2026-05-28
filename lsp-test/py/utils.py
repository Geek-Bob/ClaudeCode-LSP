def format_name(first: str, last: str) -> str:
    return f"{last}, {first}"


def sum_numbers(numbers: list[float]) -> float:
    return sum(numbers)


def capitalize(s: str) -> str:
    return s[0].upper() + s[1:] if s else ""


class Logger:
    def __init__(self) -> None:
        self._logs: list[str] = []

    def log(self, message: str) -> None:
        from datetime import datetime
        self._logs.append(f"[{datetime.now().isoformat()}] {message}")

    def get_logs(self) -> list[str]:
        return self._logs.copy()

    def clear(self) -> None:
        self._logs.clear()
