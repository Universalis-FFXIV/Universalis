import { MarketBoardItemListingBase } from "./MarketBoardItemListingBase";

export interface MarketBoardItemListing extends MarketBoardItemListingBase {
	isCrafted?: boolean;
	retainerCity: string | number;
	total: number;
	worldName?: string;
	worldID?: number;
	sourceName?: string;
}
