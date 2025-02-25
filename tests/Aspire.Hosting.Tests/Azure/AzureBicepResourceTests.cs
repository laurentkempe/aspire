// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Nodes;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Publishing;
using Xunit;

namespace Aspire.Hosting.Tests.Azure;

public class AzureBicepResourceTests
{
    [Fact]
    public void AddBicepResource()
    {
        var builder = DistributedApplication.CreateBuilder();

        var bicepResource = builder.AddBicepTemplateString("mytemplate", "content")
                                   .WithParameter("param1", "value1")
                                   .WithParameter("param2", "value2");

        Assert.Equal("content", bicepResource.Resource.TemplateString);
        Assert.Equal("value1", bicepResource.Resource.Parameters["param1"]);
        Assert.Equal("value2", bicepResource.Resource.Parameters["param2"]);
    }

    [Fact]
    public void GetOutputReturnsOutputValue()
    {
        var builder = DistributedApplication.CreateBuilder();

        var bicepResource = builder.AddBicepTemplateString("templ", "content");

        bicepResource.Resource.Outputs["resourceEndpoint"] = "https://myendpoint";

        Assert.Equal("https://myendpoint", bicepResource.GetOutput("resourceEndpoint").Value);
    }

    [Fact]
    public void GetOutputValueThrowsIfNoOutput()
    {
        var builder = DistributedApplication.CreateBuilder();

        var bicepResource = builder.AddBicepTemplateString("templ", "content");

        Assert.Throws<InvalidOperationException>(() => bicepResource.GetOutput("resourceEndpoint").Value);
    }

    [Fact]
    public void AssertManifestLayout()
    {
        var builder = DistributedApplication.CreateBuilder();

        var param = builder.AddParameter("p1");

        var bicepResource = builder.AddBicepTemplateString("templ", "content")
                                    .WithParameter("param1", "value1")
                                    .WithParameter("param2", ["1", "2"])
                                    .WithParameter("param3", new JsonObject() { ["value"] = "nested" })
                                    .WithParameter("param4", param);

        bicepResource.Resource.ConnectionStringTemplate = "put this in a manifest";

        // This makes a temp file
        var obj = GetManifest(bicepResource.Resource.WriteToManifest);

        Assert.NotNull(obj);
        Assert.Equal("azure.bicep.v0", obj["type"]?.ToString());
        Assert.NotNull(obj["path"]?.ToString());
        Assert.Equal("put this in a manifest", obj["connectionString"]?.ToString());
        var parameters = obj["params"];
        Assert.NotNull(parameters);
        Assert.Equal("value1", parameters?["param1"]?.ToString());
        Assert.Equal("1", parameters?["param2"]?[0]?.ToString());
        Assert.Equal("2", parameters?["param2"]?[1]?.ToString());
        Assert.Equal("nested", parameters?["param3"]?["value"]?.ToString());
        Assert.Equal($"{{{param.Resource.Name}.value}}", parameters?["param4"]?.ToString());
    }

    [Fact]
    public void AddBicepCosmosDb()
    {
        var builder = DistributedApplication.CreateBuilder();

        var cosmos = builder.AddBicepCosmosDb("cosmos");
        cosmos.AddDatabase("db", "mydatabase");

        cosmos.Resource.Outputs["documentEndpoint"] = "https://myendpoint";
        cosmos.Resource.Outputs[cosmos.Resource.AccountKeyOutputKey] = "mykey";

        var databases = cosmos.Resource.Parameters["databases"] as IEnumerable<string>;

        Assert.Equal("Aspire.Hosting.Azure.Bicep.cosmosdb.bicep", cosmos.Resource.TemplateResourceName);
        Assert.Equal("cosmos", cosmos.Resource.Name);
        Assert.Equal("cosmos", cosmos.Resource.Parameters["databaseAccountName"]);
        Assert.NotNull(databases);
        Assert.Equal(["mydatabase"], databases);
        Assert.Equal("AccountEndpoint=https://myendpoint;AccountKey=mykey;", cosmos.Resource.GetConnectionString());
        Assert.Equal("AccountEndpoint={cosmos.outputs.documentEndpoint};AccountKey={key(Microsoft.DocumentDB/databaseAccounts@2023-04-15/{cosmos.outputs.accountName}).primaryMasterKey}", cosmos.Resource.ConnectionStringTemplate);
    }

