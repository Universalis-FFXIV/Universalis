import chalk from "chalk";
import { MongoClient } from "mongodb";
import readline from "readline";
import sha from "sha.js";

const db = MongoClient.connect("mongodb://localhost:27017/", { useNewUrlParser: true, useUnifiedTopology: true });

const stdin = readline.createInterface({
    completer: null,
    input: process.stdin,
    prompt: chalk.cyan("> "),
    terminal: true,
});

stdin.on("line", (line) => {
    const args = line.split(/\s+/g);
    const command = args.pop();

    if (command === "addkey") {
        if (args.length !== 2) {
            return console.log(chalk.bgRedBright("Missing arguments."));
        }
        return console.log(chalk.bgGreenBright("Success."));
    }
});
