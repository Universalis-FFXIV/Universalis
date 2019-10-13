import { Collection, Db, MongoError } from "mongodb";
import { Logger } from "winston";

export interface BlacklistEntry {
    uploaderID: string;
}

export class BlacklistManager {
    public static async create(logger: Logger, db: Db): Promise<BlacklistManager> {
        const blacklist = db.collection("blacklist");

        const indices = [
            { uploaderID: 1 }
        ];
        const indexNames = indices.map(Object.keys);
        for (let i = 0; i < indices.length; i++) {
            // We check each individually to ensure we don't duplicate indices on failure.
            if (!await blacklist.indexExists(indexNames[i]).catch(logger.error)) {
                await blacklist.createIndex(indices[i]).catch(logger.error);
            }
        }

        return new BlacklistManager(blacklist);
    }

    private collection: Collection<BlacklistEntry>;

    private constructor(collection: Collection<BlacklistEntry>) {
        this.collection = collection;
    }

    /** Add an uploader to the blacklist, preventing their data from being processed. */
    public async add(uploaderID: string): Promise<void> {
        try {
            await this.collection.insertOne({ uploaderID });
        } catch (e) {
            if ((e as MongoError).code !== 11000) throw e;
        }
    }

    /** Check if the blacklist has an uploader. */
    public async has(uploaderID: string): Promise<boolean> {
        return await this.collection.findOne({ uploaderID }) != null;
    }
}
