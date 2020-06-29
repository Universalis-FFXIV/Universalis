import * as should from "should";
import * as winston from "winston";

import { RemoteDataManager } from "../src/remote/RemoteDataManager";

describe("RemoteDataManager", () => {
	const logger = winston.createLogger();
	const manager: RemoteDataManager = new RemoteDataManager({
		logger,
		remoteFileDirectory: "../public-test",
	});

	it("should retrieve CSV files properly", async () => {
		const json = JSON.parse((await manager.fetchFile("dc.json")).toString());
		should.equal(json.Light[4], "Zodiark");
	});

	it("should parse CSV files properly", async () => {
		const csv = await manager.parseCSV("World.csv");
		should.deepEqual(csv[0], ["key", "0", "1", "2", "3"]);
	});
});
