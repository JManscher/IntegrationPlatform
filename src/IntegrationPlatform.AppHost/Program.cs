using Aspire.Hosting.Dapr;
using IntegrationPlatform.AppHost.Entity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis");

builder.AddDapr((options) =>
{
    options.EnableTelemetry = true;
});


var entityLocation =  builder.Configuration.GetValue<string>("CONFIGURATION_ENTITIES") ?? "../../configuration/entities"; // Replace with the actual directory path

var entityConfigurations =  EntityConfigurator.GetConfigurations(entityLocation);

foreach (var entityConfiguration in entityConfigurations)
{
    builder.AddProject<IntegrationPlatform_Persistor>(entityConfiguration.Name)
        .WithDaprSidecar(entityConfiguration.Name)
        .WithEnvironment("ENTITY_NAME", entityConfiguration.Name)
        .WithEnvironment("ENTITY_PRIMARY_KEY", entityConfiguration.Config.PrimaryKey)
        .WithEnvironment("ENTITY_PARTITION_KEY", entityConfiguration.Config.PartitionKey)
        .WithEnvironment("ENTITY_TYPE_FULLNAME", entityConfiguration.Config.TypeFullName);
}
 

if (builder.Configuration.GetValue<string>("DAPR_CLI_PATH") is { } daprCliPath)
    builder.Services.Configure<DaprOptions>(options => { options.DaprPath = daprCliPath; });

builder.Build().Run();
 