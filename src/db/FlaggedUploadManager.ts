import { Collection, Db, MongoError } from "mongodb";
import { MarketBoardItemListingUpload } from "../models/MarketBoardItemListingUpload";

export interface FlaggedUploadEntry {
	worldID: number;
    itemID: number;
    listings: MarketBoardItemListingUpload[];
}

export class FlaggedUploadManager {
	public static async create(
		db: Db,
	): Promise<FlaggedUploadManager> {
		const flaggedUploadCollection = db.collection("flaggedUpload");
		return new FlaggedUploadManager(flaggedUploadCollection);
	}

	private collection: Collection<FlaggedUploadEntry>;

	private constructor(collection: Collection<FlaggedUploadEntry>) {
		this.collection = collection;
	}

	public async add(worldID: number, itemID: number, listings: MarketBoardItemListingUpload[]): Promise<void> {
		try {
			await this.collection.insertOne({ worldID, itemID, listings });
		} catch (e) {
			if ((e as MongoError).code !== 11000) throw e;
		}
	}

	public async remove(worldID: number, itemID: number, listings: MarketBoardItemListingUpload[]): Promise<void> {
		try {
			await this.collection.deleteOne({ worldID, itemID, listings });
		} catch (e) {
			if ((e as MongoError).code !== 11000) throw e;
		}
	}

	public async has(worldID: number, itemID: number, listings: MarketBoardItemListingUpload[]): Promise<boolean> {
		return (await this.collection.findOne({ worldID, itemID, listings })) != null;
	}
}
