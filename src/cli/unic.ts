import chalk from "chalk";
import readline from "readline";

const stdin = readline.createInterface({
    completer: null,
    input: process.stdin,
    output: process.stdout,
    prompt: chalk.cyan("> "),
    terminal: true,
});

stdin.on("line", (line) => {

});
