namespace Cosmos.Mi.Demo.Api.Cosmos;

// Slide 4 — db "tienda" / container "productos" con partition key
// /categoria. La clave del emulador es PÚBLICA (documentada por
// Microsoft), solo vale contra el emulador local, jamás contra Azure.
public static class CosmosDefaults
{
    public const string Database = "tienda";
    public const string Container = "productos";
    public const string PartitionKeyPath = "/categoria"; // slide 4/11

    public const string EmuladorConnectionString =
        "AccountEndpoint=https://localhost:8081/;" +
        "AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==;";
}
