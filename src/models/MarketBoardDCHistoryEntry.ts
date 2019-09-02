export interface MarketBoardDCHistoryEntry {
    worldName: string;
    hq: 1 | 0;
    pricePerUnit: number;
    quantity: number;
    total?: number;
    buyerName: string;
    timestamp: number;
}
