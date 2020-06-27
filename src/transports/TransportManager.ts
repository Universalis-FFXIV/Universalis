import { ITransport } from "../models/transports/ITransport";

// Singleton transport manager.
export class TransportManager {
	public transports: ITransport[];

	constructor() {
		this.transports = [];
	}

	public getTransport(name: string): ITransport {
		return this.transports.find((transport) => transport.name === name);
	}

	public addTransport(newTransport: ITransport) {
		if (
			this.transports.some((transport) => transport.name === newTransport.name)
		)
			return;
		this.transports.push(newTransport);
	}

	public removeLastTransport(): ITransport {
		return this.transports.pop();
	}
}
