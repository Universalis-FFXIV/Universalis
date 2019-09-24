import { RemoteDataManager } from "../src/RemoteDataManager";
import should from "should";
import winston = require("winston");

describe("RemoteDataManager", () => {
    const logger = winston.createLogger();
    const manager: RemoteDataManager = new RemoteDataManager({ logger, remoteFileDirectory: "../public-test" });

    it("should retrieve CSV files properly", async () => {
        const json = JSON.parse((await manager.fetchFile("dc.json")).toString());
        should.equal(json.Light[4], "Zodiark");
    })

    it("should parse CSV files properly", async () => {
        const csv = await manager.parseCSV("World.csv");
        should.deepEqual(csv[0], ["key", "0", "1", "2", "3"]);
    })
});
