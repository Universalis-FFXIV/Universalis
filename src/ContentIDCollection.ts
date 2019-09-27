import { Collection } from "mongodb";

export class ContentIDCollection {
    private contentIDCollection: Collection;

    public static async create(contentIDCollection: Collection): Promise<ContentIDCollection> {
        await contentIDCollection.createIndexes([
            { key: { contentID: 1 }, unique: true },
            { key: { contentType: 1 }, unique: true }
        ]);

        return new ContentIDCollection(contentIDCollection);
    }

    private constructor(contentIDCollection: Collection) {
        this.contentIDCollection = contentIDCollection;
    }

    /** Get an object from the database. */
    public async get(contentID: string): Promise<any> {
        const query = { contentID };

        const content = await this.contentIDCollection.findOne(query, { projection: { _id: 0 } });

        return content;
    }

    /** Add content to the database. */
    public async set(contentID: string, contentType: string, content: any): Promise<any> {
        content.contentID = contentID;
        content.contentType = contentType;

        await this.contentIDCollection.insertOne(content);

        return content;
    }
}
