# swagger_client.MarketBoardListingsApi

All URIs are relative to *https://universalis.app*

Method | HTTP request | Description
------------- | ------------- | -------------
[**api_v2_world_or_dc_item_ids_get**](MarketBoardListingsApi.md#api_v2_world_or_dc_item_ids_get) | **GET** /api/v2/{worldOrDc}/{itemIds} | Retrieves the data currently shown on the market board for the requested item and world or data center.  Item IDs can be comma-separated in order to retrieve data for multiple items at once.


# **api_v2_world_or_dc_item_ids_get**
> CurrentlyShownMultiViewV2 api_v2_world_or_dc_item_ids_get(item_ids, world_or_dc, listings=listings, entries=entries, no_gst=no_gst, hq=hq, stats_within=stats_within, entries_within=entries_within)

Retrieves the data currently shown on the market board for the requested item and world or data center.  Item IDs can be comma-separated in order to retrieve data for multiple items at once.

### Example
```python
from __future__ import print_function
import time
import swagger_client
from swagger_client.rest import ApiException
from pprint import pprint

# create an instance of the API class
api_instance = swagger_client.MarketBoardListingsApi()
item_ids = 'item_ids_example' # str | The item ID or comma-separated item IDs to retrieve data for.
world_or_dc = 'world_or_dc_example' # str | The world or data center to retrieve data for. This may be an ID or a name.
listings = '' # str | The number of listings to return. By default, all listings will be returned. (optional) (default to )
entries = '' # str | The number of entries to return. By default, a maximum of 5 entries will be returned. (optional) (default to )
no_gst = '' # str | If the result should not have Gil sales tax (GST) factored in. GST is applied to all  consumer purchases in-game, and is separate from the retainer city tax that impacts what sellers receive.  By default, GST is factored in. Set this parameter to true or 1 to prevent this. (optional) (default to )
hq = '' # str | Filter for HQ listings and entries. By default, both HQ and NQ listings and entries will be returned. (optional) (default to )
stats_within = '' # str | The amount of time before now to calculate stats over, in milliseconds. By default, this is 7 days. (optional) (default to )
entries_within = '' # str | The amount of time before now to take entries within, in seconds. Negative values will be ignored. (optional) (default to )

try:
    # Retrieves the data currently shown on the market board for the requested item and world or data center.  Item IDs can be comma-separated in order to retrieve data for multiple items at once.
    api_response = api_instance.api_v2_world_or_dc_item_ids_get(item_ids, world_or_dc, listings=listings, entries=entries, no_gst=no_gst, hq=hq, stats_within=stats_within, entries_within=entries_within)
    pprint(api_response)
except ApiException as e:
    print("Exception when calling MarketBoardListingsApi->api_v2_world_or_dc_item_ids_get: %s\n" % e)
```

### Parameters

Name | Type | Description  | Notes
------------- | ------------- | ------------- | -------------
 **item_ids** | **str**| The item ID or comma-separated item IDs to retrieve data for. | 
 **world_or_dc** | **str**| The world or data center to retrieve data for. This may be an ID or a name. | 
 **listings** | **str**| The number of listings to return. By default, all listings will be returned. | [optional] [default to ]
 **entries** | **str**| The number of entries to return. By default, a maximum of 5 entries will be returned. | [optional] [default to ]
 **no_gst** | **str**| If the result should not have Gil sales tax (GST) factored in. GST is applied to all  consumer purchases in-game, and is separate from the retainer city tax that impacts what sellers receive.  By default, GST is factored in. Set this parameter to true or 1 to prevent this. | [optional] [default to ]
 **hq** | **str**| Filter for HQ listings and entries. By default, both HQ and NQ listings and entries will be returned. | [optional] [default to ]
 **stats_within** | **str**| The amount of time before now to calculate stats over, in milliseconds. By default, this is 7 days. | [optional] [default to ]
 **entries_within** | **str**| The amount of time before now to take entries within, in seconds. Negative values will be ignored. | [optional] [default to ]

### Return type

[**CurrentlyShownMultiViewV2**](CurrentlyShownMultiViewV2.md)

### Authorization

No authorization required

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: text/plain, application/json, text/json

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

