import { MarketBoardItemListingBase } from "./MarketBoardItemListingBase";

export interface MarketBoardItemListing extends MarketBoardItemListingBase {
    retainerCity: string;
    total: number;
    worldName?: string;
}
