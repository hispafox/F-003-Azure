using Devops.Repos.Demo.Api.Repos;

namespace Devops.Repos.Demo.Api.Tests;

// CAPA 1 — branch policies (slides 5, 12, 20).
[Trait("Category", "Unit")]
public class Unit_BranchPolicyTests
{
    [Fact]
    public void Policies_Minimas_Tienen_RequiredReviewers_Y_Build()
    {
        var m = BranchPolicyAdvisor.Minimas;
        Assert.Contains(BranchPolicy.RequiredReviewers, m);
        Assert.Contains(BranchPolicy.BuildExitoso, m);
        Assert.Contains(BranchPolicy.NoPushDirecto, m);
    }

    [Fact]
    public void Policies_Recomendadas_Incluyen_LinkedWorkItems_Slide_12()
    {
        var r = BranchPolicyAdvisor.Recomendadas;
        Assert.Contains(BranchPolicy.LinkedWorkItems, r);
        Assert.Contains(BranchPolicy.LimitarMergeTypes, r);
    }

    [Fact]
    public void Evaluar_Sin_Minimas_Reporta_Faltantes()
    {
        var ev = BranchPolicyAdvisor.Evaluar(
            [BranchPolicy.RequiredReviewers]);
        Assert.False(ev.Cumple);
        Assert.Contains(BranchPolicy.BuildExitoso, ev.Faltantes);
        Assert.Contains(BranchPolicy.ResolucionDeComentarios, ev.Faltantes);
    }

    [Fact]
    public void Evaluar_Con_Todas_Las_Minimas_Cumple()
    {
        var ev = BranchPolicyAdvisor.Evaluar(BranchPolicyAdvisor.Minimas);
        Assert.True(ev.Cumple);
        Assert.Empty(ev.Faltantes);
    }
}
