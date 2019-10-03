import { Collection, Db } from "mongodb";

export class ContentIDCollection {
    public static async create(db: Db): Promise<ContentIDCollection> {
        const contentIDCollection = db.collection("content");

        const indices = [
            { contentID: 1 },
            { contentTypeitemID: 1 }
        ];
        const indexNames = indices.map(Object.keys);
        for (let i = 0; i < indices.length; i++) {
            // We check each individually to ensure we don't duplicate indices on failure.
            if (!await contentIDCollection.indexExists(indexNames[i]).catch(console.error)) {
                await contentIDCollection.createIndex(indices[i]).catch(console.error);
            }
        }

        return new ContentIDCollection(contentIDCollection);
    }

    private contentIDCollection: Collection;

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
