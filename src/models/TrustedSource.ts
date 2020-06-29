export interface TrustedSource {
	apiKey?: string; // We parse data from this object, so this needs to be optional so we can remove it
	sourceName: string;
	uploadCount: number;
}
