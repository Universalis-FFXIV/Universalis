# IO.Swagger.Model.UserAlertView
## Properties

Name | Type | Description | Notes
------------ | ------------- | ------------- | -------------
**Id** | **string** | The alert&#39;s ID. | [optional] 
**ItemID** | **int?** | The alert&#39;s item ID. | [optional] 
**Created** | **string** | The time that this alert was created, in milliseconds since the UNIX epoch. | [optional] 
**LastChecked** | **string** | The last time that this alert was checked, in milliseconds since the UNIX epoch. | [optional] 
**Name** | **string** | The alert&#39;s name. | [optional] 
**Server** | **string** | The alert&#39;s server. | [optional] 
**Expiry** | **string** | The expiry time of this alert, in milliseconds since the UNIX epoch. | [optional] 
**TriggerConditions** | **List&lt;string&gt;** | The trigger conditions for this alert. | [optional] 
**TriggerType** | **string** | The trigger type of this alert. | [optional] 
**TriggerLastSent** | **string** | The last time this alert was triggered, in milliseconds since the UNIX epoch. | [optional] 
**TriggerDataCenter** | **bool?** | Whether or not this alert should trigger on the entire data center. | [optional] 
**TriggerHQ** | **bool?** | Whether or not this alert should trigger on HQ items. | [optional] 
**TriggerNQ** | **bool?** | Whether or not this alert should trigger on NQ items. | [optional] 
**TriggerActive** | **bool?** | Whether or not this alert is active. | [optional] 

[[Back to Model list]](../README.md#documentation-for-models) [[Back to API list]](../README.md#documentation-for-api-endpoints) [[Back to README]](../README.md)

