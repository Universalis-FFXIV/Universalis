# ListingView

## Properties
Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**last_review_time** | **int** | The time that this listing was posted, in seconds since the UNIX epoch. | [optional] 
**price_per_unit** | **int** | The price per unit sold. | [optional] 
**quantity** | **int** | The stack size sold. | [optional] 
**stain_id** | **int** | The ID of the dye on this item. | [optional] 
**world_name** | **str** | The world name, if applicable. | [optional] 
**world_id** | **int** | The world ID, if applicable. | [optional] 
**creator_name** | **str** | The creator&#39;s character name. | [optional] 
**creator_id** | **str** | A SHA256 hash of the creator&#39;s ID. | [optional] 
**hq** | **bool** | Whether or not the item is high-quality. | [optional] 
**is_crafted** | **bool** | Whether or not the item is crafted. | [optional] 
**listing_id** | **str** | A SHA256 hash of the ID of this listing. Due to some current client-side bugs, this will almost always be null. | [optional] 
**materia** | [**list[MateriaView]**](MateriaView.md) | The materia on this item. | [optional] 
**on_mannequin** | **bool** | Whether or not the item is being sold on a mannequin. | [optional] 
**retainer_city** | **int** | The city ID of the retainer.  Limsa Lominsa &#x3D; 1  Gridania &#x3D; 2  Ul&#39;dah &#x3D; 3  Ishgard &#x3D; 4  Kugane &#x3D; 7  Crystarium &#x3D; 10 | [optional] 
**retainer_id** | **str** | A SHA256 hash of the retainer&#39;s ID. | [optional] 
**retainer_name** | **str** | The retainer&#39;s name. | [optional] 
**seller_id** | **str** | A SHA256 hash of the seller&#39;s ID. | [optional] 
**total** | **int** | The total price. | [optional] 

[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)


