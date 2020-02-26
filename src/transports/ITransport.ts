import { MarketBoardItemListing } from "../models/MarketBoardItemListing";

export interface ITransport {
    name: string;
    fetchData: (itemId: number, world: string) => Promise<MarketBoardItemListing[]>;
}
