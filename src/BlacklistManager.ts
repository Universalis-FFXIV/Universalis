import { Collection } from "mongodb";

export class BlacklistManager {
    private blacklist: Collection;

    constructor(blacklist: Collection) {
        this.blacklist = blacklist;
    }

    /** Add an uploader to the blacklist, preventing their data from being processed. */
    public async add(uploaderID: string): Promise<void> {
        await this.blacklist.insertOne({ uploaderID });
    }

    /** Check if the blacklist has an uploader. */
    public async has(uploaderID: string): Promise<boolean> {
        const exists = await this.blacklist.findOne({ uploaderID });
        return exists ? true : false;
    }
}
