/* 
 * Universalis
 *
 * Welcome to the Universalis documentation page.  <br />  <br />  There is a rate limit of 20 req/s (40 req/s burst) on the API, and 10 req/s (20 req/s burst) on the website itself, if you're scraping instead.  The number of simultaneous connections per IP is capped to 8.    To map item IDs to item names or vice versa, use <a href=\"https://xivapi.com/docs/Search#search\">XIVAPI</a>.  In addition to XIVAPI, you can also get item ID mappings from <a href=\"https://lumina.xiv.dev/docs/intro.html\">Lumina</a>,  <a href=\"https://raw.githubusercontent.com/xivapi/ffxiv-datamining/master/csv/Item.csv\">this sheet</a>, or  <a href=\"https://raw.githubusercontent.com/ffxiv-teamcraft/ffxiv-teamcraft/master/apps/client/src/assets/data/items.json\">this</a> pre-made dump.    To get a mapping of world IDs to world names, use <a href=\"https://xivapi.com/World\">XIVAPI</a> or  <a href=\"https://github.com/xivapi/ffxiv-datamining/blob/master/csv/World.csv\">this sheet</a>.  The <code>key</code> column represents the world ID, and the <code>Name</code> column represents the world name.  Note that not all listed worlds are available to be used &#8212; many of the worlds in this sheet are test worlds,  or Korean worlds (Korea is unsupported at this time).    <br />  <br />  If you use this API heavily for your projects, please consider supporting the website on  <a href=\"https://liberapay.com/karashiiro\">Liberapay</a>, <a href=\"https://ko-fi.com/karashiiro\">Ko-fi</a>, or  <a href=\"https://patreon.com/universalis\">Patreon</a>, or making a one-time donation on  <a href=\"https://ko-fi.com/karashiiro\">Ko-fi</a>. Any support is appreciated!  
 *
 * OpenAPI spec version: v2
 * 
 * Generated by: https://github.com/swagger-api/swagger-codegen.git
 */

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using RestSharp;
using IO.Swagger.Client;
using IO.Swagger.Model;

namespace IO.Swagger.Api
{
    /// <summary>
    /// Represents a collection of functions to interact with the API endpoints
    /// </summary>
    public interface IMarketBoardListingsApi : IApiAccessor
    {
        #region Synchronous Operations
        /// <summary>
        /// Retrieves the data currently shown on the market board for the requested item and world or data center.  Item IDs can be comma-separated in order to retrieve data for multiple items at once.
        /// </summary>
        /// <remarks>
        /// 
        /// </remarks>
        /// <exception cref="IO.Swagger.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="itemIds">The item ID or comma-separated item IDs to retrieve data for.</param>
        /// <param name="worldOrDc">The world or data center to retrieve data for. This may be an ID or a name.</param>
        /// <param name="listings">The number of listings to return. By default, all listings will be returned. (optional)</param>
        /// <param name="entries">The number of entries to return. By default, a maximum of 5 entries will be returned. (optional)</param>
        /// <param name="noGst">If the result should not have Gil sales tax (GST) factored in. GST is applied to all  consumer purchases in-game, and is separate from the retainer city tax that impacts what sellers receive.  By default, GST is factored in. Set this parameter to true or 1 to prevent this. (optional)</param>
        /// <param name="hq">Filter for HQ listings and entries. By default, both HQ and NQ listings and entries will be returned. (optional)</param>
        /// <param name="statsWithin">The amount of time before now to calculate stats over, in milliseconds. By default, this is 7 days. (optional)</param>
        /// <param name="entriesWithin">The amount of time before now to take entries within, in seconds. Negative values will be ignored. (optional)</param>
        /// <returns>CurrentlyShownMultiViewV2</returns>
        CurrentlyShownMultiViewV2 ApiV2WorldOrDcItemIdsGet (string itemIds, string worldOrDc, string listings = null, string entries = null, string noGst = null, string hq = null, string statsWithin = null, string entriesWithin = null);

