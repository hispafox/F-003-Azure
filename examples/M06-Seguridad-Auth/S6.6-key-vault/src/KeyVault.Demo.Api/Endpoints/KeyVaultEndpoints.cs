using KeyVault.Demo.Api.KeyVault;

namespace KeyVault.Demo.Api.Endpoints;

public sealed record ReferenciaDto(string Valor);
public sealed record RotacionDto(DateTimeOffset Expira, DateTimeOffset Ahora, int? VentanaDias);
public sealed record PlanDto(QueGuardar Que, AccesoKv Acceso, string VaultName, string ItemName);

public static class KeyVaultEndpoints
{
    public static void MapKeyVault(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

        var kv = app.MapGroup("/kv");

        // Slides 2-5 — ¿dónde va esto y con qué rol mínimo?
        kv.MapGet("/donde", (QueGuardar que, AccesoKv acceso) =>
        {
            var d = KeyVaultItemAdvisor.Donde(que);
            return Results.Ok(new
            {
                que = que.ToString(),
                destino = d.ToString(),
                vaAKeyVault = d != Destino.ManagedIdentity,
                rolMinimo = KeyVaultItemAdvisor.RolMinimo(d, acceso),
            });
        });

        // Slide 6 — construir una Key Vault Reference.
        kv.MapGet("/referencia", (string vault, string secret, string? version) =>
            Results.Ok(new
            {
                referencia = KeyVaultReference.Construir(vault, secret, version),
            }));

        // Slide 6 — parsear una Key Vault Reference.
        kv.MapPost("/referencia/parse", (ReferenciaDto dto) =>
        {
            if (!KeyVaultReference.EsReferencia(dto.Valor))
                return Results.BadRequest(new { error = "No es una Key Vault Reference" });
            return Results.Ok(KeyVaultReference.Parsear(dto.Valor));
        });

        // Slides 8-9 — ¿toca rotar el secreto?
        kv.MapPost("/rotacion", (RotacionDto dto) =>
            Results.Ok(SecretRotationPolicy.Evaluar(
                dto.Expira, dto.Ahora,
                dto.VentanaDias ?? SecretRotationPolicy.VentanaDiasPorDefecto)));

        // Slides 2-6 — plan de almacenamiento completo.
        kv.MapPost("/plan", (PlanDto dto, IKeyVaultPlanner planner) =>
            Results.Ok(planner.Planificar(
                dto.Que, dto.Acceso, dto.VaultName, dto.ItemName)));
    }
}
