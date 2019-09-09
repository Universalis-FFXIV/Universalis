import { ItemMateria } from "./ItemMateria";

export interface MarketBoardItemListingBase {
    hq: 1 | 0;
    materia?: ItemMateria[];
    pricePerUnit: number;
    quantity: number;
    total?: number;
    retainerName: string;
    creatorName?: string;
    onMannequin?: boolean;
    sellerID: number;
    creatorID?: number;
    lastReviewTime: number;
    stainID?: number;
    // todo tax
}
