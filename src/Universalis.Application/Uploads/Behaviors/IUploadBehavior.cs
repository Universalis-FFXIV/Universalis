using System.Threading;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Universalis.Application.Uploads.Schema;
using Universalis.Entities.Uploads;

namespace Universalis.Application.Uploads.Behaviors
{
    public interface IUploadBehavior
    {
        /// <summary>
        /// Whether or not this upload behavior should be executed.
        /// </summary>
        /// <param name="parameters">The request parameters.</param>
        /// <returns><see langword="true" /> if the behavior should be executed, otherwise <see langword="false" />.</returns>
        public bool ShouldExecute(UploadParameters parameters);

        /// <summary>
        /// Executes the upload behavior.
        /// </summary>
        /// <param name="source">The upload application.</param>
        /// <param name="parameters">The request parameters.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>An <see cref="IActionResult"/> if the request should return early, or <see langword="null" /> if the request should continue.</returns>
        public Task<IActionResult> Execute(TrustedSource source, UploadParameters parameters, CancellationToken cancellationToken = default);
    }
}