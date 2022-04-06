# HistoryView

## Properties
Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**item_id** | **int** | The item ID. | [optional] 
**world_id** | **int** | The world ID, if applicable. | [optional] 
**last_upload_time** | **int** | The last upload time for this endpoint, in milliseconds since the UNIX epoch. | [optional] 
**entries** | [**list[MinimizedSaleView]**](MinimizedSaleView.md) | The historical sales. | [optional] 
**dc_name** | **str** | The DC name, if applicable. | [optional] 
**stack_size_histogram** | **dict(str, int)** | A map of quantities to sale counts, representing the number of sales of each quantity. | [optional] 
**stack_size_histogram_nq** | **dict(str, int)** | A map of quantities to NQ sale counts, representing the number of sales of each quantity. | [optional] 
**stack_size_histogram_hq** | **dict(str, int)** | A map of quantities to HQ sale counts, representing the number of sales of each quantity. | [optional] 
**regular_sale_velocity** | **float** | The average number of sales per day, over the past seven days (or the entirety of the shown sales, whichever comes first). | [optional] 
**nq_sale_velocity** | **float** | The average number of NQ sales per day, over the past seven days (or the entirety of the shown sales, whichever comes first). | [optional] 
**hq_sale_velocity** | **float** | The average number of HQ sales per day, over the past seven days (or the entirety of the shown sales, whichever comes first). | [optional] 
**world_name** | **str** | The world name, if applicable. | [optional] 

[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)


