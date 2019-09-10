export interface MarketBoardHistoryEntry {
    hq: boolean;
    pricePerUnit: number;
    quantity: number;
    total?: number;
    buyerName: string;
    timestamp: number;
    onMannequin?: boolean;
    sellerID: string;
}
