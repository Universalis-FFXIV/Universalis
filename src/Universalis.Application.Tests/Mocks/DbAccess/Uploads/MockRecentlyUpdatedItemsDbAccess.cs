using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.DbAccess.Uploads;
using Universalis.Entities.Uploads;

namespace Universalis.Application.Tests.Mocks.DbAccess.Uploads
{
    public class MockRecentlyUpdatedItemsDbAccess : IRecentlyUpdatedItemsDbAccess
    {
        private readonly List<RecentlyUpdatedItems> _collection = new();

        public Task Create(RecentlyUpdatedItems document, CancellationToken cancellationToken = default)
        {
            _collection.Add(document);
            return Task.CompletedTask;
        }

        public Task<RecentlyUpdatedItems> Retrieve(RecentlyUpdatedItemsQuery query, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_collection.FirstOrDefault());
        }

        public async Task Update(RecentlyUpdatedItems document, RecentlyUpdatedItemsQuery query, CancellationToken cancellationToken = default)
        {
            await Delete(query, cancellationToken);
            await Create(document, cancellationToken);
        }

        public async Task Push(uint itemId, CancellationToken cancellationToken = default)
        {
            var query = new RecentlyUpdatedItemsQuery();
            var existing = await Retrieve(query, cancellationToken);

            if (existing == null)
            {
                await Create(new RecentlyUpdatedItems
                {
                    Items = new List<uint> { itemId },
                }, cancellationToken);
                return;
            }

            var newItems = existing.Items;
            newItems.Insert(0, itemId);
            newItems = existing.Items.Take(RecentlyUpdatedItemsDbAccess.MaxItems).ToList();

            existing.Items = newItems;
        }

        public Task Delete(RecentlyUpdatedItemsQuery query, CancellationToken cancellationToken = default)
        {
            _collection.Remove(_collection.FirstOrDefault());
            return Task.CompletedTask;
        }
    }
}