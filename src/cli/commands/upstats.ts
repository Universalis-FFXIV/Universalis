import { CLIResources } from "../../models/CLIResources";

async function upstats(resources: CLIResources, args: string[]) {
	const uploaders = await resources.trustedSources.getUploadersCount();
	uploaders.forEach(console.log);
}

module.exports = upstats;
