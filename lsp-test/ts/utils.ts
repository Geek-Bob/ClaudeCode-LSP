export function formatName(first: string, last: string): string {
    return `${last}, ${first}`;
}

export function sum(numbers: number[]): number {
    return numbers.reduce((a, b) => a + b, 0);
}

export function capitalize(str: string): string {
    return str.charAt(0).toUpperCase() + str.slice(1);
}

export class Logger {
    private logs: string[] = [];

    log(message: string): void {
        const timestamp = new Date().toISOString();
        this.logs.push(`[${timestamp}] ${message}`);
    }

    getLogs(): string[] {
        return [...this.logs];
    }

    clear(): void {
        this.logs = [];
    }
}
