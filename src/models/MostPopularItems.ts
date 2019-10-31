export interface MostPopularItems {
    setName: string;
    items?: number[];
    internal?: {
        itemID: number;
        uploadCount: number;
    };
}
