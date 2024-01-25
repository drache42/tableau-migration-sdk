﻿using System.Threading;
using System.Threading.Tasks;

namespace Tableau.Migration.Api
{
    /// <summary>
    /// Interface for a content typed API client that can publish items.
    /// </summary>
    /// <typeparam name="TPublish">The content publish type.</typeparam>
    /// <typeparam name="TPublishResult">The publish result type.</typeparam>
    public interface IPublishApiClient<TPublish, TPublishResult>
        where TPublishResult : class, IContentReference
    {
        /// <summary>
        /// Publishes a content item.
        /// </summary>
        /// <param name="item">The content item to publish.</param>
        /// <param name="cancel">A cancellation token to obey.</param>
        /// <returns>The results of the publishing with a content reference of the newly published item.</returns>
        Task<IResult<TPublishResult>> PublishAsync(TPublish item, CancellationToken cancel);
    }

    /// <summary>
    /// Interface for a content typed API client that can publish items.
    /// </summary>
    /// <typeparam name="TPublish">The content publish type.</typeparam>
    public interface IPublishApiClient<TPublish> : IPublishApiClient<TPublish, TPublish>
        where TPublish : class, IContentReference
    { }
}