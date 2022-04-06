# IO.Swagger.Api.UserListsApi

All URIs are relative to *https://localhost*

Method | HTTP request | Description
------------- | ------------- | -------------
[**ApiV2ListsListIdGet**](UserListsApi.md#apiv2listslistidget) | **GET** /api/v2/lists/{listId} | Retrieves a user list.


<a name="apiv2listslistidget"></a>
# **ApiV2ListsListIdGet**
> UserListView ApiV2ListsListIdGet (Guid? listId)

Retrieves a user list.

### Example
```csharp
using System;
using System.Diagnostics;
using IO.Swagger.Api;
using IO.Swagger.Client;
using IO.Swagger.Model;

namespace Example
{
    public class ApiV2ListsListIdGetExample
    {
        public void main()
        {
            var apiInstance = new UserListsApi();
            var listId = new Guid?(); // Guid? | The ID of the list to retrieve.

            try
            {
                // Retrieves a user list.
                UserListView result = apiInstance.ApiV2ListsListIdGet(listId);
                Debug.WriteLine(result);
            }
            catch (Exception e)
            {
                Debug.Print("Exception when calling UserListsApi.ApiV2ListsListIdGet: " + e.Message );
            }
        }
    }
}
```

### Parameters

Name | Type | Description  | Notes
------------- | ------------- | ------------- | -------------
 **listId** | [**Guid?**](Guid?.md)| The ID of the list to retrieve. | 

### Return type

[**UserListView**](UserListView.md)

### Authorization

No authorization required

### HTTP request headers

 - **Content-Type**: Not defined
 - **Accept**: text/plain, application/json, text/json

[[Back to top]](#) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to Model list]](../README.md#documentation-for-models) [[Back to README]](../README.md)

