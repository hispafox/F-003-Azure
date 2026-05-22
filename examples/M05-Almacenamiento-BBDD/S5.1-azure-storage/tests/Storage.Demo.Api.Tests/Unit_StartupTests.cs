using Azure.Storage.Files.Shares;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Storage.Demo.Api.Tests;

// CAPA 1 — regresión: con la config dev por defecto
// (StorageConnection=UseDevelopmentStorage=true), arrancar el host construye
// los 4 clientes de Storage en Program.cs. El atajo de Azurite NO expande
// FileEndpoint, así que `new ShareServiceClient(cs)` lanzaba
// NullReferenceException y la app no arrancaba. No necesita Docker: solo se
// construye el cliente (no hay llamada a Azure).
public class Unit_StartupTests
{
    [Fact]
    public void Host_Arranca_Con_Config_Dev_Por_Defecto()
    {
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(b => b.UseSetting("StorageConnection", "UseDevelopmentStorage=true"));

        // Forzar la construcción del host y resolver el cliente de File.
        var share = factory.Services.GetRequiredService<ShareServiceClient>();

        Assert.NotNull(share);
    }
}
