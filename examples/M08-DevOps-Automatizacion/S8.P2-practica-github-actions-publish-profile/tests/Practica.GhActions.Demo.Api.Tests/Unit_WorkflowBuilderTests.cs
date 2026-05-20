using Practica.GhActions.Demo.Api.GhActions;

namespace Practica.GhActions.Demo.Api.Tests;

// CAPA 1 — esqueleto del workflow GHA (slides 9, 14, 15, 18).
[Trait("Category", "Unit")]
public class Unit_WorkflowBuilderTests
{
    [Fact]
    public void Workflow_Minimal_Tiene_Un_Solo_Job_Build_And_Deploy()
    {
        var w = WorkflowBuilder.Construir(new OpcionesWorkflow(AppName: "x"));
        Assert.Single(w.Jobs);
        Assert.Equal("build-and-deploy", w.Jobs[0].Nombre);
    }

    [Fact]
    public void Triggers_Por_Defecto_Son_Push_Main_Y_Workflow_Dispatch()
    {
        var w = WorkflowBuilder.Construir(new OpcionesWorkflow(AppName: "x"));
        Assert.Contains(w.Triggers, t => t.Contains("push.branches", StringComparison.Ordinal));
        Assert.Contains(w.Triggers, t => t.Contains("workflow_dispatch", StringComparison.Ordinal));
    }

    [Fact]
    public void Solo_En_Tags_Sustituye_Trigger_De_Branches()
    {
        var w = WorkflowBuilder.Construir(new OpcionesWorkflow(
            AppName: "x", SoloEnTags: true));
        Assert.Contains(w.Triggers, t => t.Contains("push.tags", StringComparison.Ordinal));
        Assert.DoesNotContain(w.Triggers, t => t.Contains("push.branches", StringComparison.Ordinal));
    }

    [Fact]
    public void Incluir_Tests_Crea_Dos_Jobs_Con_Dependencia()
    {
        var w = WorkflowBuilder.Construir(new OpcionesWorkflow(
            AppName: "x", IncluirTests: true));
        Assert.Equal(2, w.Jobs.Count);

        var buildTest = w.Jobs.Single(j => j.Nombre == "build-test");
        Assert.Null(buildTest.Necesita);
        Assert.Contains(buildTest.Steps, s => s.Contains("dotnet test", StringComparison.Ordinal));

        var deploy = w.Jobs.Single(j => j.Nombre == "deploy");
        Assert.Equal("build-test", deploy.Necesita);
    }

    [Fact]
    public void Deploy_Step_Referencia_Secret_Y_App_Name()
    {
        var w = WorkflowBuilder.Construir(new OpcionesWorkflow(
            AppName: "webapp-pedro"));
        var pasos = w.Jobs[0].Steps;
        Assert.Contains(pasos, s =>
            s.Contains("azure/webapps-deploy@v3", StringComparison.Ordinal)
            && s.Contains("webapp-pedro", StringComparison.Ordinal)
            && s.Contains("AZURE_WEBAPP_PUBLISH_PROFILE", StringComparison.Ordinal));
    }

    [Fact]
    public void Smoke_Al_Final_Anade_Paso()
    {
        var w = WorkflowBuilder.Construir(new OpcionesWorkflow(
            AppName: "x", SmokeAlFinal: true));
        Assert.Contains(w.Jobs[0].Steps,
            s => s.Contains("Smoke test", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Environment_Production_Activado_Lo_Refleja()
    {
        var w = WorkflowBuilder.Construir(new OpcionesWorkflow(
            AppName: "x", EnvironmentProduccion: true));
        Assert.Equal("production", w.Environment);
    }

    [Fact]
    public void Setup_Dotnet_Usa_La_Version_Solicitada()
    {
        var w = WorkflowBuilder.Construir(new OpcionesWorkflow(
            AppName: "x", DotnetVersion: "10.0.x"));
        Assert.Contains(w.Jobs[0].Steps,
            s => s.Contains("setup-dotnet", StringComparison.Ordinal)
                 && s.Contains("10.0.x", StringComparison.Ordinal));
    }
}
