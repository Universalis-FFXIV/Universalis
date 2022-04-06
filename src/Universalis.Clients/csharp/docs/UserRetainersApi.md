# IO.Swagger.Api.UserRetainersApi

All URIs are relative to *https://universalis.app*

Method | HTTP request | Description
------------- | ------------- | -------------
[**ApiV2RetainersIdGet**](UserRetainersApi.md#apiv2retainersidget) | **GET** /api/v2/retainers/{id} | Retrieves a retainer. Requires the session cookie to be set correctly.


<a name="apiv2retainersidget"></a>
# **ApiV2RetainersIdGet**
> UserRetainerView ApiV2RetainersIdGet (Guid? id)

Retrieves a retainer. Requires the session cookie to be set correctly.

### Example
```csharp
using System;
using System.Diagnostics;
using IO.Swagger.Api;
using IO.Swagger.Client;
using IO.Swagger.Model;

namespace Example
{
    public class ApiV2RetainersIdGetExample
    {
        public void main()
        {
            var apiInstance = new UserRetainersApi();
            var id = new Guid?(); // Guid? | 

            try
            {
                // Retrieves a retainer. Requires the session cookie to be set correctly.
                UserRetainerView result = apiInstance.ApiV2RetainersIdGet(id);
                Debug.WriteLine(result);
            }
            catch (Exception e)
            {
                Debug.Print("Exception when calling UserRetainersApi.ApiV2RetainersIdGet: " + e.Message );
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

[**UserRetainerView**](UserRetainerView.md)

### Authorization

No authorization required

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: text/plain, application/json, text/json

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

