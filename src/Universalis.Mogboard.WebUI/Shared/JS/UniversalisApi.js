class UniversalisApi
{
    get(endpoint, queries, callback)
    {
        queries = queries ? queries : {};
        
        let query = Object.keys(queries)
            .map(k => encodeURIComponent(k) + '=' + encodeURIComponent(queries[k]))
            .join('&');

        endpoint = endpoint +'?'+ query;

        fetch(`https://universalis.app/api${endpoint}`)
            .then(response => response.json())
            .then(callback)
    }

    /**
     * Returns extended history data for the provided world/DC and item.
     */
    extendedHistory(worldDc, itemId, callback)
    {
        this.get(`/history/${worldDc}/${itemId}`, null, callback);
    }
}

export default new UniversalisApi;
