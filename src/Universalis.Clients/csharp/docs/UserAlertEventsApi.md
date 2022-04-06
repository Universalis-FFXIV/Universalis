# IO.Swagger.Api.UserAlertEventsApi

All URIs are relative to *https://universalis.app*

Method | HTTP request | Description
------------- | ------------- | -------------
[**ApiV2AlertEventsIdGet**](UserAlertEventsApi.md#apiv2alerteventsidget) | **GET** /api/v2/alert-events/{id} | Retrieves an alert event. Requires the session cookie to be set correctly.


<a name="apiv2alerteventsidget"></a>
# **ApiV2AlertEventsIdGet**
> UserAlertEventView ApiV2AlertEventsIdGet (Guid? id)

Retrieves an alert event. Requires the session cookie to be set correctly.

### Example
```csharp
using System;
using System.Diagnostics;
using IO.Swagger.Api;
using IO.Swagger.Client;
using IO.Swagger.Model;

namespace Example
{
    public class ApiV2AlertEventsIdGetExample
    {
        public void main()
        {
            var apiInstance = new UserAlertEventsApi();
            var id = new Guid?(); // Guid? | 

            try
            {
                // Retrieves an alert event. Requires the session cookie to be set correctly.
                UserAlertEventView result = apiInstance.ApiV2AlertEventsIdGet(id);
                Debug.WriteLine(result);
            }
            catch (Exception e)
            {
                Debug.Print("Exception when calling UserAlertEventsApi.ApiV2AlertEventsIdGet: " + e.Message );
            }
        }
    }
}
```

### Parameters

Name | Type | Description  | Notes
------------- | ------------- | ------------- | -------------
 **id** | [**Guid?**](Guid?.md)|  | 

### Return type

[**UserAlertEventView**](UserAlertEventView.md)

### Authorization

No authorization required

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: text/plain, application/json, text/json

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

