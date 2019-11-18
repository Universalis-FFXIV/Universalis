export interface MostPopularItems {
    setName: "itemPopularity";
    items?: number[];
    internal?: {
        itemID: number;
        uploadCount: number;
    };
}
