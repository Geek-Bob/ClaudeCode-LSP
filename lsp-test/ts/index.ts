interface Greeter {
    greet(): string;
}

interface Identifiable {
    id: number;
    getName(): string;
}

class Person implements Greeter, Identifiable {
    constructor(
        public id: number,
        private name: string,
        private age: number
    ) {}

    getName(): string {
        return this.name;
    }

    greet(): string {
        return `Hello, I'm ${this.name}, ${this.age} years old.`;
    }

    haveBirthday(): void {
        this.age++;
    }
}

class Animal implements Greeter {
    constructor(
        private species: string,
        private sound: string
    ) {}

    greet(): string {
        return `${this.species} says ${this.sound}!`;
    }
}

function makeGreet(greeter: Greeter): void {
    console.log(greeter.greet());
}

const alice = new Person(1, "Alice", 30);
const dog = new Animal("Dog", "Woof");

makeGreet(alice);
makeGreet(dog);

export { Person, Animal, makeGreet };
export type { Greeter, Identifiable };
