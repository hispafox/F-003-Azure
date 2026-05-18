namespace Cosmos.Demo.Api.Cosmos;

// Constantes del emulador local de Cosmos DB. La clave NO es un secreto:
// es la clave fija y PÚBLICA del emulador, documentada por Microsoft
// (https://learn.microsoft.com/azure/cosmos-db/emulator). Solo sirve
// contra el emulador local; jamás contra una cuenta real.
public static class CosmosDefaults
{
    public const string EmuladorConnectionString =
        "AccountEndpoint=https://localhost:8081/;" +
        "AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==;";

    public const string Database = "tienda";
    public const string Container = "pedidos";
    public const string PartitionKeyPath = "/clienteId"; // slide 6
}
