export interface MarketBoardHistoryEntry {
	hq: boolean;
	pricePerUnit: number;
	quantity: number;
	total?: number;
	buyerName: string;
	timestamp: number;
	uploadApplication?: string;
	onMannequin?: boolean;
	worldName?: string;
	worldID?: number;
}