    [Fact]
    public void AddBicepCosmosDbDatabaseReferencesParentConnectionString()
    {
        var builder = DistributedApplication.CreateBuilder();

        var db = builder.AddBicepCosmosDb("cosmos").AddDatabase("db", "mydatabase");

        var obj = GetManifest(db.Resource.WriteToManifest);

        Assert.NotNull(obj);
        Assert.Equal("azure.bicep.v0", obj["type"]?.ToString());
        Assert.Equal("{cosmos.connectionString}", obj["connectionString"]?.ToString());
        Assert.Equal("cosmos", obj["parent"]?.ToString());
    }

    [Fact]
    public void AddAppConfiguration()
    {
        var builder = DistributedApplication.CreateBuilder();

        var appConfig = builder.AddBicepAppConfiguration("appConfig");

        appConfig.Resource.Outputs["appConfigEndpoint"] = "https://myendpoint";

        Assert.Equal("Aspire.Hosting.Azure.Bicep.appconfig.bicep", appConfig.Resource.TemplateResourceName);
        Assert.Equal("appConfig", appConfig.Resource.Name);
        Assert.Equal("appconfig", appConfig.Resource.Parameters["configName"]);
        Assert.Equal("https://myendpoint", appConfig.Resource.GetConnectionString());
        Assert.Equal("{appConfig.outputs.appConfigEndpoint}", appConfig.Resource.ConnectionStringTemplate);
    }

    [Fact]
    public void AddBicepRedis()
    {
        var builder = DistributedApplication.CreateBuilder();

        var redis = builder.AddBicepAzureRedis("redis");

        redis.Resource.Outputs["hostName"] = "myhost";
        redis.Resource.Outputs[redis.Resource.AccountKeyOutputKey] = "mykey";

        Assert.Equal("Aspire.Hosting.Azure.Bicep.redis.bicep", redis.Resource.TemplateResourceName);
        Assert.Equal("redis", redis.Resource.Name);
        Assert.Equal("redis", redis.Resource.Parameters["redisCacheName"]);
        Assert.Equal("myhost,ssl=true,password=mykey", redis.Resource.GetConnectionString());
        Assert.Equal("{redis.outputs.hostName},ssl=true,password={key(Microsoft.Cache/redis@2023-04-15/{redis.outputs.cacheName}).primaryKey}", redis.Resource.ConnectionStringTemplate);
    }

    [Fact]
    public void AddBicepKeyVault()
    {
        var builder = DistributedApplication.CreateBuilder();

        var keyVault = builder.AddBicepKeyVault("keyVault");

        keyVault.Resource.Outputs["vaultUri"] = "https://myvault";

        Assert.Equal("Aspire.Hosting.Azure.Bicep.keyvault.bicep", keyVault.Resource.TemplateResourceName);
        Assert.Equal("keyVault", keyVault.Resource.Name);
        Assert.Equal("keyvault", keyVault.Resource.Parameters["vaultName"]);
        Assert.Equal("https://myvault", keyVault.Resource.GetConnectionString());
        Assert.Equal("{keyVault.outputs.vaultUri}", keyVault.Resource.ConnectionStringTemplate);
    }

    [Fact]
    public void AddBicepSqlServer()
    {
        var builder = DistributedApplication.CreateBuilder();

        var sql = builder.AddBicepAzureSqlServer("sql");
        sql.AddDatabase("db", "database");

        sql.Resource.Outputs["sqlServerFqdn"] = "myserver";

        var databases = sql.Resource.Parameters["databases"] as IEnumerable<string>;

        Assert.Equal("Aspire.Hosting.Azure.Bicep.sql.bicep", sql.Resource.TemplateResourceName);
        Assert.Equal("sql", sql.Resource.Name);
        Assert.Equal("sql", sql.Resource.Parameters["serverName"]);
        Assert.NotNull(databases);
        Assert.Equal(["database"], databases);
        Assert.Equal("Server=tcp:myserver,1433;Encrypt=True;Authentication=\"Active Directory Default\"", sql.Resource.GetConnectionString());
        Assert.Equal("Server=tcp:{sql.outputs.sqlServerFqdn},1433;Encrypt=True;Authentication=\"Active Directory Default\"", sql.Resource.ConnectionStringTemplate);
    }

