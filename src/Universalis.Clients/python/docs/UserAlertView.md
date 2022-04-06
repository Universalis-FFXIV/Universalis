# UserAlertView

## Properties
Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**id** | **str** | The alert&#39;s ID. | [optional] 
**item_id** | **int** | The alert&#39;s item ID. | [optional] 
**created** | **str** | The time that this alert was created, in milliseconds since the UNIX epoch. | [optional] 
**last_checked** | **str** | The last time that this alert was checked, in milliseconds since the UNIX epoch. | [optional] 
**name** | **str** | The alert&#39;s name. | [optional] 
**server** | **str** | The alert&#39;s server. | [optional] 
**expiry** | **str** | The expiry time of this alert, in milliseconds since the UNIX epoch. | [optional] 
**trigger_conditions** | **list[str]** | The trigger conditions for this alert. | [optional] 
**trigger_type** | **str** | The trigger type of this alert. | [optional] 
**trigger_last_sent** | **str** | The last time this alert was triggered, in milliseconds since the UNIX epoch. | [optional] 
**trigger_data_center** | **bool** | Whether or not this alert should trigger on the entire data center. | [optional] 
**trigger_hq** | **bool** | Whether or not this alert should trigger on HQ items. | [optional] 
**trigger_nq** | **bool** | Whether or not this alert should trigger on NQ items. | [optional] 
**trigger_active** | **bool** | Whether or not this alert is active. | [optional] 

[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)


