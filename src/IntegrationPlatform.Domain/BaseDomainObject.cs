namespace IntegrationPlatform.Domain;

public class BaseDomainObject
{
    internal BaseDomainObject() { }
    public required string Id { get; init; }
    public DateTimeOffset? Modified { get; set;}
    public bool IsDeleted {get; set;}
    public int TTL {get; set;} = -1;
}
