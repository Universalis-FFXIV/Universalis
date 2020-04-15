import chalk from "chalk";
import { spawn, SpawnOptionsWithoutStdio } from "child_process";
import readline from "readline";

import { CLIResources } from "../../models/CLIResources";

function logs(resources: CLIResources, args: string[]) {
	return new Promise((resolve) => {
		const stdin = readline.createInterface({
			input: process.stdin,
			output: process.stdout,
			prompt: chalk.greenBright("> "),
		});
		let logger = initLogger(args, resolve);

		stdin.prompt();
		stdin.on("line", (line) => {
			if (line === "exit") {
				logger.kill();
			} else if (line.startsWith("filter")) {
				const fieldName = line.split(" ")[1];
				logger.removeAllListeners("close");
				logger = initLogger(args, resolve, {
					stdio: ["pipe", null, "pipe"],
				}).on("message", (message) => {
					if (message.includes(fieldName)) {
						console.log(message);
					}
				});
			}
		});
	});
}

function initLogger(
	args: string[],
	cb: (arg0?: unknown) => void,
	options?: SpawnOptionsWithoutStdio,
) {
	const logger = spawn(`pm2 logs universalis${args.join(" ")}`, options);
	logger.on("close", () => {
		cb();
	});
	return logger;
}

module.exports = logs;
