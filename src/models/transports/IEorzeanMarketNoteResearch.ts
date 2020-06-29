export interface IEorzeanMarketNoteResearch {
	itemID: number;
	world: string;
	dc?: string;

	priceNQ: number;
	priceNQWorld?: string;
	priceHQ: number;
	priceHQWorld?: string;
	stockNQ: number;
	stockHQ: number;
	circulationNQ: number;
	circulationHQ: number;
	turnoverPerDayNQ: number;
	turnoverPerDayHQ: number;
	researchedTime: number;
	researchedTimeWorld?: string;
}
