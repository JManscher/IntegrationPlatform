using Newtonsoft.Json;

namespace IntegrationPlatform.Domain;

public class Customer : BaseDomainObject
{
    public string Name { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public string Address { get; set; }
    public string City { get; set; }
    public string State { get; set; }
    public string Zip { get; set; }
    public string Country { get; set; }
}
