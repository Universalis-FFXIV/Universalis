const commontags = require("common-tags");
const request = require("request-promise");

// Download and parse World CSV
const worldMap = new Map();
const init = (async () => {
	const dataFile = await request("https://raw.githubusercontent.com/xivapi/ffxiv-datamining/master/csv/World.csv");
	let lines = dataFile.match(/[^\r\n]+/g).slice(3);
	for (let line of lines) {
	    line = line.split(",");
	    worldMap.set(line[1].replace(/[^a-zA-Z]+/g, ""), line[0]);
	}
	["Chaos", "Light", "Elemental", "Gaia", "Mana", "Aether", "Crystal", "Primal"].forEach((dc) => {
		worldMap.set(dc, dc);
	});
})();

module.exports = {
	name: "market",
	args: true,
	async execute(client, message, logger, args) {
		if (args.length < 3) return message.reply("please provide an item name in your command, followed by the World or DC name to query.");

		await init;

		let itemName = args.slice(0, args.length - 1).join(" ");
		let worldName = args[args.length - 1];
		worldName = worldName.charAt(0).toUpperCase() + worldName.substr(1);

		const searchData = JSON.parse(await request(`https://xivapi.com/search?string=${itemName}`)).Results[0];
		const itemID = searchData.ID;
		itemName = searchData.Name;
		const worldID = worldMap.get(worldName);

		if (!worldID) return message.reply("that isn't a valid World or DC name. Please check the spelling of the name entered.");

		const allListings = JSON.parse(await request(`https://universalis.app/api/${worldID}/${itemID}`)).listings;

		const trimmedListings = allListings.map((listing) => {
			return {
				hq: listing.hq,
				pricePerUnit: listing.pricePerUnit,
				quantity: listing.quantity,
				retainerName: listing.retainerName,
				total: listing.total,
			};
		}).slice(0, Math.min(10, allListings.length));

		message.channel.send(commontags.stripIndents`
			__${allListings.length} results for ${worldName} (Showing up to 10):__
			${trimmedListings.map((listing) => {
				let output = listing.quantity + " **" + itemName + "** for " +
					listing.pricePerUnit + " Gil by " + listing.retainerName;
				if (listing.quantity > 1)  {
					output += " (For a total of " + listing.total + " Gil)";
				}
				return output;
			}).join("\n")}
		`);
	}
};
