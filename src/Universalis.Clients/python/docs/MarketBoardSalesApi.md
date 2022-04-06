# swagger_client.MarketBoardSalesApi

All URIs are relative to *https://universalis.app*

Method | HTTP request | Description
------------- | ------------- | -------------
[**api_v2_history_world_or_dc_item_ids_get**](MarketBoardSalesApi.md#api_v2_history_world_or_dc_item_ids_get) | **GET** /api/v2/history/{worldOrDc}/{itemIds} | Retrieves the history data for the requested item and world or data center.  Item IDs can be comma-separated in order to retrieve data for multiple items at once.


# **api_v2_history_world_or_dc_item_ids_get**
> HistoryMultiViewV2 api_v2_history_world_or_dc_item_ids_get(item_ids, world_or_dc, entries_to_return=entries_to_return, stats_within=stats_within, entries_within=entries_within)

Retrieves the history data for the requested item and world or data center.  Item IDs can be comma-separated in order to retrieve data for multiple items at once.

### Example
```python
from __future__ import print_function
import time
import swagger_client
from swagger_client.rest import ApiException
from pprint import pprint

# create an instance of the API class
api_instance = swagger_client.MarketBoardSalesApi()
item_ids = 'item_ids_example' # str | The item ID or comma-separated item IDs to retrieve data for.
world_or_dc = 'world_or_dc_example' # str | The world or data center to retrieve data for. This may be an ID or a name.
entries_to_return = 'entries_to_return_example' # str | The number of entries to return. By default, this is set to 1800, but may be set to a maximum of 999999. (optional)
stats_within = '' # str | The amount of time before now to calculate stats over, in milliseconds. By default, this is 7 days. (optional) (default to )
entries_within = '' # str | The amount of time before now to take entries within, in seconds. Negative values will be ignored. (optional) (default to )

try:
    # Retrieves the history data for the requested item and world or data center.  Item IDs can be comma-separated in order to retrieve data for multiple items at once.
    api_response = api_instance.api_v2_history_world_or_dc_item_ids_get(item_ids, world_or_dc, entries_to_return=entries_to_return, stats_within=stats_within, entries_within=entries_within)
    pprint(api_response)
except ApiException as e:
    print("Exception when calling MarketBoardSalesApi->api_v2_history_world_or_dc_item_ids_get: %s\n" % e)
```

### Parameters

Name | Type | Description  | Notes
------------- | ------------- | ------------- | -------------
 **item_ids** | **str**| The item ID or comma-separated item IDs to retrieve data for. | 
 **world_or_dc** | **str**| The world or data center to retrieve data for. This may be an ID or a name. | 
 **entries_to_return** | **str**| The number of entries to return. By default, this is set to 1800, but may be set to a maximum of 999999. | [optional] 
 **stats_within** | **str**| The amount of time before now to calculate stats over, in milliseconds. By default, this is 7 days. | [optional] [default to ]
 **entries_within** | **str**| The amount of time before now to take entries within, in seconds. Negative values will be ignored. | [optional] [default to ]

### Return type

[**HistoryMultiViewV2**](HistoryMultiViewV2.md)

### Authorization

No authorization required

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: text/plain, application/json, text/json

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

