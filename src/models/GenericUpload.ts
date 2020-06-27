import { MarketBoardHistoryEntry } from "./MarketBoardHistoryEntry";
import { MarketBoardItemListingUpload } from "./MarketBoardItemListingUpload";
import { MarketTaxRates } from "./MarketTaxRates";

export interface GenericUpload {
	uploaderID: number | string;

	itemID?: number;
	itemIDs?: number[];
	worldID?: number;

	contentID?: string | number;
	characterName?: string;
	entries?: MarketBoardHistoryEntry[];
	listings?: MarketBoardItemListingUpload[];
	marketTaxRates?: MarketTaxRates;

	op?: {
		listings?: number;
	};
}
