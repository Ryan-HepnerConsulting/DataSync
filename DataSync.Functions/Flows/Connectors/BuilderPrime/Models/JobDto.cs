namespace DataSync.Functions.Flows.connectors.BuilderPrime.Models;

public sealed class BuilderPrimeJob
{
    public string ExternalId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Phone { get; set; } = "";
    public string City { get; set; } = "";
    public string State { get; set; } = "";
    public string PostalCode { get; set; } = "";
    public string Stage { get; set; } = "New";
}