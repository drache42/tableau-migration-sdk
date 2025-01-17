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

using System;
using System.Threading;
using System.Threading.Tasks;
using Tableau.Migration.Content.Search;
using Tableau.Migration.Engine.Manifest;

namespace Tableau.Migration.Engine.Endpoints.Search
{
    /// <summary>
    /// <see cref="IContentReferenceFinder{TContent}"/> implementation that finds source references
    /// from the migration manifest.
    /// </summary>
    /// <typeparam name="TContent">The content type.</typeparam>
    public class ManifestSourceContentReferenceFinder<TContent> : IContentReferenceFinder<TContent>
        where TContent : IContentReference
    {
        private readonly IMigrationManifestEditor _manifest;

        /// <summary>
        /// Creates a new <see cref="ManifestSourceContentReferenceFinder{TContent}"/> object.
        /// </summary>
        /// <param name="manifest">The manifest.</param>
        public ManifestSourceContentReferenceFinder(IMigrationManifestEditor manifest)
        {
            _manifest = manifest;
        }

        /// <inheritdoc />
        public Task<IContentReference?> FindByIdAsync(Guid id, CancellationToken cancel)
        {
            var partition = _manifest.Entries.GetOrCreatePartition<TContent>();

            if (partition.BySourceId.TryGetValue(id, out var entry))
            {
                return Task.FromResult<IContentReference?>(entry.Source);
            }

            return Task.FromResult<IContentReference?>(null);
        }
    }
}
