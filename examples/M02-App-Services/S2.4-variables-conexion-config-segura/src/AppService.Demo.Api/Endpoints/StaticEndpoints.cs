namespace AppService.Demo.Api.Endpoints;

public static class StaticEndpoints
{
    public static IEndpointRouteBuilder MapStatic(this IEndpointRouteBuilder app)
    {
        // Slide 25 — Cache-Control en respuestas idempotentes.
        // Front Door / CDN cachean en edge usando estas cabeceras.
        // Slide 29 — Estos endpoints encajan como applicationInitialization
        // para precalentar conexiones de DB/cache antes de recibir tráfico.

        app.MapGet("/api/products", (HttpContext http, int limit = 10) =>
        {
            limit = Math.Clamp(limit, 1, 100);

            // 60 s — datos que cambian poco pero no son inmutables
            http.Response.Headers.CacheControl = "public, max-age=60";

            var products = Enumerable.Range(1, limit).Select(i => new
            {
                id = i,
                name = $"Product {i}",
                price = 9.99m + i
            });

            return Results.Ok(products);
        });

        app.MapGet("/api/categorias", (HttpContext http) =>
        {
            // 1 h — cambia muy raramente
            http.Response.Headers.CacheControl = "public, max-age=3600";

            var cats = new[] { "Electronics", "Books", "Toys", "Home", "Sports" };
            return Results.Ok(cats);
        });

        return app;
    }
}
