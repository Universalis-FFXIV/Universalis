import { Collection, Db } from "mongodb";

export interface BlacklistEntry {
    uploaderID: string
}

export class BlacklistManager {
    private collection: Collection<BlacklistEntry>;

    constructor(db: Db) {
        this.collection = db.collection("blacklist");
        this.collection.createIndexes([
            { key: { uploaderID: 1 }, unique: true }
        ])
    }

    /** Add an uploader to the blacklist, preventing their data from being processed. */
    public async add(uploaderID: string): Promise<void> {
        await this.collection.insertOne({ uploaderID });
    }

    /** Check if the blacklist has an uploader. */
    public async has(uploaderID: string): Promise<boolean> {
        return await this.collection.findOne({ uploaderID }) != null;
    }
}
