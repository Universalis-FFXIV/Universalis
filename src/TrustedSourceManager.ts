import { Collection, Db } from "mongodb";
import { sha512 } from "sha.js";
import { TrustedSource } from "./models/TrustedSource";

export class TrustedSourceManager {
    private collection: Collection<TrustedSource>;

    constructor(db: Db) {
        this.collection = db.collection("trustedSources");
        this.collection.createIndexes([
            { key: { apiKey: 1 }, unique: true }
        ]);
    }

    public async addToTrusted(apiKey: string, sourceName: string): Promise<void> {
        await this.collection.insertOne({
            apiKey: this.apiKeyHash(apiKey),
            sourceName,
            uploadCount: 0,
        });
    }

    public async get(apiKey: string): Promise<TrustedSource> {
        return await this.collection.findOne({ apiKey: this.apiKeyHash(apiKey) });
    }

    public async increaseUploadCount(apiKey: string, increase: number = 1) {
        return await this.collection.updateOne(
            { apiKey: this.apiKeyHash(apiKey) },
            { $inc: { uploadCount: increase } }
        );
    }

    private apiKeyHash(apiKey: string): string {
        return new sha512().update(apiKey).digest("hex");
    }
}
