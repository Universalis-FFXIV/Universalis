# IO.Swagger.Api.UserCharactersApi

All URIs are relative to *https://universalis.app*

Method | HTTP request | Description
------------- | ------------- | -------------
[**ApiV2CharactersIdGet**](UserCharactersApi.md#apiv2charactersidget) | **GET** /api/v2/characters/{id} | Retrieves a characters. Requires the session cookie to be set correctly.


<a name="apiv2charactersidget"></a>
# **ApiV2CharactersIdGet**
> UserCharacterView ApiV2CharactersIdGet (Guid? id)

Retrieves a characters. Requires the session cookie to be set correctly.

### Example
```csharp
using System;
using System.Diagnostics;
using IO.Swagger.Api;
using IO.Swagger.Client;
using IO.Swagger.Model;

namespace Example
{
    public class ApiV2CharactersIdGetExample
    {
        public void main()
        {
            var apiInstance = new UserCharactersApi();
            var id = new Guid?(); // Guid? | 

            try
            {
                // Retrieves a characters. Requires the session cookie to be set correctly.
                UserCharacterView result = apiInstance.ApiV2CharactersIdGet(id);
                Debug.WriteLine(result);
            }
            catch (Exception e)
            {
                Debug.Print("Exception when calling UserCharactersApi.ApiV2CharactersIdGet: " + e.Message );
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

[**UserCharacterView**](UserCharacterView.md)

### Authorization

No authorization required

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: text/plain, application/json, text/json

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

