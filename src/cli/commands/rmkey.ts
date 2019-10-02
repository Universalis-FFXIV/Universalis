import chalk from "chalk";

import { CLIResources } from "../../models/CLIResources";

async function rmkey(resources: CLIResources, args: string[]) {
    if (args.length < 1) {
        return console.log(chalk.bgRedBright.black("Missing argument apiKey."));
    }
    await resources.trustedSources.remove(args[0]).then(() => {
        console.log(chalk.bgGreenBright.black("Success."));
    }).catch((err) => {
        console.error(chalk.red(err));
    });
}

module.exports = rmkey;
