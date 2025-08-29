namespace DataSync.Functions.Flows.connectors.HomeDepot.Models;

public sealed class HomeDepotOrder
{
    public string OrderId { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string Phone { get; set; } = "";
    public string City { get; set; } = "";
    public string State { get; set; } = "";
    public string PostalCode { get; set; } = "";
    public string Status { get; set; } = "new";
    public DateTime UpdatedUtc { get; set; }
}