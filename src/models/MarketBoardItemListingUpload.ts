import { MarketBoardItemListingBase } from "./MarketBoardItemListingBase";

export interface MarketBoardItemListingUpload
	extends MarketBoardItemListingBase {
	retainerCity: number;
	totalTax?: number;
	uploaderID: number | string;

	uploadApplication?: string;
}
