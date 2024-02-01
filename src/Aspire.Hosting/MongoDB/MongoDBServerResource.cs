// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.MongoDB;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a MongoDB container.
/// </summary>
/// <param name="name">The name of the resource.</param>
public class MongoDBServerResource(string name) : Resource(name), IMongoDBParentResource
{
    /// <summary>
    /// Gets the connection string for the MongoDB server.
    /// </summary>
    /// <returns>A connection string for the MongoDB server in the form "mongodb://host:port".</returns>
    public void EvaluateConnectionString(ConnectionStringCallbackContext context)
    {
        if (!this.TryGetAllocatedEndPoints(out var allocatedEndpoints))
        {
            throw new DistributedApplicationException("Expected allocated endpoints!");
        }

        var allocatedEndpoint = allocatedEndpoints.Single();

        context.ConnectionString = new MongoDBConnectionStringBuilder()
            .WithServer(allocatedEndpoint.Address)
            .WithPort(allocatedEndpoint.Port)
            .Build();
    }
}
