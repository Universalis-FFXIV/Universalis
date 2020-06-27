import { MinimizedHistoryEntry } from "./MinimizedHistoryEntry";

export interface MinimizedDCHistoryEntry extends MinimizedHistoryEntry {
	worldName: string;
	uploaderID: string;
}