        /// <summary>
        /// Retrieves the data currently shown on the market board for the requested item and world or data center.  Item IDs can be comma-separated in order to retrieve data for multiple items at once.
        /// </summary>
        /// <remarks>
        /// 
        /// </remarks>
        /// <exception cref="IO.Swagger.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="itemIds">The item ID or comma-separated item IDs to retrieve data for.</param>
        /// <param name="worldOrDc">The world or data center to retrieve data for. This may be an ID or a name.</param>
        /// <param name="listings">The number of listings to return. By default, all listings will be returned. (optional)</param>
        /// <param name="entries">The number of entries to return. By default, a maximum of 5 entries will be returned. (optional)</param>
        /// <param name="noGst">If the result should not have Gil sales tax (GST) factored in. GST is applied to all  consumer purchases in-game, and is separate from the retainer city tax that impacts what sellers receive.  By default, GST is factored in. Set this parameter to true or 1 to prevent this. (optional)</param>
        /// <param name="hq">Filter for HQ listings and entries. By default, both HQ and NQ listings and entries will be returned. (optional)</param>
        /// <param name="statsWithin">The amount of time before now to calculate stats over, in milliseconds. By default, this is 7 days. (optional)</param>
        /// <param name="entriesWithin">The amount of time before now to take entries within, in seconds. Negative values will be ignored. (optional)</param>
        /// <returns>ApiResponse of CurrentlyShownMultiViewV2</returns>
        ApiResponse<CurrentlyShownMultiViewV2> ApiV2WorldOrDcItemIdsGetWithHttpInfo (string itemIds, string worldOrDc, string listings = null, string entries = null, string noGst = null, string hq = null, string statsWithin = null, string entriesWithin = null);
        #endregion Synchronous Operations
        #region Asynchronous Operations
        /// <summary>
        /// Retrieves the data currently shown on the market board for the requested item and world or data center.  Item IDs can be comma-separated in order to retrieve data for multiple items at once.
        /// </summary>
        /// <remarks>
        /// 
        /// </remarks>
        /// <exception cref="IO.Swagger.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="itemIds">The item ID or comma-separated item IDs to retrieve data for.</param>
        /// <param name="worldOrDc">The world or data center to retrieve data for. This may be an ID or a name.</param>
        /// <param name="listings">The number of listings to return. By default, all listings will be returned. (optional)</param>
        /// <param name="entries">The number of entries to return. By default, a maximum of 5 entries will be returned. (optional)</param>
        /// <param name="noGst">If the result should not have Gil sales tax (GST) factored in. GST is applied to all  consumer purchases in-game, and is separate from the retainer city tax that impacts what sellers receive.  By default, GST is factored in. Set this parameter to true or 1 to prevent this. (optional)</param>
        /// <param name="hq">Filter for HQ listings and entries. By default, both HQ and NQ listings and entries will be returned. (optional)</param>
        /// <param name="statsWithin">The amount of time before now to calculate stats over, in milliseconds. By default, this is 7 days. (optional)</param>
        /// <param name="entriesWithin">The amount of time before now to take entries within, in seconds. Negative values will be ignored. (optional)</param>
        /// <returns>Task of CurrentlyShownMultiViewV2</returns>
        System.Threading.Tasks.Task<CurrentlyShownMultiViewV2> ApiV2WorldOrDcItemIdsGetAsync (string itemIds, string worldOrDc, string listings = null, string entries = null, string noGst = null, string hq = null, string statsWithin = null, string entriesWithin = null);

