# swagger_client.UserListsApi

All URIs are relative to *https://universalis.app*

Method | HTTP request | Description
------------- | ------------- | -------------
[**api_v2_lists_list_id_get**](UserListsApi.md#api_v2_lists_list_id_get) | **GET** /api/v2/lists/{listId} | Retrieves a user list.


# **api_v2_lists_list_id_get**
> UserListView api_v2_lists_list_id_get(list_id)

Retrieves a user list.

### Example
```python
from __future__ import print_function
import time
import swagger_client
from swagger_client.rest import ApiException
from pprint import pprint

# create an instance of the API class
api_instance = swagger_client.UserListsApi()
list_id = 'list_id_example' # str | The ID of the list to retrieve.

try:
    # Retrieves a user list.
    api_response = api_instance.api_v2_lists_list_id_get(list_id)
    pprint(api_response)
except ApiException as e:
    print("Exception when calling UserListsApi->api_v2_lists_list_id_get: %s\n" % e)
```

### Parameters

Name | Type | Description  | Notes
------------- | ------------- | ------------- | -------------
 **list_id** | [**str**](.md)| The ID of the list to retrieve. | 

### Return type

[**UserListView**](UserListView.md)

### Authorization

No authorization required

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: text/plain, application/json, text/json

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

