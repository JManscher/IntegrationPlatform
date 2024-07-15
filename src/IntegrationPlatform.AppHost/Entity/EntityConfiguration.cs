namespace IntegrationPlatform.AppHost.Entity
{
    public record EntityConfiguration
    {
        public required string Kind { get; init; }
        public required string Name { get; init; }
        public required Config Config { get; init; }
    }

    public record Config
    {

        public required string PrimaryKey { get; init; }
        public required string PartitionKey { get; init; }  

        public required string TypeFullName {get; init;}
    }
}