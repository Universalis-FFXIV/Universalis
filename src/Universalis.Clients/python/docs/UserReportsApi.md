# swagger_client.UserReportsApi

All URIs are relative to *https://universalis.app*

Method | HTTP request | Description
------------- | ------------- | -------------
[**api_v2_reports_id_get**](UserReportsApi.md#api_v2_reports_id_get) | **GET** /api/v2/reports/{id} | Retrieves a user report. Requires the session cookie to be set correctly.


# **api_v2_reports_id_get**
> UserReportView api_v2_reports_id_get(id)

Retrieves a user report. Requires the session cookie to be set correctly.

### Example
```python
from __future__ import print_function
import time
import swagger_client
from swagger_client.rest import ApiException
from pprint import pprint

# create an instance of the API class
api_instance = swagger_client.UserReportsApi()
id = 'id_example' # str | 

try:
    # Retrieves a user report. Requires the session cookie to be set correctly.
    api_response = api_instance.api_v2_reports_id_get(id)
    pprint(api_response)
except ApiException as e:
    print("Exception when calling UserReportsApi->api_v2_reports_id_get: %s\n" % e)
```

### Parameters

Name | Type | Description  | Notes
------------- | ------------- | ------------- | -------------
 **id** | [**str**](.md)|  | 

### Return type

[**UserReportView**](UserReportView.md)

### Authorization

No authorization required

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: text/plain, application/json, text/json

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

