﻿using System;
using System.Threading.Tasks;
using AutoFixture;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Tableau.Migration.Api;
using Tableau.Migration.Content.Files;
using Tableau.Migration.Content.Search;
using Tableau.Migration.Engine;
using Tableau.Migration.Engine.Endpoints;
using Tableau.Migration.Paging;
using Xunit;

namespace Tableau.Migration.Tests.Unit.Engine.Endpoints
{
    public class TableauApiEndpointBaseTests
    {
        #region - Test Classes -

        public class TestApiEndpoint : TableauApiEndpointBase
        {
            new public IApiClient ServerApi => base.ServerApi;

            new public ISitesApiClient SiteApi => base.SiteApi;

            new public AsyncServiceScope EndpointScope => base.EndpointScope;

            public TestApiEndpoint(IServiceScopeFactory serviceScopeFactory,
                ITableauApiEndpointConfiguration config,
                IContentReferenceFinderFactory finderFactory,
                IContentFileStore fileStore)
                : base(serviceScopeFactory, config, finderFactory, fileStore)
            { }
        }

        public class TableauApiEndpointBaseTest : TableauApiEndpointTestBase<TestApiEndpoint>
        {
            protected readonly TestApiEndpoint Endpoint;

            public TableauApiEndpointBaseTest()
            {
                Endpoint = new(MigrationServices.GetRequiredService<IServiceScopeFactory>(),
                    Create<ITableauApiEndpointConfiguration>(),
                    Create<IContentReferenceFinderFactory>(),
                    Create<IContentFileStore>()
                );
            }
        }

        #endregion

        #region - Ctor -

        public class Ctor : AutoFixtureTestBase
        {
            [Fact]
            public void CreatesApiClient()
            {
                var apiClient = AutoFixture.Freeze<IApiClient>();

                var migrationServiceCollection = new ServiceCollection()
                    .AddMigrationApiClient()
                    .AddTransient(p => apiClient);

                var migrationServices = migrationServiceCollection.BuildServiceProvider();
                var serviceScopeFactory = migrationServices.GetRequiredService<IServiceScopeFactory>();

                var config = Create<ITableauApiEndpointConfiguration>();
                var mockFinderFactory = Create<IContentReferenceFinderFactory>();
                var mockFileStore = Create<IContentFileStore>();

                var endpoint = new TestApiEndpoint(serviceScopeFactory, config, mockFinderFactory, mockFileStore);

                Assert.Same(apiClient, endpoint.ServerApi);
            }

            [Fact]
            public void ApiClientScopeFileStoreMatchesMigrationFileStore()
            {
                var apiClient = AutoFixture.Freeze<IApiClient>();

                var migrationServiceCollection = new ServiceCollection()
                    .AddTableauMigrationSdk()
                    .AddTransient(p => apiClient);

                var migrationServices = migrationServiceCollection.BuildServiceProvider();
                var serviceScopeFactory = migrationServices.GetRequiredService<IServiceScopeFactory>();

                var config = Create<ITableauApiEndpointConfiguration>();
                var mockFinderFactory = Create<IContentReferenceFinderFactory>();
                var mockFileStore = Create<IContentFileStore>();

                var endpoint = new TestApiEndpoint(serviceScopeFactory, config, mockFinderFactory, mockFileStore);

                Assert.Same(mockFileStore, endpoint.EndpointScope.ServiceProvider.GetService<IContentFileStore>());
            }
        }

        #endregion

        #region - DisposeAsync -

        public class DisposeAsync : TableauApiEndpointBaseTest
        {
            [Fact]
            public async Task DisposesSiteApiAsync()
            {
                await Endpoint.InitializeAsync(Cancel);

                await Endpoint.DisposeAsync();

                MockSiteApi.Verify(x => x.DisposeAsync(), Times.Once);
            }
        }

        #endregion

        #region - SiteApi -

        public class SiteApi : TableauApiEndpointBaseTest
        {
            [Fact]
            public void NotInitialized()
            {
                Assert.Throws<InvalidOperationException>(() => Endpoint.SiteApi);
            }

            [Fact]
            public async Task SignInFailedAsync()
            {
                MockServerApi.Setup(x => x.SignInAsync(Cancel))
                    .ReturnsAsync(AsyncDisposableResult<ISitesApiClient>.Failed(new Exception()));

                await Endpoint.InitializeAsync(Cancel);

                Assert.Throws<InvalidOperationException>(() => Endpoint.SiteApi);
            }

            [Fact]
            public async Task InitializedSuccessfullyAsync()
            {
                await Endpoint.InitializeAsync(Cancel);

                Assert.Same(MockSiteApi.Object, Endpoint.SiteApi);
            }
        }

        #endregion

        #region - InitializeAsync -

        public class InitializeAsync : TableauApiEndpointBaseTest
        {
            [Fact]
            public async Task SignsInAsync()
            {
                var signInResult = AsyncDisposableResult<ISitesApiClient>.Succeeded(MockSiteApi.Object);
                MockServerApi.Setup(x => x.SignInAsync(Cancel)).ReturnsAsync(() => signInResult);

                var result = await Endpoint.InitializeAsync(Cancel);

                Assert.Same(signInResult, result);
                MockServerApi.Verify(x => x.SignInAsync(Cancel), Times.Once);
            }
        }

        #endregion

        #region - GetPager -

        public class GetPager : TableauApiEndpointBaseTest
        {
            [Fact]
            public async Task GetsPager()
            {
                await Endpoint.InitializeAsync(Cancel);

                var mockPager = new Mock<IPager<TestContentType>>();
                MockSiteApi.Setup(x => x.GetListApiClient<TestContentType>().GetPager(1523)).Returns(mockPager.Object);

                var result = Endpoint.GetPager<TestContentType>(1523);

                Assert.Same(mockPager.Object, result);
            }
        }

        #endregion
    }
}