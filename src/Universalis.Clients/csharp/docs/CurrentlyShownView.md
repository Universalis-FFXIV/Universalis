# IO.Swagger.Model.CurrentlyShownView
## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**ItemID** | **int?** | The item ID. | [optional] 
**WorldID** | **int?** | The world ID, if applicable. | [optional] 
**LastUploadTime** | **long?** | The last upload time for this endpoint, in milliseconds since the UNIX epoch. | [optional] 
**Listings** | [**List&lt;ListingView&gt;**](ListingView.md) | The currently-shown listings. | [optional] 
**RecentHistory** | [**List&lt;SaleView&gt;**](SaleView.md) | The currently-shown sales. | [optional] 
**DcName** | **string** | The DC name, if applicable. | [optional] 
**CurrentAveragePrice** | **float?** | The average listing price, with outliers removed beyond 3 standard deviations of the mean. | [optional] 
**CurrentAveragePriceNQ** | **float?** | The average NQ listing price, with outliers removed beyond 3 standard deviations of the mean. | [optional] 
**CurrentAveragePriceHQ** | **float?** | The average HQ listing price, with outliers removed beyond 3 standard deviations of the mean. | [optional] 
**RegularSaleVelocity** | **float?** | The average number of sales per day, over the past seven days (or the entirety of the shown sales, whichever comes first).  This number will tend to be the same for every item, because the number of shown sales is the same and over the same period.  This statistic is more useful in historical queries. | [optional] 
**NqSaleVelocity** | **float?** | The average number of NQ sales per day, over the past seven days (or the entirety of the shown sales, whichever comes first).  This number will tend to be the same for every item, because the number of shown sales is the same and over the same period.  This statistic is more useful in historical queries. | [optional] 
**HqSaleVelocity** | **float?** | The average number of HQ sales per day, over the past seven days (or the entirety of the shown sales, whichever comes first).  This number will tend to be the same for every item, because the number of shown sales is the same and over the same period.  This statistic is more useful in historical queries. | [optional] 
**AveragePrice** | **float?** | The average sale price, with outliers removed beyond 3 standard deviations of the mean. | [optional] 
**AveragePriceNQ** | **float?** | The average NQ sale price, with outliers removed beyond 3 standard deviations of the mean. | [optional] 
**AveragePriceHQ** | **float?** | The average HQ sale price, with outliers removed beyond 3 standard deviations of the mean. | [optional] 
**MinPrice** | **int?** | The minimum listing price. | [optional] 
**MinPriceNQ** | **int?** | The minimum NQ listing price. | [optional] 
**MinPriceHQ** | **int?** | The minimum HQ listing price. | [optional] 
**MaxPrice** | **int?** | The maximum listing price. | [optional] 
**MaxPriceNQ** | **int?** | The maximum NQ listing price. | [optional] 
**MaxPriceHQ** | **int?** | The maximum HQ listing price. | [optional] 
**StackSizeHistogram** | **Dictionary&lt;string, int?&gt;** | A map of quantities to listing counts, representing the number of listings of each quantity. | [optional] 
**StackSizeHistogramNQ** | **Dictionary&lt;string, int?&gt;** | A map of quantities to NQ listing counts, representing the number of listings of each quantity. | [optional] 
**StackSizeHistogramHQ** | **Dictionary&lt;string, int?&gt;** | A map of quantities to HQ listing counts, representing the number of listings of each quantity. | [optional] 
**WorldName** | **string** | The world name, if applicable. | [optional] 
**WorldUploadTimes** | **Dictionary&lt;string, long?&gt;** | The last upload times in milliseconds since epoch for each world in the response, if this is a DC request. | [optional] 

[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)

