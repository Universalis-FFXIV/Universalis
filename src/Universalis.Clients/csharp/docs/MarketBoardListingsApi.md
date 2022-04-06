# IO.Swagger.Api.MarketBoardListingsApi

All URIs are relative to *https://localhost*

Method | HTTP request | Description
------------- | ------------- | -------------
[**ApiV2WorldOrDcItemIdsGet**](MarketBoardListingsApi.md#apiv2worldordcitemidsget) | **GET** /api/v2/{worldOrDc}/{itemIds} | Retrieves the data currently shown on the market board for the requested item and world or data center.  Item IDs can be comma-separated in order to retrieve data for multiple items at once.


<a name="apiv2worldordcitemidsget"></a>
# **ApiV2WorldOrDcItemIdsGet**
> CurrentlyShownMultiViewV2 ApiV2WorldOrDcItemIdsGet (string itemIds, string worldOrDc, string listings = null, string entries = null, string noGst = null, string hq = null, string statsWithin = null, string entriesWithin = null)

Retrieves the data currently shown on the market board for the requested item and world or data center.  Item IDs can be comma-separated in order to retrieve data for multiple items at once.

### Example
```csharp
using System;
using System.Diagnostics;
using IO.Swagger.Api;
using IO.Swagger.Client;
using IO.Swagger.Model;

namespace Example
{
    public class ApiV2WorldOrDcItemIdsGetExample
    {
        public void main()
        {
            var apiInstance = new MarketBoardListingsApi();
            var itemIds = itemIds_example;  // string | The item ID or comma-separated item IDs to retrieve data for.
            var worldOrDc = worldOrDc_example;  // string | The world or data center to retrieve data for. This may be an ID or a name.
            var listings = listings_example;  // string | The number of listings to return. By default, all listings will be returned. (optional) 
            var entries = entries_example;  // string | The number of entries to return. By default, a maximum of 5 entries will be returned. (optional) 
            var noGst = noGst_example;  // string | If the result should not have Gil sales tax (GST) factored in. GST is applied to all  consumer purchases in-game, and is separate from the retainer city tax that impacts what sellers receive.  By default, GST is factored in. Set this parameter to true or 1 to prevent this. (optional) 
            var hq = hq_example;  // string | Filter for HQ listings and entries. By default, both HQ and NQ listings and entries will be returned. (optional) 
            var statsWithin = statsWithin_example;  // string | The amount of time before now to calculate stats over, in milliseconds. By default, this is 7 days. (optional) 
            var entriesWithin = entriesWithin_example;  // string | The amount of time before now to take entries within, in seconds. Negative values will be ignored. (optional) 

            try
            {
                // Retrieves the data currently shown on the market board for the requested item and world or data center.  Item IDs can be comma-separated in order to retrieve data for multiple items at once.
                CurrentlyShownMultiViewV2 result = apiInstance.ApiV2WorldOrDcItemIdsGet(itemIds, worldOrDc, listings, entries, noGst, hq, statsWithin, entriesWithin);
                Debug.WriteLine(result);
            }
            catch (Exception e)
            {
                Debug.Print("Exception when calling MarketBoardListingsApi.ApiV2WorldOrDcItemIdsGet: " + e.Message );
            }
        }
    }
}
```

### Parameters

Name | Type | Description  | Notes
------------- | ------------- | ------------- | -------------
 **itemIds** | **string**| The item ID or comma-separated item IDs to retrieve data for. | 
 **worldOrDc** | **string**| The world or data center to retrieve data for. This may be an ID or a name. | 
 **listings** | **string**| The number of listings to return. By default, all listings will be returned. | [optional] 
 **entries** | **string**| The number of entries to return. By default, a maximum of 5 entries will be returned. | [optional] 
 **noGst** | **string**| If the result should not have Gil sales tax (GST) factored in. GST is applied to all  consumer purchases in-game, and is separate from the retainer city tax that impacts what sellers receive.  By default, GST is factored in. Set this parameter to true or 1 to prevent this. | [optional] 
 **hq** | **string**| Filter for HQ listings and entries. By default, both HQ and NQ listings and entries will be returned. | [optional] 
 **statsWithin** | **string**| The amount of time before now to calculate stats over, in milliseconds. By default, this is 7 days. | [optional] 
 **entriesWithin** | **string**| The amount of time before now to take entries within, in seconds. Negative values will be ignored. | [optional] 

### Return type

[**CurrentlyShownMultiViewV2**](CurrentlyShownMultiViewV2.md)

### Authorization

No authorization required

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: text/plain, application/json, text/json

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

