﻿using System.Threading;
using System.Threading.Tasks;
using Tableau.Migration.Content.Files;
using Tableau.Migration.Engine.Endpoints;
using Tableau.Migration.Engine.Hooks.Transformers;
using Tableau.Migration.Engine.Pipelines;

namespace Tableau.Migration.Engine.Preparation
{
    /// <summary>
    /// <see cref="IContentItemPreparer{TContent, TPublish}"/> implementation that pulls
    /// the publish item from the source endpoint.
    /// </summary>
    /// <typeparam name="TContent"><inheritdoc /></typeparam>
    /// <typeparam name="TPublish"><inheritdoc /></typeparam>
    public class EndpointContentItemPreparer<TContent, TPublish> : ContentItemPreparerBase<TContent, TPublish>
        where TPublish : class
    {
        private readonly ISourceEndpoint _source;

        /// <summary>
        /// Creates a new <see cref="EndpointContentItemPreparer{TContent, TPublish}"/> object.
        /// </summary>
        /// <param name="source">The source endpoint.</param>
        /// <param name="transformerRunner"><inheritdoc /></param>
        /// <param name="pipeline"><inheritdoc /></param>
        public EndpointContentItemPreparer(ISourceEndpoint source,
            IContentTransformerRunner transformerRunner, IMigrationPipeline pipeline)
            : base(transformerRunner, pipeline)
        {
            _source = source;
        }

        /// <inheritdoc />
        protected override async Task<IResult<TPublish>> PullAsync(ContentMigrationItem<TContent> item, CancellationToken cancel)
        {
            return await _source.PullAsync<TContent, TPublish>(item.SourceItem, cancel).ConfigureAwait(false);
        }
    }
}