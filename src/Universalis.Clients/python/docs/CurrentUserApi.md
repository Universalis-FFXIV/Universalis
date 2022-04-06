# swagger_client.CurrentUserApi

All URIs are relative to *https://universalis.app*

Method | HTTP request | Description
------------- | ------------- | -------------
[**api_v2_usersme_get**](CurrentUserApi.md#api_v2_usersme_get) | **GET** /api/v2/users/@me | Retrieves the current user. Requires the session cookie to be set correctly.


# **api_v2_usersme_get**
> UserView api_v2_usersme_get()

Retrieves the current user. Requires the session cookie to be set correctly.

### Example
```python
from __future__ import print_function
import time
import swagger_client
from swagger_client.rest import ApiException
from pprint import pprint

# create an instance of the API class
api_instance = swagger_client.CurrentUserApi()

try:
    # Retrieves the current user. Requires the session cookie to be set correctly.
    api_response = api_instance.api_v2_usersme_get()
    pprint(api_response)
except ApiException as e:
    print("Exception when calling CurrentUserApi->api_v2_usersme_get: %s\n" % e)
```

### Parameters
This endpoint does not need any parameter.

### Return type

[**UserView**](UserView.md)

### Authorization

No authorization required

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: text/plain, application/json, text/json

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

