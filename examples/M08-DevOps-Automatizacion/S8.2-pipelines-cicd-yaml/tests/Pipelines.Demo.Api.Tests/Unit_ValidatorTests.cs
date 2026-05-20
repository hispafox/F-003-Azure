using Pipelines.Demo.Api.Pipelines;

namespace Pipelines.Demo.Api.Tests;

// CAPA 1 — validador estructural (slides 5, 6, 7, 8, 13).
[Trait("Category", "Unit")]
public class Unit_ValidatorTests
{
    private const string PipelineOK = """
        trigger: { branches: { include: [main] } }
        pool: { vmImage: 'ubuntu-latest' }
        stages:
        - stage: Build
          jobs:
          - job: B
            steps:
            - script: dotnet build
            - script: dotnet test
        - stage: Deploy
          dependsOn: Build
          jobs:
          - deployment: D
            environment: 'ventas-production'
            strategy:
              runOnce:
                deploy:
                  steps:
                  - script: echo deploy
        """;

    [Fact]
    public void Pipeline_Correcto_Es_Valido()
    {
        var p = PipelineYamlParser.Parsear(PipelineOK);
        var v = PipelineStructureValidator.Validar(p);
        Assert.True(v.Valido);
        Assert.Empty(v.Errores);
    }

    [Fact]
    public void Sin_Stages_Es_Invalido()
    {
        var v = PipelineStructureValidator.Validar(new PipelineDef(
            null, null, [], null, [], []));
        Assert.False(v.Valido);
        Assert.Contains(v.Errores, x => x.Contains("stages"));
    }

    [Fact]
    public void DependsOn_A_Stage_Inexistente_Es_Error()
    {
        const string yaml = """
            stages:
            - stage: A
              dependsOn: NoExiste
              jobs:
              - job: J
                steps: [{ script: echo }]
            """;
        var v = PipelineStructureValidator.Validar(PipelineYamlParser.Parsear(yaml));
        Assert.False(v.Valido);
        Assert.Contains(v.Errores, x => x.Contains("NoExiste"));
    }

    [Fact]
    public void Job_Sin_Steps_Es_Error()
    {
        var p = new PipelineDef(null, null, [], null, [],
            [new StageDef("S", [], null,
                [new JobDef("J", false, null, [])])]);
        var v = PipelineStructureValidator.Validar(p);
        Assert.False(v.Valido);
        Assert.Contains(v.Errores, x => x.Contains("steps"));
    }

    [Fact]
    public void Deployment_Sin_Environment_Es_Error()
    {
        var p = new PipelineDef(null, null, [], null, [],
            [new StageDef("Deploy", [], null,
                [new JobDef("D", true, null,
                    [new StepDef("script", null, "echo deploy")])])]);
        var v = PipelineStructureValidator.Validar(p);
        Assert.False(v.Valido);
        Assert.Contains(v.Errores, x => x.Contains("environment"));
    }

    [Fact]
    public void Job_Normal_Con_Environment_De_Prod_Es_Aviso_Slide_8()
    {
        const string yaml = """
            stages:
            - stage: Build
              jobs:
              - job: B
                environment: production
                steps:
                - script: dotnet test
            """;
        var v = PipelineStructureValidator.Validar(PipelineYamlParser.Parsear(yaml));
        // El error real puede o no estar; lo que sí debe haber es aviso.
        Assert.Contains(v.Avisos, x => x.Contains("producción"));
    }

    [Fact]
    public void Falta_Step_De_Test_Es_Aviso_Slide_6()
    {
        const string yaml = """
            stages:
            - stage: Build
              jobs:
              - job: B
                steps:
                - script: dotnet build
            """;
        var v = PipelineStructureValidator.Validar(PipelineYamlParser.Parsear(yaml));
        Assert.Contains(v.Avisos, x => x.Contains("tests"));
    }
}
