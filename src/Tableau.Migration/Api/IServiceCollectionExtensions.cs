﻿using System.IO.Abstractions;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Tableau.Migration.Api.Labels;
using Tableau.Migration.Api.Permissions;
using Tableau.Migration.Api.Publishing;
using Tableau.Migration.Api.Search;
using Tableau.Migration.Api.Simulation;
using Tableau.Migration.Api.Tags;
using Tableau.Migration.Content.Files;
using Tableau.Migration.Net;
using Tableau.Migration.Net.Simulation;

namespace Tableau.Migration.Api
{
    /// <summary>
    /// Static class containing API client extension methods for <see cref="IServiceCollection"/> objects.
    /// </summary>
    internal static class IServiceCollectionExtensions
    {
        /// <summary>
        /// Registers migration API client services.
        /// </summary>
        /// <param name="services">The service collection to register services with.</param>
        /// <returns>The same service collection for fluent API calls.</returns>
        internal static IServiceCollection AddMigrationApiClient(this IServiceCollection services)
        {
            //Check for HTTP dependencies and add them if they haven't already been added.
            if (!services.Any(s => s.ServiceType == typeof(IHttpClient)))
                services.AddHttpServices();

            services.AddSingleton<IFileSystem, FileSystem>();
            services.AddSingleton<ITaskDelayer, TaskDelayer>();

            //Bootstrap and scope state tracking services.
            services.AddScoped<ApiClientInput>();
            services.AddScoped<IApiClientInput>(p => p.GetRequiredService<ApiClientInput>());
            services.AddScoped<IApiClientInputInitializer>(p => p.GetRequiredService<ApiClientInput>());
            services.AddScoped<IScopedApiClientFactory, ScopedApiClientFactory>();
            services.AddScoped<IPermissionsApiClientFactory, PermissionsApiClientFactory>();
            services.AddScoped<ITagsApiClientFactory, TagsApiClientFactory>();
            services.AddScoped<IViewsApiClientFactory, ViewsApiClientFactory>();
            services.AddScoped<ILabelsApiClientFactory, LabelsApiClientFactory>();

            //Main API client.
            services.AddTransient<IApiClient, ApiClient>();
            services.AddTransient<IGroupsApiClient, GroupsApiClient>();
            services.AddTransient<IJobsApiClient, JobsApiClient>();
            services.AddTransient<IProjectsApiClient, ProjectsApiClient>();
            services.AddTransient<ISitesApiClient, SitesApiClient>();
            services.AddTransient<IUsersApiClient, UsersApiClient>();
            services.AddTransient<IDataSourcesApiClient, DataSourcesApiClient>();
            services.AddTransient<IWorkbooksApiClient, WorkbooksApiClient>();
            services.AddTransient<IViewsApiClient, ViewsApiClient>();

            //API Simulator.
            services.AddSingleton<ITableauApiSimulatorFactory, TableauApiSimulatorFactory>();
            services.AddSingleton<ITableauApiSimulatorCollection, TableauApiSimulatorCollection>();
            services.AddSingleton<IResponseSimulatorProvider, TableauApiResponseSimulatorProvider>();

            //Publishing services.
            services.AddScoped<IDataSourcePublisher, DataSourcePublisher>();
            services.AddScoped<IWorkbookPublisher, WorkbookPublisher>();
            services.AddScoped<IConnectionManager, ConnectionManager>();
            services.AddScoped(typeof(ILabelsApiClient<>), typeof(LabelsApiClient<>));

            //Non-Engine content search services.
            services.AddScoped<ApiContentReferenceFinderFactory>();
            services.AddScoped(p => p.GetRequiredService<ApiClientInput>().ContentReferenceFinderFactory);
            services.AddScoped(typeof(BulkApiContentReferenceCache<>));

            //Non-Engine content file services.
            services.AddSingleton<IContentFilePathResolver, ContentTypeFilePathResolver>();
            services.AddSingleton<ISymmetricEncryptionFactory, Aes256EncryptionFactory>();
            services.AddScoped<TemporaryDirectoryContentFileStore>();
            services.AddScoped(p => new EncryptedFileStore(p, p.GetRequiredService<TemporaryDirectoryContentFileStore>()));
            services.AddScoped(p => p.GetRequiredService<ApiClientInput>().FileStore);

            return services;
        }
    }
}