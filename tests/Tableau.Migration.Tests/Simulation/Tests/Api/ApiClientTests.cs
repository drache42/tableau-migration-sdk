﻿using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Tableau.Migration.Api;
using Tableau.Migration.Api.Rest;
using Tableau.Migration.Api.Rest.Models.Responses;
using Tableau.Migration.Api.Simulation.Rest;
using Tableau.Migration.Api.Simulation.Rest.Net.Responses;
using Xunit;

namespace Tableau.Migration.Tests.Simulation.Tests.Api
{
    public class ApiClientTests
    {
        public abstract class ApiClientTest : ApiClientTestBase
        {
            protected void AssertDefaultRestApiVersion(string? expectedVersion)
            {
                var versionProvider = ServiceProvider.GetRequiredService<ITableauServerVersionProvider>();
                Assert.Equal(expectedVersion, versionProvider.Version?.RestApiVersion);
            }

            protected void AssertSession(Action<IServerSessionProvider> assert)
            {
                var sessionProvider = ServiceProvider.GetRequiredService<IServerSessionProvider>();
                assert(sessionProvider);
            }

            protected void AssertAuthenticationToken(Action<IAuthenticationTokenProvider> assert)
            {
                var tokenProvider = ServiceProvider.GetRequiredService<IAuthenticationTokenProvider>();
                assert(tokenProvider);
            }
        }

        public class GetServerInfoAsync : ApiClientTest
        {
            [Fact]
            public async Task Returns_server_info_on_success()
            {
                var restApiVersion = "9.99";
                var productVersion = "9999.9";
                var buildVersion = "99999.99.9999.9999";

                var serverInfo = new ServerInfoResponse.ServerInfoType(restApiVersion, productVersion, buildVersion);

                Api.Data.ServerInfo = serverInfo;

                var result = await ApiClient.GetServerInfoAsync(Cancel);

                Assert.True(result.Success);
                Assert.NotNull(result.Value);

                Assert.Equal(restApiVersion, result.Value.RestApiVersion);
                Assert.Equal(productVersion, result.Value.ProductVersion);
                Assert.Equal(buildVersion, result.Value.BuildVersion);

                AssertDefaultRestApiVersion(restApiVersion);
            }

            [Fact]
            public async Task Returns_error_on_failure()
            {
                Api.RestApi.QueryServerInfo.RespondWithError();

                var result = await ApiClient.GetServerInfoAsync(Cancel);

                Assert.False(result.Success);
                Assert.Null(result.Value);

                var error = Assert.Single(result.Errors);
                Assert.IsType<RestException>(error);

                AssertDefaultRestApiVersion(null);
            }
        }

        public class SignInAsync : ApiClientTest
        {
            [Fact]
            public async Task Returns_site_client_on_success()
            {
                var signIn = Create<SignInResponse.CredentialsType>();

                Assert.NotNull(signIn.User);
                Assert.NotNull(signIn.Site);

                Api.Data.SignIn = signIn;

                await using var result = await ApiClient.SignInAsync(Cancel);

                Assert.True(result.Success);
                Assert.NotNull(result.Value);

                AssertSession(p =>
                {
                    Assert.Equal(signIn.User.Id, p.UserId);
                    Assert.Equal(signIn.Site.Id, p.SiteId);
                    Assert.Equal(signIn.Site.ContentUrl, p.SiteContentUrl);
                });

                AssertAuthenticationToken(p =>
                {
                    Assert.Equal(signIn.Token, p.Token);
                });
            }

            [Fact]
            public async Task Returns_error_on_failure()
            {
                Api.RestApi.Auth.SignIn.RespondWithError();

                await using var result = await ApiClient.SignInAsync(Cancel);

                Assert.False(result.Success);
                Assert.Null(result.Value);

                var error = Assert.Single(result.Errors);
                Assert.IsType<RestException>(error);

                AssertSession(p =>
                {
                    Assert.Null(p.UserId);
                    Assert.Null(p.SiteId);
                    Assert.Null(p.SiteContentUrl);
                });

                AssertAuthenticationToken(p =>
                {
                    Assert.Null(p.Token);
                });
            }

            [Fact]
            public async Task Returns_error_on_invalid_credentials()
            {
                var errorBuilder = new StaticRestErrorBuilder(
                    HttpStatusCode.Unauthorized,
                    1,
                    "Login error",
                    "The credentials (name or password, or personal access token name or secret) are invalid for the specified site, or the site contentURL is invalid.");

                Api.RestApi.Auth.SignIn.RespondWithError(errorBuilder);

                await using var result = await ApiClient.SignInAsync(Cancel);

                Assert.False(result.Success);
                Assert.Null(result.Value);

                var error = Assert.Single(result.Errors);
                var restException = Assert.IsType<RestException>(error);

                Assert.Equal("401001", restException.Code);
                Assert.Equal(errorBuilder.Summary, restException.Summary);
                Assert.Equal(errorBuilder.Detail, restException.Detail);

                AssertSession(p =>
                {
                    Assert.Null(p.UserId);
                    Assert.Null(p.SiteId);
                    Assert.Null(p.SiteContentUrl);
                });

                AssertAuthenticationToken(p =>
                {
                    Assert.Null(p.Token);
                });
            }
        }

        public class SignOutAsync : ApiClientTest
        {
            [Fact]
            public async Task Returns_success()
            {
                await using var sitesClient = await GetSitesClientAsync(Cancel);

                Assert.NotNull(sitesClient);

                var result = await sitesClient.SignOutAsync(Cancel);

                Assert.True(result.Success);

                AssertAuthenticationToken(p =>
                {
                    Assert.Null(p.Token);
                });
            }

            [Fact]
            public async Task Returns_error_on_failure()
            {
                Api.RestApi.Auth.SignOut.RespondWithError();

                await using var sitesClient = await GetSitesClientAsync(Cancel);

                Assert.NotNull(sitesClient);

                var result = await sitesClient.SignOutAsync(Cancel);

                Assert.False(result.Success);

                var error = Assert.Single(result.Errors);
                Assert.IsType<RestException>(error);

                AssertAuthenticationToken(p =>
                {
                    Assert.Null(p.Token);
                });
            }
        }
    }
}