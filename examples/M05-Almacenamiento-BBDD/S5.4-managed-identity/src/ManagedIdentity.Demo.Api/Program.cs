using Azure.Core;
using Azure.Storage.Blobs;
using ManagedIdentity.Demo.Api.Endpoints;
using ManagedIdentity.Demo.Api.Security;

var builder = WebApplication.CreateBuilder(args);
var cfg = builder.Configuration;

// Slide 21 — UN ÚNICO TokenCredential singleton, compartido por todos
// los clientes. Cachea el token internamente; crear credenciales o
// clientes por request es el anti-pattern (token thrashing).
builder.Services.AddSingleton<TokenCredential>(_ => CredentialFactory.Crear(cfg));

// Slides 6, 8, 21 — cliente conectado SOLO con la credencial (sin key).
// Construir el cliente NO autentica (lazy); el token se pide en la 1ª
// llamada real. Endpoint placeholder si no hay Storage configurado.
builder.Services.AddSingleton(sp =>
{
    var credential = sp.GetRequiredService<TokenCredential>();
    var endpoint = cfg["StorageBlobEndpoint"];
    if (string.IsNullOrWhiteSpace(endpoint))
        endpoint = "https://devstoreaccount1.blob.core.windows.net";
    return new BlobServiceClient(new Uri(endpoint), credential);
});

var app = builder.Build();

app.MapSeguridad();

app.Run();

// Para WebApplicationFactory<Program> en los tests.
public partial class Program { }
