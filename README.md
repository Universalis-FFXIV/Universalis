# Universalis
A crowdsourced market board aggregator. Not even nearly completed, though contributions are welcome.

# Development
Requires Node.js v10 or higher, and MongoDB Community Edition v4.2 or higher.

Clone the repo, and then `npm install` to download the dependencies, followed by `npm run build` to compile. Use `npm run start-dev` to test it on localhost.

# Uploads
Listings upload format (JSON):

```
{
    worldID: number;
    itemID: number;
    uploaderID: string | number;
    listings: [{
        listingID: number;
        hq: boolean;
        materia?: [{
            slotID: number;
            materiaID: number;
        }];
        pricePerUnit: number;
        quantity: number;
        retainerID: number;
        retainerName: string;
        retainerCity: string;
        creatorName?: string;
        sellerID: number;
        creatorID?: number;
        lastReviewTime: number;
        dyeID: number;
    }];
}
```

History upload format (JSON):

```
{
    worldID: number;
    itemID: number;
    uploaderID: string | number;
    entries: [{
        hq: boolean;
        pricePerUnit: number;
        quantity: number;
        buyerName: string;
        timestamp: number;
        sellerID: number;
    }];
}
```

Crafter upload format (JSON):

```
{
    contentID: number;
    characterName: string;
}
```
