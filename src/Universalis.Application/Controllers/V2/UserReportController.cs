using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
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
[RequireMogboardAuthorization]
#if !DEBUG
[ApiExplorerSettings(IgnoreApi = true)]
#endif
public class UserReportController : ControllerBase
{
    private readonly IMogboardTable<UserReport, UserReportId> _reports;

    public UserReportController(IMogboardTable<UserReport, UserReportId> reports)
    {
        _reports = reports;
    }

    /// <summary>
    /// Retrieves a user report. Requires the session cookie to be set correctly.
    /// </summary>
    /// <response code="200">Data retrieved successfully.</response>
    /// <response code="403">No session information was set, or the corresponding user was missing.</response>
    /// <response code="404">The report could not be found.</response>
    [HttpGet]
    [MapToApiVersion("1")]
    [Route("reports/{id:guid}")]
    [ApiTag("User reports")]
    [ProducesResponseType(typeof(UserReportView), 200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public Task<IActionResult> Get(Guid id, CancellationToken cancellationToken = default)
    {
        return GetV2(id, cancellationToken);
    }

    /// <summary>
    /// Retrieves a user report. Requires the session cookie to be set correctly.
    /// </summary>
    /// <response code="200">Data retrieved successfully.</response>
    /// <response code="403">No session information was set, or the corresponding user was missing.</response>
    /// <response code="404">The report could not be found.</response>
    [HttpGet]
    [MapToApiVersion("2")]
    [Route("v{version:apiVersion}/reports/{id:guid}")]
    [ApiTag("User reports")]
    [ProducesResponseType(typeof(UserReportView), 200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetV2(Guid id, CancellationToken cancellationToken = default)
    {
        var reportId = new UserReportId(id);
        var report = await _reports.Get(reportId, cancellationToken);
        if (report == null)
        {
            return NotFound();
        }

        var userReportView = new UserReportView
        {
            Id = report.Id.ToString(),
            TimestampMs = report.Added.ToUnixTimeMilliseconds().ToString(),
            Name = report.Name,
            Items = report.Items!.ToList(),
        };

        return Ok(userReportView);
    }
}