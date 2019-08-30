import { MarketBoardHistoryEntry } from "./MarketBoardHistoryEntry";

export interface MarketBoardSaleHistoryUpload {
    worldID: number;
    itemID: number;
    entries: MarketBoardHistoryEntry[];
}
