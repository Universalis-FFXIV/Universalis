import { MarketBoardHistoryEntry } from "./MarketBoardHistoryEntry";

export interface MarketBoardSaleHistoryUpload {
    worldID: number;
    itemID: number;
    entry1: MarketBoardHistoryEntry;
    entry2?: MarketBoardHistoryEntry;
    entry3?: MarketBoardHistoryEntry;
    entry4?: MarketBoardHistoryEntry;
    entry5?: MarketBoardHistoryEntry;
    entry6?: MarketBoardHistoryEntry;
    entry7?: MarketBoardHistoryEntry;
    entry8?: MarketBoardHistoryEntry;
    entry9?: MarketBoardHistoryEntry;
    entry10?: MarketBoardHistoryEntry;
}
