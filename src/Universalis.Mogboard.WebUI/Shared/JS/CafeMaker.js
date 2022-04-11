class CafeMaker // For some crazy reason this can't extend the XIVAPI class, it errors on load with "TypeError: Super expression must either be null or a function"
{
    get(endpoint, queries, callback)
    {
        queries = queries ? queries : {};
        
        let query = Object.keys(queries)
            .map(k => encodeURIComponent(k) + '=' + encodeURIComponent(queries[k]))
            .join('&');

        endpoint = endpoint +'?'+ query;

        fetch(`https://cafemaker.wakingsands.com${endpoint}`, { mode: 'cors' })
            .then(response => response.json())
            .then(callback)
    }

    /**
     * Fuse the results of two searches, calling the provided callback after the second search is provided.
     */
    fuse(callback) {
        let search1, search2;
        return (json) => {
            if (!search1) {
                search1 = json;
                return;
            } else {
                search2 = json;
                const fusedJson = search1;

                search2.Results.forEach(result => {
                    if (!fusedJson.Results.find(item => item.ID === result.ID)) {
                        fusedJson.Results.push(result);
                    }
                });

                fusedJson.Pagination.Results = fusedJson.Results.length
                fusedJson.Pagination.ResultsTotal = fusedJson.Results.length

                callback(fusedJson);
            }
        };
    }

    /**
     * Search for an item
     */
    search(string, callback) {
        const fusedCb = this.fuse(callback);

        const params1 = {
            indexes:     'item',
            filters:     'ItemSearchCategory.ID>=1',
            columns:     'ID,Icon,Name,LevelItem,Rarity,ItemSearchCategory.Name,ItemSearchCategory.ID,ItemKind.Name',
            string:      string.trim(),
            limit:        100,
            sort_field:  'LevelItem',
            sort_order:  'desc'
        };

        const params2 = {...params1}; // For some reason it needs to be set up the same way as XIVAPI; this should just be an implementation detail, TODO debug

        this.get(`/search`, params1, fusedCb);
        this.get(`/search`, params2, fusedCb);
    }

    /**
     * A limited search
     */
    searchLimited(string, callback) {
        const fusedCb = this.fuse(callback);

        const params1 = {
            indexes:     'item',
            filters:     'ItemSearchCategory.ID>=1',
            columns:     'ID,Name',
            string:      string.trim(),
            limit:       10,
        };

        const params2 = {...params1};

        this.get(`/search`, params1, fusedCb);
        this.get(`/search`, params2, fusedCb);
    }

    /**
     * Search for a character
     */
    searchCharacter(name, server, callback) {
        this.get(`/character/search`, {
            name: name,
            server: server
        }, callback);
    }

    /**
     * Get a specific character
     */
    getCharacter(lodestoneId, callback) {
        this.get(`/character/${lodestoneId}`, {}, callback);
    }

    /**
     * Confirm character verification state
     */
    characterVerification(lodestoneId, token, callback) {
        this.get(`/character/${lodestoneId}/verification`, {
            token: token
        }, callback);
    }

    /**
     * Return information about an item
     */
    getItem(itemId, callback) {
        this.get(`/Item/${itemId}`, {}, callback);
    }

    /**
     * Get a list of servers grouped by their data center
     */
    getServerList(callback) {
        fetch(`https://universalis.app/json/dc.json`, { mode: 'cors' })
            .then(response => response.json())
            .then(callback)
    }

    /**
     *
     */
    getMarketPrices(itemId, server, callback)
    {
        const options = {
            columns: 'Prices,Item',
            servers: server,
        };

        this.get(`/market/item/${itemId}`, options, callback);
    }
}

export default new CafeMaker;
