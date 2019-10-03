import chalk from "chalk";
import fs from "fs";
import { Collection, MongoClient } from "mongodb";
import path from "path";
import readline from "readline";
import util from "util";

import { TrustedSourceManager } from "../TrustedSourceManager";

import { CLIResources } from "../models/CLIResources";

const { unicVersion } = require("../../package.json");

const readdir = util.promisify(fs.readdir);

// Load resources
const db = MongoClient.connect("mongodb://localhost:27017/", { useNewUrlParser: true, useUnifiedTopology: true });
var extendedHistory: Collection;
var recentData: Collection;

var trustedSources: TrustedSourceManager;

const commands: Map<string, () => Promise<void>> = new Map();

var resources: CLIResources;

const init = (async () => {
    const universalisDB = (await db).db("universalis");

    extendedHistory = universalisDB.collection("extendedHistory");
    recentData = universalisDB.collection("recentData");

    trustedSources = await TrustedSourceManager.create(universalisDB);

    const commandFiles = (await readdir(path.join(__dirname, "./commands")))
        .filter((fileName) => fileName.endsWith(".js"));

    for (const fileName of commandFiles) {
        commands.set(fileName.substr(0, fileName.indexOf(".")),
            require(path.join(__dirname, `./commands/${fileName}`)));
    }

    resources = {
        extendedHistory,
        recentData,
        trustedSources
    };
})();

// Console application
console.log(chalk.cyan(`Universalis Console Tool v${unicVersion}`));

const stdin = readline.createInterface({
    completer: autocomplete,
    input: process.stdin,
    output: process.stdout,
    prompt: chalk.cyan("> ")
});

stdin.prompt();

stdin.on("line", async (line) => {
    await init;

    let args = line.split(/\s+/g);
    const command = args[0];
    args = args.slice(1);

    const commandF = commands.get(command);
    if (commandF) {
        await commandF(resources, args);
    } else if (command === "exit") {
        stdin.close();
    } else {
        console.log(chalk.bgYellow.black(`'${command}' is not a valid command.`));
    }

    stdin.prompt();
}).on("close", () => {
    console.log(chalk.cyan("\nGoodbye."));
    process.exit(0);
});

function autocomplete(line: string) {
    const completions = ["addkey", "rmkey", "upstats", "drop", "dropex"];
    const hits = completions.filter((command) => command.startsWith(line));
    return [hits.length ? hits : completions, line];
}
