# IO.Swagger.Api.CurrentUserApi

All URIs are relative to *https://universalis.app*

Method | HTTP request | Description
------------- | ------------- | -------------
[**ApiV2UsersmeGet**](CurrentUserApi.md#apiv2usersmeget) | **GET** /api/v2/users/@me | Retrieves the current user. Requires the session cookie to be set correctly.


<a name="apiv2usersmeget"></a>
# **ApiV2UsersmeGet**
> UserView ApiV2UsersmeGet ()

Retrieves the current user. Requires the session cookie to be set correctly.

### Example
```csharp
using System;
using System.Diagnostics;
using IO.Swagger.Api;
using IO.Swagger.Client;
using IO.Swagger.Model;

namespace Example
{
    public class ApiV2UsersmeGetExample
    {
        public void main()
        {
            var apiInstance = new CurrentUserApi();

            try
            {
                // Retrieves the current user. Requires the session cookie to be set correctly.
                UserView result = apiInstance.ApiV2UsersmeGet();
                Debug.WriteLine(result);
            }
            catch (Exception e)
            {
                Debug.Print("Exception when calling CurrentUserApi.ApiV2UsersmeGet: " + e.Message );
            }
        }
    }
}
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

