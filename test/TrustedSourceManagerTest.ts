import { MongoClient } from "mongodb";
import should from "should";
import { TrustedSourceManager } from "../src/TrustedSourceManager";

const dummyKey = "dummyKey";

describe("TrustedSourceManager", () => {
    var client: MongoClient;
    var manager: TrustedSourceManager;

    before(async () => {
        client = await MongoClient.connect("mongodb://localhost", { useNewUrlParser: true, useUnifiedTopology: true });
        const db = client.db("universalis-test");
        await db.dropDatabase();
        manager = await TrustedSourceManager.create(db);
    });

    after(async () => {
        await client.close();
    });

    it("should report unkown keys as not trusted", async () => {
        should.not.exist(await manager.get(dummyKey));
    });

    it("should store new api keys properly", async () => {
        await manager.addToTrusted(dummyKey, "tests");
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
