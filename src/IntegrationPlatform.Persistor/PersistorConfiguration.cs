using System.Reflection;
using IntegrationPlatform.Domain;

namespace IntegrationPlatform.Persistor;

public class PersistorConfiguration
{
    public PersistorConfiguration(IConfiguration configuration)
    {
        string primaryKey = configuration.GetValue<string>("ENTITY_PRIMARY_KEY") ?? throw new ArgumentNullException("ENTITY_PRIMARY_KEY");
        string partitionKey = configuration.GetValue<string>("ENTITY_PARTITION_KEY") ?? throw new ArgumentNullException("ENTITY_PARTITION_KEY");
        string typeFullName = configuration.GetValue<string>("ENTITY_TYPE_FULLNAME") ?? throw new ArgumentNullException("ENTITY_TYPE_FULLNAME");

        var entityType = Type.GetType(typeFullName, true, true);

        EntityType = entityType ?? throw new NullReferenceException($"Type {typeFullName} not found");

        if(EntityType.BaseType != typeof(BaseDomainObject))
        {
            throw new InvalidOperationException($"Type {typeFullName} must inherit from {nameof(BaseDomainObject)}");
        }

        PartitionKey = EntityType.GetProperties().FirstOrDefault(p => p.Name.Equals(partitionKey, StringComparison.CurrentCultureIgnoreCase)) ?? throw new NullReferenceException($"Partition key {partitionKey} not found in entity {typeFullName}");
        PrimaryKey = EntityType.GetProperties().FirstOrDefault(p => p.Name.Equals(primaryKey, StringComparison.InvariantCultureIgnoreCase)) ?? throw new NullReferenceException($"primaryKey {primaryKey} not found in entity {typeFullName}");
        PartitionKeyName = char.ToLowerInvariant(partitionKey[0]) + partitionKey[1..];
    }
    public required Type EntityType { get; init; }
    public required PropertyInfo PartitionKey { get; init; }
    public string PartitionKeyName {get; init;}
    
    public required PropertyInfo PrimaryKey { get; init; }
    public string GetPartitionKeyValue(object entity)
    {
        return PartitionKey.GetValue(entity)?.ToString() ?? throw new NullReferenceException($"Partition key {PartitionKey.Name} not found in entity {EntityType}");
    }

    public string GetPrimaryKeyValue(object entity)
    {
        return PrimaryKey.GetValue(entity)?.ToString() ?? throw new NullReferenceException($"Primary key {PrimaryKey.Name} not found in entity {EntityType}");
    }

}
