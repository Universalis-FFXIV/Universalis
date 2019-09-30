import chalk from "chalk";
import { MongoClient } from "mongodb";
import readline from "readline";

import { TrustedSourceManager } from "../TrustedSourceManager";

// Load database
const db = MongoClient.connect("mongodb://localhost:27017/", { useNewUrlParser: true, useUnifiedTopology: true });
var trustedSources: TrustedSourceManager;

const init = (async () => {
    const universalisDB = (await db).db("universalis");

    trustedSources = await TrustedSourceManager.create(universalisDB);
})();

// Console application
console.log(chalk.cyan(`Universalis Console Tool v${require("../../package.json").unicVersion}`));

const stdin = readline.createInterface({
    completer: autocomplete,
    input: process.stdin,
    output: process.stdout,
    prompt: chalk.cyan("> ")
});

stdin.prompt();

stdin.on("line", async (line) => {
    await init;

    const args = line.split(/\s+/g);
    const command = args.pop();

    if (command === "addkey") {
        if (args.length < 2) {
            return console.log(chalk.bgRedBright.black("Missing argument apiKey or sourceName."));
        }
        await trustedSources.add(args[0], args[1]).then(() => {
            console.log(chalk.bgGreenBright.black("Success."));
        }).catch((err) => {
            console.error(chalk.red(err));
        });
    } else if (command === "rmkey") {
        if (args.length < 1) {
            return console.log(chalk.bgRedBright.black("Missing argument apiKey."));
        }
        await trustedSources.remove(args[0]).then(() => {
            console.log(chalk.bgGreenBright.black("Success."));
        }).catch((err) => {
            console.error(chalk.red(err));
        });
    } else {
        console.log(chalk.bgYellow.black(`'${command}' is not a valid command.`));
    }

    stdin.prompt();
}).on("close", () => {
    console.log(chalk.cyan("\nGoodbye."));
    process.exit(0);
});

function autocomplete(line: string) {
    const completions = ["addkey"];
    const hits = completions.filter((command) => line.startsWith(command));
    return [hits.length ? hits : completions, line];
}