        /// <summary>
        /// Retrieves the data currently shown on the market board for the requested item and world or data center.  Item IDs can be comma-separated in order to retrieve data for multiple items at once.
        /// </summary>
        /// <remarks>
        /// 
        /// </remarks>
        /// <exception cref="IO.Swagger.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="itemIds">The item ID or comma-separated item IDs to retrieve data for.</param>
        /// <param name="worldOrDc">The world or data center to retrieve data for. This may be an ID or a name.</param>
        /// <param name="listings">The number of listings to return. By default, all listings will be returned. (optional)</param>
        /// <param name="entries">The number of entries to return. By default, a maximum of 5 entries will be returned. (optional)</param>
        /// <param name="noGst">If the result should not have Gil sales tax (GST) factored in. GST is applied to all  consumer purchases in-game, and is separate from the retainer city tax that impacts what sellers receive.  By default, GST is factored in. Set this parameter to true or 1 to prevent this. (optional)</param>
        /// <param name="hq">Filter for HQ listings and entries. By default, both HQ and NQ listings and entries will be returned. (optional)</param>
        /// <param name="statsWithin">The amount of time before now to calculate stats over, in milliseconds. By default, this is 7 days. (optional)</param>
        /// <param name="entriesWithin">The amount of time before now to take entries within, in seconds. Negative values will be ignored. (optional)</param>
        /// <returns>Task of ApiResponse (CurrentlyShownMultiViewV2)</returns>
        System.Threading.Tasks.Task<ApiResponse<CurrentlyShownMultiViewV2>> ApiV2WorldOrDcItemIdsGetAsyncWithHttpInfo (string itemIds, string worldOrDc, string listings = null, string entries = null, string noGst = null, string hq = null, string statsWithin = null, string entriesWithin = null);
        #endregion Asynchronous Operations
    }

    /// <summary>
    /// Represents a collection of functions to interact with the API endpoints
    /// </summary>
    public partial class MarketBoardListingsApi : IMarketBoardListingsApi
    {
        private IO.Swagger.Client.ExceptionFactory _exceptionFactory = (name, response) => null;

