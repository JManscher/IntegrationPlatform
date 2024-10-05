using System.Configuration;
using System.Reflection;
using IntegrationPlatform.Domain;

namespace IntegrationPlatform.Persistor;

public class PersistorConfiguration
{
    public PersistorConfiguration(IConfiguration configuration)
    {
        string partitionKey = configuration.GetValue<string>("ENTITY_PARTITION_KEY") ?? throw new NullReferenceException("ENTITY_PARTITION_KEY");
        string typeFullName = configuration.GetValue<string>("ENTITY_TYPE_FULLNAME") ?? throw new NullReferenceException("ENTITY_TYPE_FULLNAME");
        string name = configuration.GetValue<string>("ENTITY_NAME") ?? throw new NullReferenceException("ENTITY_NAME");

        var entityType = Type.GetType(typeFullName, true, true);

        EntityType = entityType ?? throw new NullReferenceException($"Type {typeFullName} not found");

        if(EntityType.BaseType != typeof(BaseDomainObject))
        {
            throw new InvalidOperationException($"Type {typeFullName} must inherit from {nameof(BaseDomainObject)}");
        }

        PartitionKey = EntityType.GetProperties().FirstOrDefault(p => p.Name.Equals(partitionKey, StringComparison.CurrentCultureIgnoreCase)) ?? throw new NullReferenceException($"Partition key {partitionKey} not found in entity {typeFullName}");
        PartitionKeyName = char.ToLowerInvariant(partitionKey[0]) + partitionKey[1..];
        Name = name;
        TimeToLiveOnDelete = configuration.GetValue("ENTITY_TTL_ON_DELETE", Convert.ToInt32(TimeSpan.FromHours(1).TotalSeconds));
    }
    public required Type EntityType { get; init; }
    public required PropertyInfo PartitionKey { get; init; }
    public string PartitionKeyName {get; init;}
    public string Name { get; init; }
    public int TimeToLiveOnDelete { get; init; }

    public string GetPartitionKeyValue(object entity)
    {
        return PartitionKey.GetValue(entity)?.ToString() ?? throw new NullReferenceException($"Partition key {PartitionKey.Name} not found in entity {EntityType}");
    }

    public string GetPrimaryKeyValue(object entity)
    {
        var baseObject = entity as BaseDomainObject;
        return baseObject!.Id;
    }

}
