var builder = DistributedApplication.CreateBuilder(args);

var catalogDb = builder.Environment.EnvironmentName switch
{
    "Development2" => builder.AddPostgres("postgres").WithPgAdmin().AddDatabase("catalogdb"),
    _ => builder.AddConnectionString("ratingsdb")
};

var basketCache = builder.AddRedis("basketcache")
                         .WithRedisCommander();

var catalogService = builder.AddProject<Projects.CatalogService>("catalogservice")
                            .WithReference(catalogDb);

var messaging = builder.AddRabbitMQContainer("messaging");

var basketService = builder.AddProject("basketservice", @"..\BasketService\BasketService.csproj")
                           .WithReference(basketCache)
                           .WithReference(messaging);

builder.AddProject<Projects.MyFrontend>("frontend")
       .WithReference(basketService)
       .WithReference(catalogService.GetEndpoint("http"));

builder.AddProject<Projects.OrderProcessor>("orderprocessor")
       .WithReference(messaging)
       .WithLaunchProfile("OrderProcessor");

builder.AddProject<Projects.ApiGateway>("apigateway")
       .WithReference(basketService)
       .WithReference(catalogService);

builder.AddProject<Projects.CatalogDb>("catalogdbapp")
       .WithReference(catalogDb);

// This project is only added in playground projects to support development/debugging
// of the dashboard. It is not required in end developer code. Comment out this code
// to test end developer dashboard launch experience. Refer to Directory.Build.props
// for the path to the dashboard binary (defaults to the Aspire.Dashboard bin output
// in the artifacts dir).
builder.AddProject<Projects.Aspire_Dashboard>(KnownResourceNames.AspireDashboard);

builder.Build().Run();
