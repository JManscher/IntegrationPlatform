using System.Text.Json;
using System.Text.Json.Serialization;
using IntegrationPlatform.Domain;
using IntegrationPlatform.Persistor;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.AddAzureCosmosClient("cosmosConnectionName", configureClientOptions: options => {
    options.SerializerOptions = new CosmosSerializationOptions
    {
        PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
    };
});

var name = builder.Configuration.GetValue<string>("ENTITY_NAME");

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => {
    var title = $"{char.ToUpper(name![0])}{name.AsSpan()[1..]} Persistor";
    options.SwaggerDoc("v1", new OpenApiInfo { Title = title, Version = "v1" });
    options.SchemaFilter<PersistEntityEventSchemaFilter>();
    options.DocumentFilter<PersistEntityEventDocumentFilter>();
});

builder.AddServiceDefaults();

builder.Services.AddSingleton<PersistorConfiguration>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.AddDaprSubscriber();

var cosmosClient = app.Services.GetRequiredService<CosmosClient>();
var config = app.Services.GetRequiredService<PersistorConfiguration>();

var cosmosdb = await cosmosClient.CreateDatabaseIfNotExistsAsync("integration-platform");
await cosmosdb.Database.CreateContainerIfNotExistsAsync(name, $"/{config.PartitionKeyName}");

var defaultSerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

// Map the endpoint and pass the EntityType to the delegate
app.MapPost($"/{name}/persist", async ([FromBody]PersistEntityEvent @event, [FromServices]CosmosClient client, [FromServices]PersistorConfiguration config, CancellationToken cancellationToken) =>
{
    var container = client.GetContainer("integration-platform", name);

    var entity = @event.Entity.Deserialize(config.EntityType, defaultSerializerOptions) ?? throw new NullReferenceException($"Entity is null or not of type {config.EntityType}");

    var partitionKeyValue = config.GetPartitionKeyValue(entity);
    var id = config.GetPrimaryKeyValue(entity);

    entity.GetType().GetProperty("Modified")?.SetValue(entity, DateTimeOffset.UtcNow);
    switch (@event.EventType)
    {
        case EventType.Create:
            await container.CreateItemAsync(entity, new PartitionKey(partitionKeyValue), cancellationToken: cancellationToken);
            break;
        case EventType.Update:
            await container.UpsertItemAsync(entity, new PartitionKey(partitionKeyValue), cancellationToken: cancellationToken);
            break;
        case EventType.Delete:
            //TODO: Should use TTL instead of deleting the item, and flag the item as deleted in the database
            //await container.DeleteItemAsync<object>(id, new PartitionKey(partitionKeyValue));
            entity.GetType().GetProperty("IsDeleted")?.SetValue(entity, true);
            var ttlProperty = entity.GetType().GetProperty("TTL");
            await container.UpsertItemAsync(entity, new PartitionKey(partitionKeyValue), cancellationToken: cancellationToken);
            break;
    }

    return TypedResults.Ok();
})
    .WithTopic("pubsub", $"{name}-persist");

app.MapDefaultEndpoints();

app.Run();