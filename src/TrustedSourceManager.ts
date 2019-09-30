import { Collection, Db, MongoError } from "mongodb";
import { sha512 } from "sha.js";
import { TrustedSource } from "./models/TrustedSource";

export class TrustedSourceManager {
    public static async create(db: Db): Promise<TrustedSourceManager> {
        const collection = db.collection("trustedSources");

        const indices = [
            { apiKey: 1 },
        ];
        const indexNames = indices.map(Object.keys);
        for (let i = 0; i < indices.length; i++) {
            // We check each individually to ensure we don't duplicate indices on failure.
            if (!await collection.indexExists(indexNames[i])) {
                await collection.createIndex(indices[i]);
            }
        }

        return new TrustedSourceManager(collection);
    }

    private collection: Collection<TrustedSource>;

    private constructor(collection: Collection<TrustedSource>) {
        this.collection = collection;
    }

    public async add(apiKey: string, sourceName: string): Promise<void> {
        try {
            await this.collection.insertOne({
                apiKey: this.apiKeyHash(apiKey),
                sourceName,
                uploadCount: 0,
            });
        } catch (e) {
            if ((e as MongoError).code !== 11000) throw e;
        }
    }

    public async remove(apiKey: string): Promise<void> {
        await this.collection.deleteOne({ apiKey: this.apiKeyHash(apiKey) });
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
