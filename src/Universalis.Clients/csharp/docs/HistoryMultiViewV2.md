# IO.Swagger.Model.HistoryMultiViewV2
## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**ItemIDs** | **List&lt;int?&gt;** | The item IDs that were requested. | [optional] 
**Items** | [**Dictionary&lt;string, HistoryView&gt;**](HistoryView.md) | The item data that was requested, keyed on the item ID. | [optional] 
**WorldID** | **int?** | The ID of the world requested, if applicable. | [optional] 
**DcName** | **string** | The name of the DC requested, if applicable. | [optional] 
**UnresolvedItems** | **List&lt;int?&gt;** | A list of IDs that could not be resolved to any item data. | [optional] 
**WorldName** | **string** | The name of the world requested, if applicable. | [optional] 

[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)

