namespace IntegrationPlatform.Domain;

public abstract class BaseDomainObject
{
    public required string Id { get; init; }
    public DateTimeOffset? Modified { get; set;}
    public bool IsDeleted {get; set;}
}
