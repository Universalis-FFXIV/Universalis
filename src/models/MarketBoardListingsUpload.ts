import { MarketBoardItemListing } from "./MarketBoardItemListing";

export interface MarketBoardListingsUpload {
    worldID: number;
    itemID: number;
    listing1: MarketBoardItemListing;
    listing2?: MarketBoardItemListing;
    listing3?: MarketBoardItemListing;
    listing4?: MarketBoardItemListing;
    listing5?: MarketBoardItemListing;
    listing6?: MarketBoardItemListing;
    listing7?: MarketBoardItemListing;
    listing8?: MarketBoardItemListing;
    listing9?: MarketBoardItemListing;
    listing10?: MarketBoardItemListing;
}
