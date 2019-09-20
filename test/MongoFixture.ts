import { Db, MongoClient } from "mongodb";

export class MongoFixture {
    private mongoUri: string;

    private client: MongoClient;
    private _db: Db;

    public constructor(mongoUri: string = "mongodb://localhost") {
        this.mongoUri = mongoUri;
    }

    public async before(): Promise<void> {
        this.client = await MongoClient.connect(this.mongoUri, { useNewUrlParser: true, useUnifiedTopology: true });
        this._db = this.client.db("universalis-test");
        await this._db.dropDatabase();
    }

    public async after(): Promise<void> {
        await this.client.close();
    }

    public get db(): Db {
        return this._db;
    }
}
