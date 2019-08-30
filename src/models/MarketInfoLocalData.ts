import { MarketBoardHistoryEntry } from "./MarketBoardHistoryEntry";
import { MarketBoardItemListing } from "./MarketBoardItemListing";

export interface MarketInfoLocalWorldData {
    listings: MarketBoardItemListing[];
    history: MarketBoardHistoryEntry[];
}
