import chalk from "chalk";

import { CLIResources } from "../../models/CLIResources";

async function addkey(resources: CLIResources, args: string[]) {
	if (args.length < 2) {
		return console.log(
			chalk.bgRedBright.black("Missing argument apiKey or sourceName."),
		);
	}
	await resources.trustedSources
		.add(args[0], args[1])
		.then(() => {
			console.log(chalk.bgGreenBright.black("Success."));
		})
		.catch((err) => {
			console.error(chalk.red(err));
		});
}

module.exports = addkey;
