﻿// Copyright (c) 2023, Salesforce, Inc.
//  SPDX-License-Identifier: Apache-2
//  
//  Licensed under the Apache License, Version 2.0 (the ""License"") 
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  
//  http://www.apache.org/licenses/LICENSE-2.0
//  
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an ""AS IS"" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
//

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Tableau.Migration.Content;
using Tableau.Migration.Engine.Pipelines;
using Tableau.Migration.Resources;

namespace Tableau.Migration.Engine.Hooks.Transformers.Default
{
    /// <summary>
    /// Transformer that maps the users from a given group.
    /// </summary>
    public class GroupUsersTransformer : ContentTransformerBase<IPublishableGroup>
    {
        private readonly IMigrationPipeline _migrationPipeline;
        private readonly ISharedResourcesLocalizer _localizer;
        private readonly ILogger<GroupUsersTransformer> _logger;

        /// <summary>
        /// Creates a new <see cref="GroupUsersTransformer"/> object.
        /// </summary>
        /// <param name="migrationPipeline">Destination content finder object.</param>
        /// <param name="localizer">A string localizer.</param>
        /// <param name="logger">The logger used to log messages.</param>
        public GroupUsersTransformer(
            IMigrationPipeline migrationPipeline,
            ISharedResourcesLocalizer localizer,
            ILogger<GroupUsersTransformer> logger) : base(localizer, logger)
        {
            _migrationPipeline = migrationPipeline;
            _localizer = localizer;
            _logger = logger;
        }

        /// <inheritdoc />
        public override async Task<IPublishableGroup?> TransformAsync(
            IPublishableGroup ctx,
            CancellationToken cancel)
        {
            var contentFinder = _migrationPipeline.CreateDestinationFinder<IUser>();

            foreach (var user in ctx.Users)
            {
                var contentDestination = await contentFinder
                    .FindDestinationReferenceAsync(user.User.Location, cancel)
                    .ConfigureAwait(false);

                if (contentDestination is not null)
                {
                    user.User = contentDestination;
                }
                else
                {
                    _logger.LogWarning(_localizer[SharedResourceKeys.GroupUsersTransformerCannotMapWarning], ctx.Name, user.User.Location);
                }
            }
            return ctx;
        }
    }
}
