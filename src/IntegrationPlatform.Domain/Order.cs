using Newtonsoft.Json;

namespace IntegrationPlatform.Domain;

public class Order : BaseDomainObject
{
    public string CustomerId { get; set; }

    public string OrderNumber { get; set; }

    public string OrderDate { get; set; }

    public string OrderStatus { get; set; }

    public string OrderTotal { get; set; }

    public List<OrderItem> OrderItems { get; set; }

}

public class OrderItem
{
    public string Id { get; set; }

    public string ProductId { get; set; }

    public string Quantity { get; set; }

    public string Price { get; set; }

    public string Total { get; set; }
}