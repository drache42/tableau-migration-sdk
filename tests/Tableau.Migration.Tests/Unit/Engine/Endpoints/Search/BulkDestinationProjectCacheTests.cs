﻿using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Tableau.Migration.Api.Rest.Models;
using Tableau.Migration.Content;
using Tableau.Migration.Engine.Endpoints.Search;
using Tableau.Migration.Paging;
using Xunit;

namespace Tableau.Migration.Tests.Unit.Engine.Endpoints.Search
{
    public class BulkDestinationProjectCacheTests
    {
        public class LoadStoreAsync : BulkDestinationCacheTest<BulkDestinationProjectCache, IProject>
        {
            public LoadStoreAsync() 
            {
                MockDestinationEndpoint.Setup(x => x.GetPager<IProject>(It.IsAny<int>()))
                    .Returns((int batchSize) => new BreadthFirstPathHierarchyPager<IProject>(new MemoryPager<IProject>(EndpointContent, batchSize), batchSize));
            }

            [Fact]
            public async Task PopulatesAllPagesFromProjectsAsync()
            {
                //Projects uses a breadth-first pager so that parent project paths
                //are built in the correct order.
                //A side-effect from that non-full/partial pages are returned when
                //a hierarchy boundary is reached.
                //Thus the cache shouldn't look for partial pages to optimize call counts,
                //and should compare to the total result count instead.

                var mockProjects = CreateMany<Mock<IProject>>().ToImmutableArray();

                var mockChildProject = mockProjects[1];
                mockChildProject.SetupGet(x => x.Location).Returns(new ContentLocation(mockProjects[0].Object.Name, mockChildProject.Object.Name));

                EndpointContent = mockProjects.Select(m => m.Object).ToList();
                BatchSize = EndpointContent.Count;
                
                var item = EndpointContent[1];

                var result = await Cache.ForLocationAsync(item.Location, Cancel);

                Assert.Equal(EndpointContent.Count, Cache.Count);
            }
        }

        public class IsProjectLockedAsync : BulkDestinationCacheTest<BulkDestinationProjectCache, IProject>
        {
            [Fact]
            public async Task NotLockedAsync()
            {
                var item = EndpointContent[1];

                //Populate cache
                await Cache.ForLocationAsync(item.Location, Cancel);

                var result = await Cache.IsProjectLockedAsync(item.Id, Cancel);

                Assert.False(result);
            }

            [Fact]
            public async Task FromStoreLoadAsync()
            {
                var mockLockedItem = Create<Mock<IProject>>();
                mockLockedItem.SetupGet(x => x.ContentPermissions).Returns(ContentPermissions.LockedToProject);

                EndpointContent[1] = mockLockedItem.Object;

                //Populate cache
                await Cache.ForLocationAsync(mockLockedItem.Object.Location, Cancel);

                var result = await Cache.IsProjectLockedAsync(mockLockedItem.Object.Id, Cancel);

                Assert.True(result);
            }

            [Fact]
            public async Task LockedWithoutNestedAsync()
            {
                var mockLockedItem = Create<Mock<IProject>>();
                mockLockedItem.SetupGet(x => x.ContentPermissions).Returns(ContentPermissions.LockedToProjectWithoutNested);

                EndpointContent[1] = mockLockedItem.Object;

                //Populate cache
                await Cache.ForLocationAsync(mockLockedItem.Object.Location, Cancel);

                var result = await Cache.IsProjectLockedAsync(mockLockedItem.Object.Id, Cancel);

                Assert.True(result);
            }

            [Fact]
            public async Task UpdatedAfterStoreLoadAsync()
            {
                var mockLockedItem = Create<Mock<IProject>>();

                EndpointContent[1] = mockLockedItem.Object;

                //Populate cache
                await Cache.ForLocationAsync(mockLockedItem.Object.Location, Cancel);

                var result = await Cache.IsProjectLockedAsync(mockLockedItem.Object.Id, Cancel);

                Assert.False(result);

                mockLockedItem.SetupGet(x => x.ContentPermissions).Returns(ContentPermissions.LockedToProject);
                Cache.UpdateLockedProjectCache(mockLockedItem.Object);

                result = await Cache.IsProjectLockedAsync(mockLockedItem.Object.Id, Cancel);

                Assert.True(result);
            }
        }
    }
}