import { ItemMateria } from "./ItemMateria";

export interface MarketBoardItemListingBase {
    listingID: string;
    hq: boolean;
    materia?: ItemMateria[];
    pricePerUnit: number;
    quantity: number;
    total?: number;
    retainerID: string;
    retainerName: string;
    creatorName?: string;
    onMannequin?: boolean;
    sellerID: string;
    creatorID?: string;
    lastReviewTime: number;
    stainID?: number;
}
