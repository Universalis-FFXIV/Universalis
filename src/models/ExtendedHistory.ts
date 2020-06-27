import { MinimizedHistoryEntry } from "./MinimizedHistoryEntry";

export interface ExtendedHistory {
	worldID: number;
	itemID: number;
	lastUploadTime: number;
	entries: MinimizedHistoryEntry[];
}
