import { MarketBoardItemListing } from "./MarketBoardItemListing";

export interface MarketBoardListingsUpload {
    worldID: number;
    itemID: number;
    listings: MarketBoardItemListing[];
}
