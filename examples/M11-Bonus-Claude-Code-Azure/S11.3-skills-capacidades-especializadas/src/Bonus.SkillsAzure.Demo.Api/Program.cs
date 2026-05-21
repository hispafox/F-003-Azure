using System.Text.Json.Serialization;
using Bonus.SkillsAzure.Demo.Api.Endpoints;
using Bonus.SkillsAzure.Demo.Api.Skills;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddSingleton<ISkillLibraryPlanner, SkillLibraryPlanner>();

var app = builder.Build();

app.MapSkills();

app.Run();

// Para WebApplicationFactory<Program> en los tests.
public partial class Program { }
