import { MarketBoardItemListingBase } from "./MarketBoardItemListingBase";

export interface MarketBoardItemListing extends MarketBoardItemListingBase {
    isCrafted?: boolean;
    retainerCity: string;
    total: number;
    worldName?: string;
    sourceName?: string;
}
