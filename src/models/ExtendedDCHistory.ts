import { MinimizedDCHistoryEntry } from "./MinimizedDCHistoryEntry";

export interface ExtendedDCHistory {
    dcName: string;
    itemID: number;
    entries: MinimizedDCHistoryEntry[];
}
