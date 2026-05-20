using System.Diagnostics;
using System.Text.Json;

namespace Iac.Bicep.Demo.Api.Tests;

// CAPA "Integration" — invoca `bicep build` REAL. SkippableFact
// (lección 2 del HANDOFF): si `bicep` no está en PATH, se omite y la
// suite queda verde. Reproducible localmente con `az bicep install`.
[Trait("Category", "Integration")]
public class Integration_BicepBuildTests
{
    [SkippableFact]
    public void Bicep_Build_Produce_Arm_Json_Con_El_Recurso_Esperado()
    {
        Skip.If(!HayBicepEnPath(),
            "bicep no está en PATH; instala con `az bicep install` y vuelve a correr.");

        // Bicep mínimo: un App Service Plan sin parámetros (slide 4).
        const string bicep = """
            targetScope = 'resourceGroup'

            param planName string = 'plan-demo'
            param location string = resourceGroup().location

            resource plan 'Microsoft.Web/serverfarms@2023-12-01' = {
              name: planName
              location: location
              sku: {
                name: 'F1'
                tier: 'Free'
                capacity: 1
              }
            }
            """;

        string dir = Path.Combine(Path.GetTempPath(),
            $"iac-bicep-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        string bicepPath = Path.Combine(dir, "main.bicep");
        string armPath = Path.Combine(dir, "main.json");

        try
        {
            File.WriteAllText(bicepPath, bicep);

            var psi = new ProcessStartInfo("bicep",
                $"build \"{bicepPath}\" --outfile \"{armPath}\"")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            using var p = Process.Start(psi)
                ?? throw new InvalidOperationException("No se pudo lanzar bicep.");
            bool exited = p.WaitForExit(30_000);
            Skip.IfNot(exited, "bicep build superó el timeout de 30s.");
            Assert.Equal(0, p.ExitCode);
            Assert.True(File.Exists(armPath),
                "bicep build no produjo el archivo ARM JSON esperado.");

            using var doc = JsonDocument.Parse(File.ReadAllText(armPath));
            var root = doc.RootElement;

            // ARM mínimo: schema + resources con un Microsoft.Web/serverfarms.
            Assert.True(root.TryGetProperty("$schema", out _));
            var resources = root.GetProperty("resources");
            Assert.True(resources.GetArrayLength() >= 1);
            Assert.Contains(
                resources.EnumerateArray()
                    .Select(r => r.GetProperty("type").GetString()),
                t => t == "Microsoft.Web/serverfarms");
        }
        finally
        {
            try { Directory.Delete(dir, recursive: true); } catch { /* best-effort */ }
        }
    }

    private static bool HayBicepEnPath()
    {
        // `bicep --version` debe existir y devolver 0 en < 5s.
        try
        {
            var psi = new ProcessStartInfo("bicep", "--version")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            using var p = Process.Start(psi);
            if (p is null) return false;
            return p.WaitForExit(5_000) && p.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
