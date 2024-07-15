using System.Dynamic;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Linq;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace IntegrationPlatform.Persistor;

public class PersistEntityEvent
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public EventType EventType { get; set; }
    public required JsonObject Entity { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EventType
{
    Create,
    Update,
    Delete
}

public class PersistEntityEventSchemaFilter(PersistorConfiguration configuration) : ISchemaFilter
{
    private readonly PersistorConfiguration configuration = configuration;

    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type == typeof(PersistEntityEvent))
        {
            schema.Properties[nameof(PersistEntityEvent.Entity).ToLowerInvariant()] = new OpenApiSchema
            {
              Reference = new OpenApiReference
              {
                  Id = configuration.EntityType.Name,
                  Type = ReferenceType.Schema
              }
            };
        }
    }
}

public class PersistEntityEventDocumentFilter(PersistorConfiguration configuration) : IDocumentFilter
{
    private readonly PersistorConfiguration configuration = configuration;

    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        context.SchemaGenerator.GenerateSchema(configuration.EntityType, context.SchemaRepository);
        context.SchemaRepository.Schemas.Remove(nameof(JToken));
    }
}