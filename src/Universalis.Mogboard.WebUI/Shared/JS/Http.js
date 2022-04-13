class Http
{
    /**
     * Get all the item for all categories for the users language
     *
     * @param language int
     * @param callback function
     */
    getItemCategories(language, callback)
    {
        const url = "/data/categories_[lang].js".replace("[lang]", language);

        fetch(url, { mode: "cors" })
            .then(response => response.json())
            .then(callback);
    }

    /**
     * Get prices for an item
     *
     * @param server
     * @param itemId
     * @param callback
     */
    getItemPrices(server, itemId, callback)
    {
        const url = mog.url_item_price.replace("-server-", server).replace("-id-", itemId);

        fetch(url, { mode: "cors" })
            .then(response => response.text())
            .then(callback);
    }

    /**
     * Get price history of an item
     *
     * @param server
     * @param itemId
     * @param callback
     */
    getItemHistory(server, itemId, callback)
    {
        const url = mog.url_item_history.replace("-server-", server).replace("-id-", itemId);

        fetch(url, { mode: "cors" })
            .then(response => response.text())
            .then(callback);
    }

    /**
     * Get the price for an item across multiple worlds, by default this will select worlds
     * based on the users server data-center, however they can customise this if they're logged in.
     *
     * @param server
     * @param itemId
     * @param callback
     */
    getItemPricesCrossWorld(server, itemId, callback)
    {
        const url = mog.url_item_cross_world.replace("-server-", server).replace("-id-", itemId);

        fetch(url, { mode: "cors" })
            .then(response => response.text())
            .then(callback);
    }
}

export default new Http;
