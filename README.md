# Universalis
A crowdsourced market board aggregator. Not necessarily ever intended for widespread use, simply an experiment at this time.

# Development
Clone the repo, and then `npm install` to download the dependencies. Use `npm run-script start-dev` to test it on localhost.

# Uploads
Listings upload format (JSON):

```
{
    worldID: number;
    itemID: number;
    listings: [{
        hq: 1 | 0;
        materia?: number[];
        pricePerUnit: number;
        quantity: number;
        retainerName: string;
        retainerCity: string;
    }];
}
```

Not yet implemented:

History upload format (JSON):

```
{
    worldID: number;
    itemID: number;
    entries: [{
        hq: 1 | 0;
        pricePerUnit: number;
        quantity: number;
        buyerName: string;
        timestamp: number;
    }];
}
```
