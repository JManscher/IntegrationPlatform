using System.Text.Json;
using IntegrationPlatform.Domain;
using IntegrationPlatform.Persistor;
using Microsoft.AspNetCore.Http.HttpResults;
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

var cosmosdb = cosmosClient.GetDatabase("integration-platform");

var container = await cosmosdb.CreateContainerIfNotExistsAsync(name, $"/{config.PartitionKeyName}");
await container.Container.SetupLastModifiedTrigger(default);

var defaultSerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

// Map the endpoint and pass the EntityType to the delegate
app.MapPost($"/{name}/persist", async Task<Ok> ([FromBody]PersistEntityEvent @event, [FromServices]CosmosClient client, [FromServices]PersistorConfiguration config, CancellationToken cancellationToken) =>
{
    var container = client.GetContainer("integration-platform", name);

    var entity = @event.Entity.Deserialize(config.EntityType, defaultSerializerOptions) ?? throw new NullReferenceException($"Entity is null or not of type {config.EntityType}");

    if(entity.GetType().BaseType != typeof(BaseDomainObject))
    {
        throw new InvalidOperationException($"Type {entity.GetType().FullName} must inh erit from {nameof(BaseDomainObject)}");
    }

    var partitionKeyValue = config.GetPartitionKeyValue(entity);
    var id = config.GetPrimaryKeyValue(entity);

    var baseEntity = entity as BaseDomainObject ?? throw new NullReferenceException("Entity is not of type BaseDomainObject");
    
    switch (@event.EventType)
    {
        case EventType.Create:
            await container.CreateItemAsync(entity, new PartitionKey(partitionKeyValue), requestOptions: new ItemRequestOptions{PreTriggers = CosmosContainerExtensions.PreTriggers}, cancellationToken: cancellationToken);
            break;
        case EventType.Update:
            await container.UpsertItemAsync(entity, new PartitionKey(partitionKeyValue), requestOptions: new ItemRequestOptions{PreTriggers = CosmosContainerExtensions.PreTriggers}, cancellationToken: cancellationToken);
            break;
        case EventType.Delete:
            var patchList = new List<PatchOperation>
            {
                PatchOperation.Add("/isDeleted", true),
                PatchOperation.Add("/ttl", config.TimeToLiveOnDelete)
            };
            await container.PatchItemAsync<BaseDomainObject>(baseEntity.Id, new PartitionKey(partitionKeyValue), patchList, requestOptions: new PatchItemRequestOptions{PreTriggers = CosmosContainerExtensions.PreTriggers}, cancellationToken: cancellationToken);
            break;
    }

    return TypedResults.Ok();
})
    .WithTopic("pubsub", $"{name}-persist");

app.MapDefaultEndpoints();

app.Run();
