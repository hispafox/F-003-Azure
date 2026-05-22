using Azure.Data.Tables;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Files.Shares;
using Azure.Storage.Queues;
using Storage.Demo.Api.Endpoints;
using Storage.Demo.Api.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Slide 7 — dos modos de conexión:
//   - Connection string (StorageConnection): desarrollo / Azurite.
//   - Managed Identity (StorageAccountUri): producción, sin secretos.
// Si hay URI configurada, se usa DefaultAzureCredential (slide 7/M05-S5.4).
var conn = builder.Configuration["StorageConnection"];
var accountUri = builder.Configuration["StorageAccountUri"];

if (!string.IsNullOrWhiteSpace(accountUri))
{
    var cred = new DefaultAzureCredential();
    builder.Services.AddSingleton(new BlobServiceClient(new Uri($"{accountUri}/"), cred));
    builder.Services.AddSingleton(new QueueServiceClient(new Uri($"{accountUri}/"), cred));
    builder.Services.AddSingleton(new TableServiceClient(new Uri($"{accountUri}/"), cred));
    builder.Services.AddSingleton(new ShareServiceClient(new Uri($"{accountUri}/"), cred));
}
else
{
    var cs = conn ?? "UseDevelopmentStorage=true"; // Azurite por defecto
    builder.Services.AddSingleton(new BlobServiceClient(cs));
    builder.Services.AddSingleton(new QueueServiceClient(cs));
    builder.Services.AddSingleton(new TableServiceClient(cs));
    // Azurite no emula Azure Files y el atajo "UseDevelopmentStorage=true" no
    // expande FileEndpoint → ShareServiceClient(cs) lanza NullReferenceException
    // al construirse. Le damos un FileEndpoint explícito para que el cliente se
    // cree (el contrato existe para ver el SDK; el File real va contra un
    // Storage real — ver README).
    builder.Services.AddSingleton(new ShareServiceClient(DevFileConnectionString(cs)));
}

// El atajo de Azurite no define FileEndpoint; lo añadimos para Azure Files.
static string DevFileConnectionString(string cs) =>
    cs == "UseDevelopmentStorage=true"
        ? "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;"
          + "AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;"
          + "BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;"
          + "QueueEndpoint=http://127.0.0.1:10001/devstoreaccount1;"
          + "TableEndpoint=http://127.0.0.1:10002/devstoreaccount1;"
          + "FileEndpoint=http://127.0.0.1:10004/devstoreaccount1;"
        : cs;

builder.Services.AddSingleton<IBlobRepository, BlobRepository>();
builder.Services.AddSingleton<ITableRepository, TableRepository>();
builder.Services.AddSingleton<IQueueRepository, QueueRepository>();
builder.Services.AddSingleton<IFileShareRepository, FileShareRepository>();

var app = builder.Build();

app.MapStorage();

app.Run();

// Para WebApplicationFactory<Program> en los tests de integración.
public partial class Program { }
