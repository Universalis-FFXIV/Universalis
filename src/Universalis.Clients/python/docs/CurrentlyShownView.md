# CurrentlyShownView

## Properties
Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**item_id** | **int** | The item ID. | [optional] 
**world_id** | **int** | The world ID, if applicable. | [optional] 
**last_upload_time** | **int** | The last upload time for this endpoint, in milliseconds since the UNIX epoch. | [optional] 
**listings** | [**list[ListingView]**](ListingView.md) | The currently-shown listings. | [optional] 
**recent_history** | [**list[SaleView]**](SaleView.md) | The currently-shown sales. | [optional] 
**dc_name** | **str** | The DC name, if applicable. | [optional] 
**current_average_price** | **float** | The average listing price, with outliers removed beyond 3 standard deviations of the mean. | [optional] 
**current_average_price_nq** | **float** | The average NQ listing price, with outliers removed beyond 3 standard deviations of the mean. | [optional] 
**current_average_price_hq** | **float** | The average HQ listing price, with outliers removed beyond 3 standard deviations of the mean. | [optional] 
**regular_sale_velocity** | **float** | The average number of sales per day, over the past seven days (or the entirety of the shown sales, whichever comes first).  This number will tend to be the same for every item, because the number of shown sales is the same and over the same period.  This statistic is more useful in historical queries. | [optional] 
**nq_sale_velocity** | **float** | The average number of NQ sales per day, over the past seven days (or the entirety of the shown sales, whichever comes first).  This number will tend to be the same for every item, because the number of shown sales is the same and over the same period.  This statistic is more useful in historical queries. | [optional] 
**hq_sale_velocity** | **float** | The average number of HQ sales per day, over the past seven days (or the entirety of the shown sales, whichever comes first).  This number will tend to be the same for every item, because the number of shown sales is the same and over the same period.  This statistic is more useful in historical queries. | [optional] 
**average_price** | **float** | The average sale price, with outliers removed beyond 3 standard deviations of the mean. | [optional] 
**average_price_nq** | **float** | The average NQ sale price, with outliers removed beyond 3 standard deviations of the mean. | [optional] 
**average_price_hq** | **float** | The average HQ sale price, with outliers removed beyond 3 standard deviations of the mean. | [optional] 
**min_price** | **int** | The minimum listing price. | [optional] 
**min_price_nq** | **int** | The minimum NQ listing price. | [optional] 
**min_price_hq** | **int** | The minimum HQ listing price. | [optional] 
**max_price** | **int** | The maximum listing price. | [optional] 
**max_price_nq** | **int** | The maximum NQ listing price. | [optional] 
**max_price_hq** | **int** | The maximum HQ listing price. | [optional] 
**stack_size_histogram** | **dict(str, int)** | A map of quantities to listing counts, representing the number of listings of each quantity. | [optional] 
**stack_size_histogram_nq** | **dict(str, int)** | A map of quantities to NQ listing counts, representing the number of listings of each quantity. | [optional] 
**stack_size_histogram_hq** | **dict(str, int)** | A map of quantities to HQ listing counts, representing the number of listings of each quantity. | [optional] 
**world_name** | **str** | The world name, if applicable. | [optional] 
**world_upload_times** | **dict(str, int)** | The last upload times in milliseconds since epoch for each world in the response, if this is a DC request. | [optional] 

[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)


