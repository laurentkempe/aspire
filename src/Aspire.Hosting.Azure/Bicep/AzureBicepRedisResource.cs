// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Represents an Azure Redis resource.
/// </summary>
/// <param name="name">The name of the resource.</param>
public class AzureBicepRedisResource(string name) :
    AzureBicepResource(name, templateResouceName: "Aspire.Hosting.Azure.Bicep.redis.bicep"),
    IResourceWithConnectionString
{
    public string ResourceNameOutputKey => "cacheName";

    public string AccountKeyOutputKey => "accountKey";

    /// <summary>
    /// Gets the connection string for the Azure Redis resource.
    /// </summary>
    /// <returns>The connection string for the Azure Redis resource.</returns>
    public string? GetConnectionString()
    {
        return $"{Outputs["hostName"]},ssl=true,password={Outputs[AccountKeyOutputKey]}";
    }
}

/// <summary>
/// Provides extension methods for adding the Azure Redis resources to the application model.
/// </summary>
public static class AzureBicepRedisExtensions
{
    /// <summary>
    /// Adds an Azure Redis resource to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureBicepRedisResource> AddBicepAzureRedis(this IDistributedApplicationBuilder builder, string name)
    {
        var resource = new AzureBicepRedisResource(name)
        {
            ConnectionStringTemplate = $"{{{name}.outputs.hostName}},ssl=true,password={{key(Microsoft.Cache/redis@2023-04-15/{{{name}.outputs.cacheName}}).primaryKey}}"
        };

        return builder.AddResource(resource)
                    .WithParameter("redisCacheName", resource.CreateBicepResourceName())
                    .WithManifestPublishingCallback(resource.WriteToManifest);
    }
}
