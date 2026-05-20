using Pipelines.Demo.Api.Pipelines;

namespace Pipelines.Demo.Api.Tests;

// CAPA 1 — parser YAML (slides 3-5, 7, 8).
[Trait("Category", "Unit")]
public class Unit_ParserTests
{
    private const string YamlMinimo = """
        trigger:
          branches:
            include: [main]
        pool:
          vmImage: 'ubuntu-latest'
        stages:
        - stage: Build
          jobs:
          - job: BuildAndTest
            steps:
            - script: dotnet build
            - script: dotnet test
        """;

    [Fact]
    public void Trigger_Branches_Y_VmImage_Se_Extraen()
    {
        var p = PipelineYamlParser.Parsear(YamlMinimo);
        Assert.NotNull(p.Trigger);
        Assert.True(p.Trigger!.Habilitado);
        Assert.Contains("main", p.Trigger.Branches);
        Assert.Equal("ubuntu-latest", p.PoolVmImage);
    }

    [Fact]
    public void Stages_Jobs_Y_Steps_Cuentan()
    {
        var p = PipelineYamlParser.Parsear(YamlMinimo);
        Assert.Single(p.Stages);
        Assert.Equal("Build", p.Stages[0].Name);
        Assert.Single(p.Stages[0].Jobs);
        Assert.Equal(2, p.Stages[0].Jobs[0].Steps.Count);
    }

    [Fact]
    public void Trigger_None_Marca_Deshabilitado()
    {
        var p = PipelineYamlParser.Parsear("""
            trigger: none
            stages:
            - stage: A
              jobs:
              - job: J
                steps:
                - script: echo hi
            """);
        Assert.NotNull(p.Trigger);
        Assert.False(p.Trigger!.Habilitado);
    }

    [Fact]
    public void Deployment_Job_Detectado_Con_Environment()
    {
        const string yaml = """
            stages:
            - stage: Deploy
              jobs:
              - deployment: DeployProd
                environment: 'ventas-production'
                strategy:
                  runOnce:
                    deploy:
                      steps:
                      - script: echo deploy
            """;
        var p = PipelineYamlParser.Parsear(yaml);
        var job = p.Stages[0].Jobs[0];
        Assert.True(job.IsDeployment);
        Assert.Equal("ventas-production", job.Environment);
        Assert.Single(job.Steps);
    }

    [Fact]
    public void DependsOn_Lista_Se_Extrae()
    {
        const string yaml = """
            stages:
            - stage: Build
              jobs:
              - job: J
                steps: [{ script: echo }]
            - stage: Deploy
              dependsOn: [Build]
              jobs:
              - job: J
                steps: [{ script: echo }]
            """;
        var p = PipelineYamlParser.Parsear(yaml);
        Assert.Contains("Build", p.Stages[1].DependsOn);
    }

    [Fact]
    public void Variables_Group_Se_Extrae()
    {
        const string yaml = """
            variables:
            - group: 'ventas-shared'
            - group: 'kv-ventas-secrets'
            stages:
            - stage: A
              jobs:
              - job: J
                steps: [{ script: echo }]
            """;
        var p = PipelineYamlParser.Parsear(yaml);
        Assert.Equal(2, p.VariableGroups.Count);
        Assert.Contains("ventas-shared", p.VariableGroups);
    }

    [Fact]
    public void Schedules_Se_Extraen()
    {
        const string yaml = """
            schedules:
            - cron: '0 2 * * *'
              displayName: Nightly
              branches:
                include: [main]
            stages:
            - stage: A
              jobs:
              - job: J
                steps: [{ script: echo }]
            """;
        var p = PipelineYamlParser.Parsear(yaml);
        Assert.Single(p.Schedules);
        Assert.Equal("0 2 * * *", p.Schedules[0].Cron);
    }

    [Fact]
    public void Yaml_Invalido_Lanza()
        => Assert.Throws<FormatException>(() =>
            // Lista no cerrada — error de sintaxis de flow-style YAML.
            PipelineYamlParser.Parsear("trigger: [main"));

    [Fact]
    public void Yaml_Vacio_Lanza()
        => Assert.Throws<ArgumentException>(() =>
            PipelineYamlParser.Parsear("   "));
}
