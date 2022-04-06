# IO.Swagger.Api.MarketBoardSalesApi

All URIs are relative to *https://universalis.app*

Method | HTTP request | Description
------------- | ------------- | -------------
[**ApiV2HistoryWorldOrDcItemIdsGet**](MarketBoardSalesApi.md#apiv2historyworldordcitemidsget) | **GET** /api/v2/history/{worldOrDc}/{itemIds} | Retrieves the history data for the requested item and world or data center.  Item IDs can be comma-separated in order to retrieve data for multiple items at once.


<a name="apiv2historyworldordcitemidsget"></a>
# **ApiV2HistoryWorldOrDcItemIdsGet**
> HistoryMultiViewV2 ApiV2HistoryWorldOrDcItemIdsGet (string itemIds, string worldOrDc, string entriesToReturn = null, string statsWithin = null, string entriesWithin = null)

Retrieves the history data for the requested item and world or data center.  Item IDs can be comma-separated in order to retrieve data for multiple items at once.

### Example
```csharp
using System;
using System.Diagnostics;
using IO.Swagger.Api;
using IO.Swagger.Client;
using IO.Swagger.Model;

namespace Example
{
    public class ApiV2HistoryWorldOrDcItemIdsGetExample
    {
        public void main()
        {
            var apiInstance = new MarketBoardSalesApi();
            var itemIds = itemIds_example;  // string | The item ID or comma-separated item IDs to retrieve data for.
            var worldOrDc = worldOrDc_example;  // string | The world or data center to retrieve data for. This may be an ID or a name.
            var entriesToReturn = entriesToReturn_example;  // string | The number of entries to return. By default, this is set to 1800, but may be set to a maximum of 999999. (optional) 
            var statsWithin = statsWithin_example;  // string | The amount of time before now to calculate stats over, in milliseconds. By default, this is 7 days. (optional) 
            var entriesWithin = entriesWithin_example;  // string | The amount of time before now to take entries within, in seconds. Negative values will be ignored. (optional) 

            try
            {
                // Retrieves the history data for the requested item and world or data center.  Item IDs can be comma-separated in order to retrieve data for multiple items at once.
                HistoryMultiViewV2 result = apiInstance.ApiV2HistoryWorldOrDcItemIdsGet(itemIds, worldOrDc, entriesToReturn, statsWithin, entriesWithin);
                Debug.WriteLine(result);
            }
            catch (Exception e)
            {
                Debug.Print("Exception when calling MarketBoardSalesApi.ApiV2HistoryWorldOrDcItemIdsGet: " + e.Message );
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
 **entriesToReturn** | **string**| The number of entries to return. By default, this is set to 1800, but may be set to a maximum of 999999. | [optional] 
 **statsWithin** | **string**| The amount of time before now to calculate stats over, in milliseconds. By default, this is 7 days. | [optional] 
 **entriesWithin** | **string**| The amount of time before now to take entries within, in seconds. Negative values will be ignored. | [optional] 

### Return type

[**HistoryMultiViewV2**](HistoryMultiViewV2.md)

### Authorization

No authorization required

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: text/plain, application/json, text/json

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

