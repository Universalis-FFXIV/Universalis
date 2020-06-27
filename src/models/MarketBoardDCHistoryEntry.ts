import { MarketBoardHistoryEntry } from "./MarketBoardHistoryEntry";

export interface MarketBoardDCHistoryEntry extends MarketBoardHistoryEntry {
	worldName: string;
	uploaderID: string;
}
