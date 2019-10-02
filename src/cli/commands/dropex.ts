import chalk from "chalk";

import { MongoError } from "mongodb";

import { CLIResources } from "../../models/CLIResources";

async function dropex(resources: CLIResources, args: string[]) {
    if (args.length < 2) {
        return console.log(chalk.bgRedBright.black("Missing argument worldID/dcName or itemID."));
    }
    if (parseInt(args[0])) {
        try {
            await resources.extendedHistory.deleteOne({ worldID: parseInt(args[0]), itemID: parseInt(args[1]) });
        } catch (e) {
            if ((e as MongoError).code !== 11000) throw e;
        }
    } else {
        try {
            await resources.extendedHistory.deleteOne({ dcName: args[0], itemID: parseInt(args[1]) });
        } catch (e) {
            if ((e as MongoError).code !== 11000) throw e;
        }
    }
    console.log(chalk.bgGreenBright.black("Success."));
}

module.exports = dropex;
