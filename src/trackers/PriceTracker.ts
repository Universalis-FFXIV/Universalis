import { Tracker } from "./Tracker";

import { Collection, Db } from "mongodb";

import { MarketBoardItemListing } from "../models/MarketBoardItemListing";
import { MarketInfoLocalData } from "../models/MarketInfoLocalData";

export class PriceTracker extends Tracker {
	public static async create(db: Db): Promise<PriceTracker> {
		const collection = db.collection("recentData");

		const indices = [{ dcName: 1 }, { itemID: 1 }, { worldID: 1 }];
		const indexNames = indices.map(Object.keys);
		for (let i = 0; i < indices.length; i++) {
			// We check each individually to ensure we don't duplicate indices on failure.
			if (!(await collection.indexExists(indexNames[i]))) {
				await collection.createIndex(indices[i]);
			}
		}

		return new PriceTracker(collection);
	}

	private constructor(collection: Collection) {
		super(collection);
	}

	public async set(
		uploaderID: string,
		sourceName: string,
		itemID: number,
		worldID: number,
		listings: MarketBoardItemListing[],
	) {
		const data: MarketInfoLocalData = {
			itemID,
			lastUploadTime: Date.now(),
			listings,
			uploaderID,
			worldID,
		};

		data.listings = data.listings.map((listing) => {
			listing.sourceName = sourceName;
			return listing;
		});

		const query = { worldID, itemID };

		const existing = (await this.collection.findOne(query, {
			projection: { _id: 0 },
		})) as MarketInfoLocalData;
		if (existing && existing.recentHistory) {
			data.recentHistory = existing.recentHistory;
		}

		this.updateDataCenterProperty(
			uploaderID,
			"listings",
			itemID,
			worldID,
			listings,
			sourceName,
		);

		if (existing) {
			await this.collection.updateOne(query, { $set: data });
		} else {
			await this.collection.insertOne(data);
		}
	}
}
