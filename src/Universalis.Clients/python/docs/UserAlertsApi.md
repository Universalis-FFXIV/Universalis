# swagger_client.UserAlertsApi

All URIs are relative to *https://universalis.app*

Method | HTTP request | Description
------------- | ------------- | -------------
[**api_v2_alerts_id_get**](UserAlertsApi.md#api_v2_alerts_id_get) | **GET** /api/v2/alerts/{id} | Retrieves an alert. Requires the session cookie to be set correctly.
[**api_v2_alerts_post**](UserAlertsApi.md#api_v2_alerts_post) | **POST** /api/v2/alerts | Creates a new user alert.


# **api_v2_alerts_id_get**
> UserAlertView api_v2_alerts_id_get(id)

Retrieves an alert. Requires the session cookie to be set correctly.

### Example
```python
from __future__ import print_function
import time
import swagger_client
from swagger_client.rest import ApiException
from pprint import pprint

# create an instance of the API class
api_instance = swagger_client.UserAlertsApi()
id = 'id_example' # str | 

try:
    # Retrieves an alert. Requires the session cookie to be set correctly.
    api_response = api_instance.api_v2_alerts_id_get(id)
    pprint(api_response)
except ApiException as e:
    print("Exception when calling UserAlertsApi->api_v2_alerts_id_get: %s\n" % e)
```

### Parameters

Name | Type | Description  | Notes
------------- | ------------- | ------------- | -------------
 **id** | [**str**](.md)|  | 

### Return type

[**UserAlertView**](UserAlertView.md)

### Authorization

No authorization required

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: text/plain, application/json, text/json

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

# **api_v2_alerts_post**
> api_v2_alerts_post(body=body)

Creates a new user alert.

### Example
```python
from __future__ import print_function
import time
import swagger_client
from swagger_client.rest import ApiException
from pprint import pprint

# create an instance of the API class
api_instance = swagger_client.UserAlertsApi()
body = swagger_client.UserAlertCreateView() # UserAlertCreateView | The alert parameters. (optional)

try:
    # Creates a new user alert.
    api_instance.api_v2_alerts_post(body=body)
except ApiException as e:
    print("Exception when calling UserAlertsApi->api_v2_alerts_post: %s\n" % e)
```

### Parameters

Name | Type | Description  | Notes
------------- | ------------- | ------------- | -------------
 **body** | [**UserAlertCreateView**](UserAlertCreateView.md)| The alert parameters. | [optional] 

### Return type

void (empty response body)

### Authorization

No authorization required

### HTTP request headers

 - **Content-Type**: application/json, text/json, application/*+json
 - **Accept**: text/plain, application/json, text/json

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

