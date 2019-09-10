# Universalis
A crowdsourced market board aggregator. Not even nearly completed, though contributions are welcome.

# Development
Requires [Node.js](https://nodejs.org/) v10 or higher, [PHP](https://www.php.net/downloads.php), [MariaDB](https://mariadb.org/download/), [Red](https://redis.io/download)[is](https://github.com/microsoftarchive/redis/releases), [Composer](https://getcomposer.org/), and [MongoDB Community Edition](https://docs.mongodb.com/manual/administration/install-community/) v4.2 or higher.

Uncomment/add in php.ini:
```
;extension=pdo_mysql
;extension=sockets
extension=redis.so
```

MariaDB commands:
```
CREATE DATABASE `dalamud`;
CREATE USER 'dalamud'@localhost IDENTIFIED BY 'dalamud';
```

Setup script:
```
npm install
git submodule init
git submodule update
cd mogboard
git submodule init
git submodule update
composer install
npm install
php bin/console doctrine:schema:create
php bin/console PopulateGameDataCommand -vvv
cd ..
npm run build
npm start
```

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
