# swagger_client.UserCharactersApi

All URIs are relative to *https://universalis.app*

Method | HTTP request | Description
------------- | ------------- | -------------
[**api_v2_characters_id_get**](UserCharactersApi.md#api_v2_characters_id_get) | **GET** /api/v2/characters/{id} | Retrieves a characters. Requires the session cookie to be set correctly.


# **api_v2_characters_id_get**
> UserCharacterView api_v2_characters_id_get(id)

Retrieves a characters. Requires the session cookie to be set correctly.

### Example
```python
from __future__ import print_function
import time
import swagger_client
from swagger_client.rest import ApiException
from pprint import pprint

# create an instance of the API class
api_instance = swagger_client.UserCharactersApi()
id = 'id_example' # str | 

try:
    # Retrieves a characters. Requires the session cookie to be set correctly.
    api_response = api_instance.api_v2_characters_id_get(id)
    pprint(api_response)
except ApiException as e:
    print("Exception when calling UserCharactersApi->api_v2_characters_id_get: %s\n" % e)
```

### Parameters

Name | Type | Description  | Notes
------------- | ------------- | ------------- | -------------
 **id** | [**str**](.md)|  | 

### Return type

[**UserCharacterView**](UserCharacterView.md)

### Authorization

No authorization required

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: text/plain, application/json, text/json

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

