export interface MarketBoardDCItemListing {
    worldName: string;
    hq: 1 | 0;
    materia?: number[];
    pricePerUnit: number;
    quantity: number;
    total?: number;
    retainerName: string;
    retainerCity: string;
    creatorName?: string;
}
