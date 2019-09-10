import { ItemMateria } from "./ItemMateria";

export interface MarketBoardItemListingBase {
    listingID: number;
    hq: boolean;
    materia?: ItemMateria[];
    pricePerUnit: number;
    quantity: number;
    total?: number;
    retainerID: number;
    retainerName: string;
    creatorName?: string;
    onMannequin?: boolean;
    sellerID: number;
    creatorID?: number;
    lastReviewTime: number;
    stainID?: number;
    // todo tax
}
