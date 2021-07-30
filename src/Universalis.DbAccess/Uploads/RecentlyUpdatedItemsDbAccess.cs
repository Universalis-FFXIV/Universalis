using MongoDB.Driver;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.Entities.Uploads;

namespace Universalis.DbAccess.Uploads
{
    public class RecentlyUpdatedItemsDbAccess : DbAccessService<RecentlyUpdatedItems, RecentlyUpdatedItemsQuery>, IRecentlyUpdatedItemsDbAccess
    {
        public static readonly int MaxItems = 200;

        public RecentlyUpdatedItemsDbAccess() : base("universalis", "extraData") { }

        public async Task Push(uint itemId)
        {
            var query = new RecentlyUpdatedItemsQuery();
            var existing = await Retrieve(query);

            if (existing == null)
            {
                await Create(new RecentlyUpdatedItems
                {
                    Items = new List<uint> { itemId },
                });
                return;
            }

            var newItems = existing.Items;
            newItems.Insert(0, itemId);
            newItems = existing.Items.Take(MaxItems).ToList();

            var updateBuilder = Builders<RecentlyUpdatedItems>.Update;
            var update = updateBuilder.Set(o => o.Items, newItems);
            await Collection.UpdateOneAsync(query.ToFilterDefinition(), update);
        }
    }
}