export interface ITransport {
	name: string;
	fetchData: (itemID: number, world: string) => Promise<any>;
}
