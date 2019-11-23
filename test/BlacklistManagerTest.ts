import * as should from "should";
import * as winston from "winston";
import { BlacklistManager } from "../src/db/BlacklistManager";
import { MongoFixture } from "./MongoFixture";

const dummyUploaderID: string = "dummyUploaderID";

describe("BlacklistManager", () => {
    const logger = winston.createLogger();
    const mongo = new MongoFixture();

    var manager: BlacklistManager;

    before(async () => {
        await mongo.before();
        manager = await BlacklistManager.create(logger, mongo.db);
    });

    after(async () => await mongo.after());

    it("should allow blacklisting of uploaders", async () => {
        should.equal(await manager.has(dummyUploaderID), false);
        await manager.add(dummyUploaderID);
        await manager.add(dummyUploaderID);
        should.equal(await manager.has(dummyUploaderID), true);
    });
});
