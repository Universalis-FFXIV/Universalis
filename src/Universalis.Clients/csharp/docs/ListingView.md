# IO.Swagger.Model.ListingView
## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**LastReviewTime** | **long?** | The time that this listing was posted, in seconds since the UNIX epoch. | [optional] 
**PricePerUnit** | **int?** | The price per unit sold. | [optional] 
**Quantity** | **int?** | The stack size sold. | [optional] 
**StainID** | **int?** | The ID of the dye on this item. | [optional] 
**WorldName** | **string** | The world name, if applicable. | [optional] 
**WorldID** | **int?** | The world ID, if applicable. | [optional] 
**CreatorName** | **string** | The creator&#39;s character name. | [optional] 
**CreatorID** | **string** | A SHA256 hash of the creator&#39;s ID. | [optional] 
**Hq** | **bool?** | Whether or not the item is high-quality. | [optional] 
**IsCrafted** | **bool?** | Whether or not the item is crafted. | [optional] 
**ListingID** | **string** | A SHA256 hash of the ID of this listing. Due to some current client-side bugs, this will almost always be null. | [optional] 
**Materia** | [**List&lt;MateriaView&gt;**](MateriaView.md) | The materia on this item. | [optional] 
**OnMannequin** | **bool?** | Whether or not the item is being sold on a mannequin. | [optional] 
**RetainerCity** | **int?** | The city ID of the retainer.  Limsa Lominsa &#x3D; 1  Gridania &#x3D; 2  Ul&#39;dah &#x3D; 3  Ishgard &#x3D; 4  Kugane &#x3D; 7  Crystarium &#x3D; 10 | [optional] 
**RetainerID** | **string** | A SHA256 hash of the retainer&#39;s ID. | [optional] 
**RetainerName** | **string** | The retainer&#39;s name. | [optional] 
**SellerID** | **string** | A SHA256 hash of the seller&#39;s ID. | [optional] 
**Total** | **int?** | The total price. | [optional] 

[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)

