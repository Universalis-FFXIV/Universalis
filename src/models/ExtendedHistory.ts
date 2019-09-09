import { MinimizedHistoryEntry } from "./MinimizedHistoryEntry";

export interface ExtendedHistory {
    worldID: number;
    itemID: number;
    entries: MinimizedHistoryEntry[];
    uploaderID: string;
}
