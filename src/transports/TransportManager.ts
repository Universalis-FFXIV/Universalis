import { ITransport } from "../models/transports/ITransport";

// Singleton transport manager.
export class TransportManager {
    transports: ITransport[];

    constructor() {
        this.transports = [];
    }

    getTransport(name: string): ITransport {
        return this.transports.find((transport) => transport.name === name);
    }

    addTransport(newTransport: ITransport) {
        if (this.transports.some((transport) => transport.name === newTransport.name)) return;
        this.transports.push(newTransport);
    }

    removeLastTransport(): ITransport {
        return this.transports.pop();
    }
}