        /// <summary>
        /// Initializes a new instance of the <see cref="MarketBoardListingsApi"/> class.
        /// </summary>
        /// <returns></returns>
        public MarketBoardListingsApi(String basePath)
        {
            this.Configuration = new IO.Swagger.Client.Configuration { BasePath = basePath };

            ExceptionFactory = IO.Swagger.Client.Configuration.DefaultExceptionFactory;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MarketBoardListingsApi"/> class
        /// using Configuration object
        /// </summary>
        /// <param name="configuration">An instance of Configuration</param>
        /// <returns></returns>
        public MarketBoardListingsApi(IO.Swagger.Client.Configuration configuration = null)
        {
            if (configuration == null) // use the default one in Configuration
                this.Configuration = IO.Swagger.Client.Configuration.Default;
            else
                this.Configuration = configuration;

            ExceptionFactory = IO.Swagger.Client.Configuration.DefaultExceptionFactory;
        }

        /// <summary>
        /// Gets the base path of the API client.
        /// </summary>
        /// <value>The base path</value>
        public String GetBasePath()
        {
            return this.Configuration.ApiClient.RestClient.BaseUrl.ToString();
        }

        /// <summary>
        /// Sets the base path of the API client.
        /// </summary>
        /// <value>The base path</value>
        [Obsolete("SetBasePath is deprecated, please do 'Configuration.ApiClient = new ApiClient(\"http://new-path\")' instead.")]
        public void SetBasePath(String basePath)
        {
            // do nothing
        }

        /// <summary>
        /// Gets or sets the configuration object
        /// </summary>
        /// <value>An instance of the Configuration</value>
        public IO.Swagger.Client.Configuration Configuration {get; set;}

        /// <summary>
        /// Provides a factory method hook for the creation of exceptions.
        /// </summary>
        public IO.Swagger.Client.ExceptionFactory ExceptionFactory
        {
            get
            {
                if (_exceptionFactory != null && _exceptionFactory.GetInvocationList().Length > 1)
                {
                    throw new InvalidOperationException("Multicast delegate for ExceptionFactory is unsupported.");
                }
                return _exceptionFactory;
            }
            set { _exceptionFactory = value; }
        }

        /// <summary>
        /// Gets the default header.
        /// </summary>
        /// <returns>Dictionary of HTTP header</returns>
        [Obsolete("DefaultHeader is deprecated, please use Configuration.DefaultHeader instead.")]
        public IDictionary<String, String> DefaultHeader()
        {
            return new ReadOnlyDictionary<string, string>(this.Configuration.DefaultHeader);
        }

        /// <summary>
        /// Add default header.
        /// </summary>
        /// <param name="key">Header field name.</param>
        /// <param name="value">Header field value.</param>
        /// <returns></returns>
        [Obsolete("AddDefaultHeader is deprecated, please use Configuration.AddDefaultHeader instead.")]
        public void AddDefaultHeader(string key, string value)
        {
            this.Configuration.AddDefaultHeader(key, value);
        }

        /// <summary>
        /// Retrieves the data currently shown on the market board for the requested item and world or data center.  Item IDs can be comma-separated in order to retrieve data for multiple items at once. 
        /// </summary>
        /// <exception cref="IO.Swagger.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="itemIds">The item ID or comma-separated item IDs to retrieve data for.</param>
        /// <param name="worldOrDc">The world or data center to retrieve data for. This may be an ID or a name.</param>
        /// <param name="listings">The number of listings to return. By default, all listings will be returned. (optional)</param>
        /// <param name="entries">The number of entries to return. By default, a maximum of 5 entries will be returned. (optional)</param>
        /// <param name="noGst">If the result should not have Gil sales tax (GST) factored in. GST is applied to all  consumer purchases in-game, and is separate from the retainer city tax that impacts what sellers receive.  By default, GST is factored in. Set this parameter to true or 1 to prevent this. (optional)</param>
        /// <param name="hq">Filter for HQ listings and entries. By default, both HQ and NQ listings and entries will be returned. (optional)</param>
        /// <param name="statsWithin">The amount of time before now to calculate stats over, in milliseconds. By default, this is 7 days. (optional)</param>
        /// <param name="entriesWithin">The amount of time before now to take entries within, in seconds. Negative values will be ignored. (optional)</param>
        /// <returns>CurrentlyShownMultiViewV2</returns>
        public CurrentlyShownMultiViewV2 ApiV2WorldOrDcItemIdsGet (string itemIds, string worldOrDc, string listings = null, string entries = null, string noGst = null, string hq = null, string statsWithin = null, string entriesWithin = null)
        {
             ApiResponse<CurrentlyShownMultiViewV2> localVarResponse = ApiV2WorldOrDcItemIdsGetWithHttpInfo(itemIds, worldOrDc, listings, entries, noGst, hq, statsWithin, entriesWithin);
             return localVarResponse.Data;
        }

        /// <summary>
        /// Retrieves the data currently shown on the market board for the requested item and world or data center.  Item IDs can be comma-separated in order to retrieve data for multiple items at once. 
        /// </summary>
        /// <exception cref="IO.Swagger.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="itemIds">The item ID or comma-separated item IDs to retrieve data for.</param>
        /// <param name="worldOrDc">The world or data center to retrieve data for. This may be an ID or a name.</param>
        /// <param name="listings">The number of listings to return. By default, all listings will be returned. (optional)</param>
        /// <param name="entries">The number of entries to return. By default, a maximum of 5 entries will be returned. (optional)</param>
        /// <param name="noGst">If the result should not have Gil sales tax (GST) factored in. GST is applied to all  consumer purchases in-game, and is separate from the retainer city tax that impacts what sellers receive.  By default, GST is factored in. Set this parameter to true or 1 to prevent this. (optional)</param>
        /// <param name="hq">Filter for HQ listings and entries. By default, both HQ and NQ listings and entries will be returned. (optional)</param>
        /// <param name="statsWithin">The amount of time before now to calculate stats over, in milliseconds. By default, this is 7 days. (optional)</param>
        /// <param name="entriesWithin">The amount of time before now to take entries within, in seconds. Negative values will be ignored. (optional)</param>
        /// <returns>ApiResponse of CurrentlyShownMultiViewV2</returns>
        public ApiResponse< CurrentlyShownMultiViewV2 > ApiV2WorldOrDcItemIdsGetWithHttpInfo (string itemIds, string worldOrDc, string listings = null, string entries = null, string noGst = null, string hq = null, string statsWithin = null, string entriesWithin = null)
        {
            // verify the required parameter 'itemIds' is set
            if (itemIds == null)
                throw new ApiException(400, "Missing required parameter 'itemIds' when calling MarketBoardListingsApi->ApiV2WorldOrDcItemIdsGet");
            // verify the required parameter 'worldOrDc' is set
            if (worldOrDc == null)
                throw new ApiException(400, "Missing required parameter 'worldOrDc' when calling MarketBoardListingsApi->ApiV2WorldOrDcItemIdsGet");

            var localVarPath = "/api/v2/{worldOrDc}/{itemIds}";
            var localVarPathParams = new Dictionary<String, String>();
            var localVarQueryParams = new List<KeyValuePair<String, String>>();
            var localVarHeaderParams = new Dictionary<String, String>(this.Configuration.DefaultHeader);
            var localVarFormParams = new Dictionary<String, String>();
            var localVarFileParams = new Dictionary<String, FileParameter>();
            Object localVarPostBody = null;

            // to determine the Content-Type header
            String[] localVarHttpContentTypes = new String[] {
            };
            String localVarHttpContentType = this.Configuration.ApiClient.SelectHeaderContentType(localVarHttpContentTypes);

            // to determine the Accept header
            String[] localVarHttpHeaderAccepts = new String[] {
                "text/plain",
                "application/json",
                "text/json"
            };
            String localVarHttpHeaderAccept = this.Configuration.ApiClient.SelectHeaderAccept(localVarHttpHeaderAccepts);
            if (localVarHttpHeaderAccept != null)
                localVarHeaderParams.Add("Accept", localVarHttpHeaderAccept);

            if (itemIds != null) localVarPathParams.Add("itemIds", this.Configuration.ApiClient.ParameterToString(itemIds)); // path parameter
            if (worldOrDc != null) localVarPathParams.Add("worldOrDc", this.Configuration.ApiClient.ParameterToString(worldOrDc)); // path parameter
            if (listings != null) localVarQueryParams.AddRange(this.Configuration.ApiClient.ParameterToKeyValuePairs("", "listings", listings)); // query parameter
            if (entries != null) localVarQueryParams.AddRange(this.Configuration.ApiClient.ParameterToKeyValuePairs("", "entries", entries)); // query parameter
            if (noGst != null) localVarQueryParams.AddRange(this.Configuration.ApiClient.ParameterToKeyValuePairs("", "noGst", noGst)); // query parameter
            if (hq != null) localVarQueryParams.AddRange(this.Configuration.ApiClient.ParameterToKeyValuePairs("", "hq", hq)); // query parameter
            if (statsWithin != null) localVarQueryParams.AddRange(this.Configuration.ApiClient.ParameterToKeyValuePairs("", "statsWithin", statsWithin)); // query parameter
            if (entriesWithin != null) localVarQueryParams.AddRange(this.Configuration.ApiClient.ParameterToKeyValuePairs("", "entriesWithin", entriesWithin)); // query parameter


            // make the HTTP request
            IRestResponse localVarResponse = (IRestResponse) this.Configuration.ApiClient.CallApi(localVarPath,
                Method.GET, localVarQueryParams, localVarPostBody, localVarHeaderParams, localVarFormParams, localVarFileParams,
                localVarPathParams, localVarHttpContentType);

            int localVarStatusCode = (int) localVarResponse.StatusCode;

            if (ExceptionFactory != null)
            {
                Exception exception = ExceptionFactory("ApiV2WorldOrDcItemIdsGet", localVarResponse);
                if (exception != null) throw exception;
            }

            return new ApiResponse<CurrentlyShownMultiViewV2>(localVarStatusCode,
                localVarResponse.Headers.ToDictionary(x => x.Name, x => x.Value.ToString()),
                (CurrentlyShownMultiViewV2) this.Configuration.ApiClient.Deserialize(localVarResponse, typeof(CurrentlyShownMultiViewV2)));
        }

        /// <summary>
        /// Retrieves the data currently shown on the market board for the requested item and world or data center.  Item IDs can be comma-separated in order to retrieve data for multiple items at once. 
        /// </summary>
        /// <exception cref="IO.Swagger.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="itemIds">The item ID or comma-separated item IDs to retrieve data for.</param>
        /// <param name="worldOrDc">The world or data center to retrieve data for. This may be an ID or a name.</param>
        /// <param name="listings">The number of listings to return. By default, all listings will be returned. (optional)</param>
        /// <param name="entries">The number of entries to return. By default, a maximum of 5 entries will be returned. (optional)</param>
        /// <param name="noGst">If the result should not have Gil sales tax (GST) factored in. GST is applied to all  consumer purchases in-game, and is separate from the retainer city tax that impacts what sellers receive.  By default, GST is factored in. Set this parameter to true or 1 to prevent this. (optional)</param>
        /// <param name="hq">Filter for HQ listings and entries. By default, both HQ and NQ listings and entries will be returned. (optional)</param>
        /// <param name="statsWithin">The amount of time before now to calculate stats over, in milliseconds. By default, this is 7 days. (optional)</param>
        /// <param name="entriesWithin">The amount of time before now to take entries within, in seconds. Negative values will be ignored. (optional)</param>
        /// <returns>Task of CurrentlyShownMultiViewV2</returns>
        public async System.Threading.Tasks.Task<CurrentlyShownMultiViewV2> ApiV2WorldOrDcItemIdsGetAsync (string itemIds, string worldOrDc, string listings = null, string entries = null, string noGst = null, string hq = null, string statsWithin = null, string entriesWithin = null)
        {
             ApiResponse<CurrentlyShownMultiViewV2> localVarResponse = await ApiV2WorldOrDcItemIdsGetAsyncWithHttpInfo(itemIds, worldOrDc, listings, entries, noGst, hq, statsWithin, entriesWithin);
             return localVarResponse.Data;

        }

        /// <summary>
        /// Retrieves the data currently shown on the market board for the requested item and world or data center.  Item IDs can be comma-separated in order to retrieve data for multiple items at once. 
        /// </summary>
        /// <exception cref="IO.Swagger.Client.ApiException">Thrown when fails to make API call</exception>
        /// <param name="itemIds">The item ID or comma-separated item IDs to retrieve data for.</param>
        /// <param name="worldOrDc">The world or data center to retrieve data for. This may be an ID or a name.</param>
        /// <param name="listings">The number of listings to return. By default, all listings will be returned. (optional)</param>
        /// <param name="entries">The number of entries to return. By default, a maximum of 5 entries will be returned. (optional)</param>
        /// <param name="noGst">If the result should not have Gil sales tax (GST) factored in. GST is applied to all  consumer purchases in-game, and is separate from the retainer city tax that impacts what sellers receive.  By default, GST is factored in. Set this parameter to true or 1 to prevent this. (optional)</param>
        /// <param name="hq">Filter for HQ listings and entries. By default, both HQ and NQ listings and entries will be returned. (optional)</param>
        /// <param name="statsWithin">The amount of time before now to calculate stats over, in milliseconds. By default, this is 7 days. (optional)</param>
        /// <param name="entriesWithin">The amount of time before now to take entries within, in seconds. Negative values will be ignored. (optional)</param>
        /// <returns>Task of ApiResponse (CurrentlyShownMultiViewV2)</returns>
        public async System.Threading.Tasks.Task<ApiResponse<CurrentlyShownMultiViewV2>> ApiV2WorldOrDcItemIdsGetAsyncWithHttpInfo (string itemIds, string worldOrDc, string listings = null, string entries = null, string noGst = null, string hq = null, string statsWithin = null, string entriesWithin = null)
        {
            // verify the required parameter 'itemIds' is set
            if (itemIds == null)
                throw new ApiException(400, "Missing required parameter 'itemIds' when calling MarketBoardListingsApi->ApiV2WorldOrDcItemIdsGet");
            // verify the required parameter 'worldOrDc' is set
            if (worldOrDc == null)
                throw new ApiException(400, "Missing required parameter 'worldOrDc' when calling MarketBoardListingsApi->ApiV2WorldOrDcItemIdsGet");

            var localVarPath = "/api/v2/{worldOrDc}/{itemIds}";
            var localVarPathParams = new Dictionary<String, String>();
            var localVarQueryParams = new List<KeyValuePair<String, String>>();
            var localVarHeaderParams = new Dictionary<String, String>(this.Configuration.DefaultHeader);
            var localVarFormParams = new Dictionary<String, String>();
            var localVarFileParams = new Dictionary<String, FileParameter>();
            Object localVarPostBody = null;

            // to determine the Content-Type header
            String[] localVarHttpContentTypes = new String[] {
            };
            String localVarHttpContentType = this.Configuration.ApiClient.SelectHeaderContentType(localVarHttpContentTypes);

            // to determine the Accept header
            String[] localVarHttpHeaderAccepts = new String[] {
                "text/plain",
                "application/json",
                "text/json"
            };
            String localVarHttpHeaderAccept = this.Configuration.ApiClient.SelectHeaderAccept(localVarHttpHeaderAccepts);
            if (localVarHttpHeaderAccept != null)
                localVarHeaderParams.Add("Accept", localVarHttpHeaderAccept);

            if (itemIds != null) localVarPathParams.Add("itemIds", this.Configuration.ApiClient.ParameterToString(itemIds)); // path parameter
            if (worldOrDc != null) localVarPathParams.Add("worldOrDc", this.Configuration.ApiClient.ParameterToString(worldOrDc)); // path parameter
            if (listings != null) localVarQueryParams.AddRange(this.Configuration.ApiClient.ParameterToKeyValuePairs("", "listings", listings)); // query parameter
            if (entries != null) localVarQueryParams.AddRange(this.Configuration.ApiClient.ParameterToKeyValuePairs("", "entries", entries)); // query parameter
            if (noGst != null) localVarQueryParams.AddRange(this.Configuration.ApiClient.ParameterToKeyValuePairs("", "noGst", noGst)); // query parameter
            if (hq != null) localVarQueryParams.AddRange(this.Configuration.ApiClient.ParameterToKeyValuePairs("", "hq", hq)); // query parameter
            if (statsWithin != null) localVarQueryParams.AddRange(this.Configuration.ApiClient.ParameterToKeyValuePairs("", "statsWithin", statsWithin)); // query parameter
            if (entriesWithin != null) localVarQueryParams.AddRange(this.Configuration.ApiClient.ParameterToKeyValuePairs("", "entriesWithin", entriesWithin)); // query parameter


            // make the HTTP request
            IRestResponse localVarResponse = (IRestResponse) await this.Configuration.ApiClient.CallApiAsync(localVarPath,
                Method.GET, localVarQueryParams, localVarPostBody, localVarHeaderParams, localVarFormParams, localVarFileParams,
                localVarPathParams, localVarHttpContentType);

            int localVarStatusCode = (int) localVarResponse.StatusCode;

            if (ExceptionFactory != null)
            {
                Exception exception = ExceptionFactory("ApiV2WorldOrDcItemIdsGet", localVarResponse);
                if (exception != null) throw exception;
            }

            return new ApiResponse<CurrentlyShownMultiViewV2>(localVarStatusCode,
                localVarResponse.Headers.ToDictionary(x => x.Name, x => x.Value.ToString()),
                (CurrentlyShownMultiViewV2) this.Configuration.ApiClient.Deserialize(localVarResponse, typeof(CurrentlyShownMultiViewV2)));
        }

    }
}
