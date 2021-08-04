using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using Universalis.Application.Views;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.DbAccess.Uploads;

namespace Universalis.Application.Controllers.V1.Extra.Stats
{
    [ApiController]
    [Route("api/extra/stats/recently-updated")]
    public class RecentlyUpdatedItemsController : ControllerBase
    {
        private readonly IRecentlyUpdatedItemsDbAccess _recentlyUpdatedItemsDb;

        public RecentlyUpdatedItemsController(IRecentlyUpdatedItemsDbAccess recentlyUpdatedItemsDb)
        {
            _recentlyUpdatedItemsDb = recentlyUpdatedItemsDb;
        }

        /// <summary>
        /// Returns a list of some of the most recently updated items on the website. This endpoint
        /// is a legacy endpoint and does not include any data on which worlds or data centers the updates happened on.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(RecentlyUpdatedItemsView), 200)]
        public async Task<RecentlyUpdatedItemsView> Get()
        {
            var data = (await _recentlyUpdatedItemsDb.Retrieve(new RecentlyUpdatedItemsQuery()))?.Items;
            return data == null
                ? new RecentlyUpdatedItemsView { Items = new List<uint>() }
                : new RecentlyUpdatedItemsView { Items = data };
        }
    }
}