    [Fact]
    public void AddBicepServiceBus()
    {
        var builder = DistributedApplication.CreateBuilder();

        var sb = builder.AddBicepAzureServiceBus("sb", ["queue1"], ["topic1"]);

        sb.Resource.Outputs["serviceBusEndpoint"] = "mynamespaceEndpoint";

        var queues = sb.Resource.Parameters["queues"] as IEnumerable<string>;
        var topics = sb.Resource.Parameters["topics"] as IEnumerable<string>;

        Assert.Equal("Aspire.Hosting.Azure.Bicep.servicebus.bicep", sb.Resource.TemplateResourceName);
        Assert.Equal("sb", sb.Resource.Name);
        Assert.Equal("sb", sb.Resource.Parameters["serviceBusNamespaceName"]);
        Assert.NotNull(queues);
        Assert.Equal(["queue1"], queues);
        Assert.NotNull(topics);
        Assert.Equal(["topic1"], topics);
        Assert.Equal("mynamespaceEndpoint", sb.Resource.GetConnectionString());
        Assert.Equal("{sb.outputs.serviceBusEndpoint}", sb.Resource.ConnectionStringTemplate);
    }

    [Fact]
    public void AddBicepStorage()
    {
        var builder = DistributedApplication.CreateBuilder();

        var storage = builder.AddAzureBicepAzureStorage("storage");

        storage.Resource.Outputs["blobEndpoint"] = "https://myblob";
        storage.Resource.Outputs["queueEndpoint"] = "https://myqueue";
        storage.Resource.Outputs["tableEndpoint"] = "https://mytable";

        var blob = storage.AddBlob("blob");
        var queue = storage.AddQueue("queue");
        var table = storage.AddTable("table");

        Assert.Equal("Aspire.Hosting.Azure.Bicep.storage.bicep", storage.Resource.TemplateResourceName);
        Assert.Equal("storage", storage.Resource.Name);
        Assert.Equal("storage", storage.Resource.Parameters["storageName"]);
        Assert.Null(storage.Resource.ConnectionStringTemplate);

        Assert.Equal("https://myblob", blob.Resource.GetConnectionString());
        Assert.Equal("https://myqueue", queue.Resource.GetConnectionString());
        Assert.Equal("https://mytable", table.Resource.GetConnectionString());

        var blobManifest = GetManifest(blob.Resource.WriteToManifest);
        Assert.Equal("{storage.outputs.blobEndpoint}", blobManifest["connectionString"]?.ToString());
        Assert.Equal("storage", blobManifest["parent"]?.ToString());

        var queueManifest = GetManifest(queue.Resource.WriteToManifest);
        Assert.Equal("{storage.outputs.queueEndpoint}", queueManifest["connectionString"]?.ToString());
        Assert.Equal("storage", blobManifest["parent"]?.ToString());

        var tableManifest = GetManifest(table.Resource.WriteToManifest);
        Assert.Equal("{storage.outputs.tableEndpoint}", tableManifest["connectionString"]?.ToString());
        Assert.Equal("storage", blobManifest["parent"]?.ToString());
    }

    private static JsonNode GetManifest(Action<ManifestPublishingContext> writeManifest)
    {
        using var ms = new MemoryStream();
        var writer = new Utf8JsonWriter(ms);
        writer.WriteStartObject();
        writeManifest(new ManifestPublishingContext(Environment.CurrentDirectory, writer));
        writer.WriteEndObject();
        writer.Flush();
        ms.Position = 0;
        var obj = JsonNode.Parse(ms);
        Assert.NotNull(obj);
        return obj;
    }
}
