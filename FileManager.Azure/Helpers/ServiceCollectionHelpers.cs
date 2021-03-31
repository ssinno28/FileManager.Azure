using System;
using System.Runtime.InteropServices;
using FileManager.Azure.Interfaces;
using FileManager.Azure.Models;
using FileManager.Azure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FileManager.Azure.Helpers
{
    public static class ServiceCollectionHelpers
    {
        public static void AddFileManager(this IServiceCollection serviceCollection, IConfiguration configuration)
        {
            serviceCollection.AddOptions();
            serviceCollection.Configure<StorageOptions>(configuration);

            serviceCollection.AddTransient(provider => new Func<string, IFileManagerService>(container =>
            {
                return new FileManagerService(
                    provider.GetService<IOptions<StorageOptions>>(),
                    provider.GetService<IHttpContextAccessor>(),
                    container);
            }));
        }
    }
}