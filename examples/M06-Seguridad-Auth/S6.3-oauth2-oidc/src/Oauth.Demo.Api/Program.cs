using Oauth.Demo.Api.Endpoints;
using Oauth.Demo.Api.Oauth;

var builder = WebApplication.CreateBuilder(args);

// Único servicio inyectable: compone advisor + PKCE + authorize URL en
// un plan de login. Sin estado → singleton (lo cruza el test DI).
builder.Services.AddSingleton<ILoginPlanner, LoginPlanner>();

var app = builder.Build();

app.MapOauth();

app.Run();

// Para WebApplicationFactory<Program> en los tests.
public partial class Program { }
