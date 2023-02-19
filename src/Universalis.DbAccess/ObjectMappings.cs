using Cassandra.Mapping;
using Universalis.Entities;
using Universalis.Entities.AccessControl;
using Universalis.Entities.MarketBoard;
using Universalis.Entities.Uploads;

namespace Universalis.DbAccess;

public class ObjectMappings : Mappings
{
    public ObjectMappings()
    {
        For<ApiKey>()
            .TableName("api_key")
            .PartitionKey(s => s.TokenSha512)
            .Column(s => s.TokenSha512, col => col.WithName("token_sha512"))
            .Column(s => s.Name, col => col.WithName("name"))
            .Column(s => s.CanUpload, col => col.WithName("can_upload"));

        For<FlaggedUploader>()
            .TableName("flagged_uploader")
            .PartitionKey(s => s.IdSha256)
            .Column(s => s.IdSha256, col => col.WithName("id_sha256"));

        For<Character>()
            .TableName("character")
            .PartitionKey(s => s.ContentIdSha256)
            .Column(s => s.ContentIdSha256, col => col.WithName("content_id_sha256"))
            .Column(s => s.Name, col => col.WithName("name"))
            .Column(s => s.WorldId, col => col.WithName("world_id"));

        For<Sale>()
            .TableName("sale")
            .PartitionKey(s => s.ItemId, s => s.WorldId)
            .ClusteringKey(s => s.SaleTime, SortOrder.Descending)
            .Column(s => s.WriteTime, col => col.WithName("write_time"))
            .Column(s => s.Id, col => col.WithName("id"))
            .Column(s => s.SaleTime, col => col.WithName("sale_time"))
            .Column(s => s.ItemId, col => col.WithName("item_id"))
            .Column(s => s.WorldId, col => col.WithName("world_id"))
            .Column(s => s.BuyerName, col => col.WithName("buyer_name"))
            .Column(s => s.Hq, col => col.WithName("hq"))
            .Column(s => s.OnMannequin, col => col.WithName("on_mannequin"))
            .Column(s => s.Quantity, col => col.WithName("quantity"))
            .Column(s => s.PricePerUnit, col => col.WithName("unit_price"))
            .Column(s => s.UploaderIdHash, col => col.WithName("uploader_id"));
    }
}