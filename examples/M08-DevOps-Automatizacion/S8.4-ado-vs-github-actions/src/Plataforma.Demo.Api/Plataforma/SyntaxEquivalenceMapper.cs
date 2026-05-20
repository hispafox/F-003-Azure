namespace Plataforma.Demo.Api.Plataforma;

public sealed record Equivalencia(string Concepto, string AdoYaml, string GitHubYaml);

// Slide 6 — equivalencias sintácticas entre Azure Pipelines YAML y
// GitHub Actions YAML. Tabla pura: misma lógica, sintaxis distinta.
public static class SyntaxEquivalenceMapper
{
    public static IReadOnlyList<Equivalencia> Todas { get; } =
    [
        new("Jerarquía",
            "stages: -> jobs: -> steps:",
            "jobs: -> steps:  (sin stages)"),
        new("Trigger en main",
            "trigger:\n  branches:\n    include: [main]",
            "on:\n  push:\n    branches: [main]"),
        new("Pull request",
            "pr:\n  branches:\n    include: [main]",
            "on:\n  pull_request:\n    branches: [main]"),
        new("Cron / schedule",
            "schedules:\n- cron: '0 2 * * *'",
            "on:\n  schedule:\n  - cron: '0 2 * * *'"),
        new("Pool / runner",
            "pool:\n  vmImage: ubuntu-latest",
            "runs-on: ubuntu-latest"),
        new("Setup .NET",
            "task: UseDotNet@2\n  inputs: { version: 8.x }",
            "uses: actions/setup-dotnet@v4\n  with: { dotnet-version: 8.x }"),
        new("Checkout (implícito vs explícito)",
            "(automático)",
            "uses: actions/checkout@v4"),
        new("Deploy App Service",
            "task: AzureWebApp@1",
            "uses: azure/webapps-deploy@v3"),
        new("Login Azure",
            "azureSubscription: 'Service-Connection'",
            "uses: azure/login@v2\n  with: { creds: ${{ secrets.AZURE_CREDENTIALS }} }"),
        new("Subir artifact",
            "publish: $(Build.ArtifactStagingDirectory)\n  artifact: app",
            "uses: actions/upload-artifact@v4\n  with: { name: app, path: ... }"),
        new("Descargar artifact",
            "(automático con `download:`)",
            "uses: actions/download-artifact@v4"),
        new("Variable inline",
            "$(buildConfiguration)",
            "${{ env.BUILD_CONFIGURATION }} o ${{ vars.NAME }}"),
        new("Secreto",
            "$(StripeApiKey) (Variable Group)",
            "${{ secrets.STRIPE_API_KEY }}"),
        new("Job depende de otro",
            "dependsOn: Build",
            "needs: build"),
        new("Condición",
            "condition: succeeded()",
            "if: success()"),
        new("Environment con approval",
            "environment: production",
            "environment: production"),
    ];

    private static readonly Dictionary<string, Equivalencia> PorConcepto =
        Todas.ToDictionary(e => e.Concepto, StringComparer.OrdinalIgnoreCase);

    public static Equivalencia? Buscar(string concepto)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(concepto);
        if (PorConcepto.TryGetValue(concepto, out var exacta)) return exacta;

        // Búsqueda parcial: cualquier equivalencia cuyo Concepto contenga el término.
        return Todas.FirstOrDefault(e =>
            e.Concepto.Contains(concepto, StringComparison.OrdinalIgnoreCase));
    }
}
