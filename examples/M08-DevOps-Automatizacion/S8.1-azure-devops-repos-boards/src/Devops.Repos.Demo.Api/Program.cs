using System.Text.Json.Serialization;
using Devops.Repos.Demo.Api.Endpoints;
using Devops.Repos.Demo.Api.Repos;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Único servicio inyectable: compone RepoStrategyAdvisor +
// BranchPolicyAdvisor + ConventionalCommitParser. Sin estado →
// singleton (lo cruza el test DI).
builder.Services.AddSingleton<IRepoBoardsPlanner, RepoBoardsPlanner>();

var app = builder.Build();

app.MapDevops();

app.Run();

// Para WebApplicationFactory<Program> en los tests.
public partial class Program { }
