import * as should from "should";
import { TrustedSourceManager } from "../src/db/TrustedSourceManager";
import { MongoFixture } from "./MongoFixture";

const dummyKey: string = "dummyKey";

describe("TrustedSourceManager", () => {
    const mongo = new MongoFixture();

    var manager: TrustedSourceManager;

    before(async () => {
        await mongo.before();
        manager = await TrustedSourceManager.create(mongo.db);
    });

    after(async () => await mongo.after());

    it("should report unkown keys as not trusted", async () => {
        should.not.exist(await manager.get(dummyKey));
    });

    it("should store new api keys properly, even when added multiple times", async () => {
        await manager.add(dummyKey, "tests");
        await manager.add(dummyKey, "tests");
    });

    it("should recognize stored keys", async () => {
        should.exist(await manager.get(dummyKey));
    });

    it("should store update counts", async () => {
        await manager.increaseUploadCount(dummyKey, 99);
        await manager.increaseUploadCount(dummyKey);
        const source = await manager.get(dummyKey);
        should.equal(source.uploadCount, 100);
    });
});
