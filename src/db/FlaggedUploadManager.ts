import { Collection, Db, MongoError } from "mongodb";
import { Logger } from "winston";
import { MarketBoardItemListingUpload } from "../models/MarketBoardItemListingUpload";

export interface FlaggedUploadEntry {
	worldID: number;
    itemID: number;
    listings: MarketBoardItemListingUpload[];
}

export class FlaggedUploadManager {
	public static async create(
		logger: Logger,
		db: Db,
	): Promise<FlaggedUploadManager> {
		const blacklist = db.collection("flaggedUpload");

		const indices = [{ uploaderID: 1 }];
		const indexNames = indices.map(Object.keys);
		for (let i = 0; i < indices.length; i++) {
			// We check each individually to ensure we don't duplicate indices on failure.
			if (!(await blacklist.indexExists(indexNames[i]).catch(logger.error))) {
				await blacklist.createIndex(indices[i]).catch(logger.error);
			}
		}

		return new FlaggedUploadManager(blacklist);
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
