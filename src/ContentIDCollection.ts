import { Collection } from "mongodb";

export class ContentIDCollection {
    private contentIDCollection: Collection;

    constructor(contentIDCollection: Collection) {
        this.contentIDCollection = contentIDCollection;
    }

    /** Get an object from the database. */
    async get(contentID: number): Promise<any> {
        const query = { contentID };

        const content = await this.contentIDCollection.findOne(query, { projection: { _id: 0 } });

        return content;
    }

    /** Add content to the database. */
    async set(contentID: number, contentType: string, content: any): Promise<any> {
        content.contentID = contentID;
        content.contentType = contentType;

        await this.contentIDCollection.insertOne(content);

        return content;
    }
}
