using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Universalis.Application.Swagger;
using Universalis.Application.Views.V2;
using Universalis.Mogboard;
using Universalis.Mogboard.Entities;
using Universalis.Mogboard.Entities.Id;

namespace Universalis.Application.Controllers.V2;

[ApiController]
[ApiVersion("1")]
[ApiVersion("2")]
[Route("api")]
[ApiExplorerSettings(IgnoreApi = true)]
public class UserListController : ControllerBase
{
    private readonly IMogboardTable<UserList, UserListId> _userListTable;

    public UserListController(IMogboardTable<UserList, UserListId> userListTable)
    {
        _userListTable = userListTable;
    }

    /// <summary>
    /// Retrieves a user list.
    /// </summary>
    /// <param name="listId">The ID of the list to retrieve.</param>
    /// <param name="cancellationToken"></param>
    /// <response code="200">Data retrieved successfully.</response>
    /// <response code="404">The list requested does not exist.</response>
    [HttpGet]
    [MapToApiVersion("1")]
    [Route("lists/{listId:guid}")]
    [ApiTag("User lists")]
    [ProducesResponseType(typeof(UserListView), 200)]
    [ProducesResponseType(404)]
    public Task<IActionResult> Get(Guid listId, CancellationToken cancellationToken = default)
    {
        return GetV2(listId, cancellationToken);
    }

    /// <summary>
    /// Retrieves a user list.
    /// </summary>
    /// <param name="listId">The ID of the list to retrieve.</param>
    /// <param name="cancellationToken"></param>
    /// <response code="200">Data retrieved successfully.</response>
    /// <response code="404">The list requested does not exist.</response>
    [HttpGet]
    [MapToApiVersion("2")]
    [Route("v{version:apiVersion}/lists/{listId:guid}")]
    [ApiTag("User lists")]
    [ProducesResponseType(typeof(UserListView), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetV2(Guid listId, CancellationToken cancellationToken = default)
    {
        var userListId = new UserListId(listId);
        var userList = await _userListTable.Get(userListId, cancellationToken);
        if (userList == null)
        {
            return NotFound();
        }

        var userListView = new UserListView
        {
            CreatedTimestampMs = userList.Added.ToUnixTimeMilliseconds().ToString(),
            UpdatedTimestampMs = userList.Updated.ToUnixTimeMilliseconds().ToString(),
            Name = userList.Name ?? string.Empty,
            Items = userList.Items?.ToList() ?? new List<int>(),
        };

        return Ok(userListView);
    }
}