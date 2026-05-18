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
    builder.Services.AddSingleton(new ShareServiceClient(cs));
}

builder.Services.AddSingleton<IBlobRepository, BlobRepository>();
builder.Services.AddSingleton<ITableRepository, TableRepository>();
builder.Services.AddSingleton<IQueueRepository, QueueRepository>();
builder.Services.AddSingleton<IFileShareRepository, FileShareRepository>();

var app = builder.Build();

app.MapStorage();

app.Run();

// Para WebApplicationFactory<Program> en los tests de integración.
public partial class Program { }
