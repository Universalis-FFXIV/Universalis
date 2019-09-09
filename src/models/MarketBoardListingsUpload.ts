import { MarketBoardItemListingUpload } from "./MarketBoardItemListingUpload";

export interface MarketBoardListingsUpload {
    worldID: number;
    itemID: number;
    listings: MarketBoardItemListingUpload[];
    uploaderID: number | string;
}
