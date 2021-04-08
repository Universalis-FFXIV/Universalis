import { MongoClient } from "mongodb";
import { createLogger } from "winston";
import { initializeWorldMappings } from "../initializeWorldMappings";
import { RemoteDataManager } from "../remote/RemoteDataManager";
import { removeOld } from "../util";

const db = MongoClient.connect("mongodb://localhost:27017/", {
	useNewUrlParser: true,
	useUnifiedTopology: true,
});

const worldMap: Map<string, number> = new Map();
const worldIDMap: Map<number, string> = new Map();

(async () => {
	const logger = createLogger();

	const remoteDataManager = new RemoteDataManager({ logger });
	await remoteDataManager.fetchAll();

	const marketableItemIDs = await remoteDataManager.getMarketableItemIDs();

	await initializeWorldMappings(worldMap, worldIDMap);

	const universalisDB = (await db).db("universalis");
	const recentData = universalisDB.collection("recentData");

	for (const [worldID] of worldIDMap) {
		for (const itemID of marketableItemIDs) {
			const deletedAny = await removeOld(recentData, worldID, itemID);
			if (deletedAny) {
				const newCount = await recentData.find({ worldID, itemID }).count();
				if (newCount === 1) {
					// tslint:disable-next-line: no-console
					console.log(`Finished cleanup, new count: 1`);
				} else {
					throw new Error(`Something went very wrong! New count: ${newCount}`);
				}
			}
		}
	}
})();
