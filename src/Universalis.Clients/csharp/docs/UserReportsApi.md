# IO.Swagger.Api.UserReportsApi

All URIs are relative to *https://localhost*

Method | HTTP request | Description
------------- | ------------- | -------------
[**ApiV2ReportsIdGet**](UserReportsApi.md#apiv2reportsidget) | **GET** /api/v2/reports/{id} | Retrieves a user report. Requires the session cookie to be set correctly.


<a name="apiv2reportsidget"></a>
# **ApiV2ReportsIdGet**
> UserReportView ApiV2ReportsIdGet (Guid? id)

Retrieves a user report. Requires the session cookie to be set correctly.

### Example
```csharp
using System;
using System.Diagnostics;
using IO.Swagger.Api;
using IO.Swagger.Client;
using IO.Swagger.Model;

namespace Example
{
    public class ApiV2ReportsIdGetExample
    {
        public void main()
        {
            var apiInstance = new UserReportsApi();
            var id = new Guid?(); // Guid? | 

            try
            {
                // Retrieves a user report. Requires the session cookie to be set correctly.
                UserReportView result = apiInstance.ApiV2ReportsIdGet(id);
                Debug.WriteLine(result);
            }
            catch (Exception e)
            {
                Debug.Print("Exception when calling UserReportsApi.ApiV2ReportsIdGet: " + e.Message );
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

[**UserReportView**](UserReportView.md)

### Authorization

No authorization required

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: text/plain, application/json, text/json

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

