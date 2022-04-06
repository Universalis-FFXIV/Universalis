# IO.Swagger.Api.UserAlertsApi

All URIs are relative to *https://localhost*

Method | HTTP request | Description
------------- | ------------- | -------------
[**ApiV2AlertsIdGet**](UserAlertsApi.md#apiv2alertsidget) | **GET** /api/v2/alerts/{id} | Retrieves an alert. Requires the session cookie to be set correctly.
[**ApiV2AlertsPost**](UserAlertsApi.md#apiv2alertspost) | **POST** /api/v2/alerts | Creates a new user alert.


<a name="apiv2alertsidget"></a>
# **ApiV2AlertsIdGet**
> UserAlertView ApiV2AlertsIdGet (Guid? id)

Retrieves an alert. Requires the session cookie to be set correctly.

### Example
```csharp
using System;
using System.Diagnostics;
using IO.Swagger.Api;
using IO.Swagger.Client;
using IO.Swagger.Model;

namespace Example
{
    public class ApiV2AlertsIdGetExample
    {
        public void main()
        {
            var apiInstance = new UserAlertsApi();
            var id = new Guid?(); // Guid? | 

            try
            {
                // Retrieves an alert. Requires the session cookie to be set correctly.
                UserAlertView result = apiInstance.ApiV2AlertsIdGet(id);
                Debug.WriteLine(result);
            }
            catch (Exception e)
            {
                Debug.Print("Exception when calling UserAlertsApi.ApiV2AlertsIdGet: " + e.Message );
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

[**UserAlertView**](UserAlertView.md)

### Authorization

No authorization required

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: text/plain, application/json, text/json

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

<a name="apiv2alertspost"></a>
# **ApiV2AlertsPost**
> void ApiV2AlertsPost (UserAlertCreateView body = null)

Creates a new user alert.

### Example
```csharp
using System;
using System.Diagnostics;
using IO.Swagger.Api;
using IO.Swagger.Client;
using IO.Swagger.Model;

namespace Example
{
    public class ApiV2AlertsPostExample
    {
        public void main()
        {
            var apiInstance = new UserAlertsApi();
            var body = new UserAlertCreateView(); // UserAlertCreateView | The alert parameters. (optional) 

            try
            {
                // Creates a new user alert.
                apiInstance.ApiV2AlertsPost(body);
            }
            catch (Exception e)
            {
                Debug.Print("Exception when calling UserAlertsApi.ApiV2AlertsPost: " + e.Message );
            }
        }
    }
}
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

 - **Content-Type**: application/json, text/json, application/_*+json
 - **Accept**: text/plain, application/json, text/json

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

