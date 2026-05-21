using System.Text.Json.Serialization;
using Bonus.IntroIaAgentica.Demo.Api.Endpoints;
using Bonus.IntroIaAgentica.Demo.Api.Intro;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddSingleton<IIntroIaAgenticaPlanner, IntroIaAgenticaPlanner>();

var app = builder.Build();

app.MapIntro();

app.Run();

// Para WebApplicationFactory<Program> en los tests.
public partial class Program { }
