import { CLIResources } from "../../models/CLIResources";

async function upstats(resources: CLIResources, args: string[]) {
	const uploaders = await resources.trustedSources.getUploadersCount();
	console.log(uploaders);
}

module.exports = upstats;
