# IO.Swagger.Model.HistoryView
## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**ItemID** | **int?** | The item ID. | [optional] 
**WorldID** | **int?** | The world ID, if applicable. | [optional] 
**LastUploadTime** | **long?** | The last upload time for this endpoint, in milliseconds since the UNIX epoch. | [optional] 
**Entries** | [**List&lt;MinimizedSaleView&gt;**](MinimizedSaleView.md) | The historical sales. | [optional] 
**DcName** | **string** | The DC name, if applicable. | [optional] 
**StackSizeHistogram** | **Dictionary&lt;string, int?&gt;** | A map of quantities to sale counts, representing the number of sales of each quantity. | [optional] 
**StackSizeHistogramNQ** | **Dictionary&lt;string, int?&gt;** | A map of quantities to NQ sale counts, representing the number of sales of each quantity. | [optional] 
**StackSizeHistogramHQ** | **Dictionary&lt;string, int?&gt;** | A map of quantities to HQ sale counts, representing the number of sales of each quantity. | [optional] 
**RegularSaleVelocity** | **float?** | The average number of sales per day, over the past seven days (or the entirety of the shown sales, whichever comes first). | [optional] 
**NqSaleVelocity** | **float?** | The average number of NQ sales per day, over the past seven days (or the entirety of the shown sales, whichever comes first). | [optional] 
**HqSaleVelocity** | **float?** | The average number of HQ sales per day, over the past seven days (or the entirety of the shown sales, whichever comes first). | [optional] 
**WorldName** | **string** | The world name, if applicable. | [optional] 

[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)

