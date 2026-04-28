namespace AppService.Demo.Api.Services;

// Slide 31 — Registrado como TYPED CLIENT vía AddHttpClient<ExternalApiClient>().
// El runtime reutiliza el HttpMessageHandler internamente para evitar agotar el
// pool de SNAT ports. Nunca hagas `new HttpClient()` por petición.
public sealed class ExternalApiClient(HttpClient httpClient)
{
    public Task<HttpResponseMessage> PingAsync(CancellationToken ct = default)
        => httpClient.GetAsync("/", ct);
}
