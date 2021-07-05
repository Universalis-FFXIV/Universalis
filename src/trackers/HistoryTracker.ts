import * as R from "remeda";

import { Tracker } from "./Tracker";

import { Collection, Db } from "mongodb";

import { ExtendedHistory } from "../models/ExtendedHistory";
import { MarketBoardHistoryEntry } from "../models/MarketBoardHistoryEntry";
import { MarketInfoLocalData } from "../models/MarketInfoLocalData";
import { MinimizedHistoryEntry } from "../models/MinimizedHistoryEntry";

export class HistoryTracker extends Tracker {
	public static async create(db: Db): Promise<HistoryTracker> {
		const recentData = db.collection("recentData");
		const extendedHistory = db.collection("extendedHistory");

		const indices = [{ dcName: 1 }, { itemID: 1 }, { worldID: 1 }];
		const indexNames = indices.map(Object.keys);
		for (let i = 0; i < indices.length; i++) {
			// We check each individually to ensure we don't duplicate indices on failure.
			if (!(await extendedHistory.indexExists(indexNames[i]))) {
				await extendedHistory.createIndex(indices[i]);
			}
		}

		return new HistoryTracker(recentData, extendedHistory);
	}

	private extendedHistory: Collection;

	private constructor(recentData: Collection, extendedHistory: Collection) {
		super(recentData);
		this.extendedHistory = extendedHistory;
	}

	public async set(
		uploaderID: string,
		itemID: number,
		worldID: number,
		recentHistory: MarketBoardHistoryEntry[],
	) {
		if (!recentHistory) return; // This should never be empty.

		const data: MarketInfoLocalData = {
			itemID,
			lastUploadTime: Date.now(),
			recentHistory,
			uploaderID,
			worldID,
		};

		const query = { worldID, itemID };

		const existing = (await this.collection.findOne(query, {
			projection: { _id: 0 },
		})) as MarketInfoLocalData;
		if (existing && existing.listings) {
			data.listings = existing.listings;
		}

		await this.updateExtendedHistory(
			uploaderID,
			itemID,
			worldID,
			recentHistory,
		);

		if (existing) {
			await this.collection.updateOne(query, { $set: data });
		} else {
			await this.collection.insertOne(data);
		}
	}

	private async updateExtendedHistory(
		uploaderID: string,
		itemID: number,
		worldID: number,
		entries: MarketBoardHistoryEntry[],
	) {
		// Cut out any properties we don't need
		let minimizedEntries: MinimizedHistoryEntry[] = entries.map((entry) => {
			return {
				hq: entry.hq,
				pricePerUnit: entry.pricePerUnit,
				quantity: entry.quantity,
				timestamp: entry.timestamp,
				uploaderID,
			};
		});

		const query = { worldID, itemID };

		const existing = (await this.extendedHistory.findOne(query, {
			projection: { _id: 0 },
		})) as ExtendedHistory;

		let extendedHistory: ExtendedHistory;
		if (existing) {
			extendedHistory = existing;

			minimizedEntries = R.pipe(
				minimizedEntries,
				R.filter(
					(entry) =>
						!extendedHistory.entries.some((ex) => {
							return (
								ex.hq === entry.hq &&
								ex.pricePerUnit === entry.pricePerUnit &&
								ex.timestamp === entry.timestamp
							);
						}),
				),
			);
		} else {
			extendedHistory = {
				entries: [],
				itemID,
				lastUploadTime: Date.now(),
				worldID,
			};
		}

		// Limit to 1800 entries
		extendedHistory.entries = R.pipe(
			extendedHistory.entries,
			R.sort((a, b) => b.timestamp - a.timestamp),
			R.take(1800 - minimizedEntries.length),
			R.concat(minimizedEntries),
		);

		if (existing) {
			await this.extendedHistory.updateOne(query, { $set: extendedHistory });
		} else {
			await this.extendedHistory.insertOne(extendedHistory);
		}

		return extendedHistory;
	}
}
