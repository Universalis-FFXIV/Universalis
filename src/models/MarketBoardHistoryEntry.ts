export interface MarketBoardHistoryEntry {
    hq: 1 | 0;
    pricePerUnit: number;
    quantity: number;
    total?: number;
    buyerName: string;
    timestamp: number;
    onMannequin?: boolean;
    buyerID: number;
    sellerID: number;
    uploaderID?: string;
}
