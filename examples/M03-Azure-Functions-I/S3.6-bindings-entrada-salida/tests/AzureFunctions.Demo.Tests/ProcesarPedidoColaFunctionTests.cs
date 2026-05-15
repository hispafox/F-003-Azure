using System.Text.Json;

namespace AzureFunctions.Demo.Tests;

public class ProcesarPedidoColaFunctionTests
{
    [Fact]
    public void Procesar_Mensaje_Valido_Devuelve_Deserializado()
    {
        var fn = TestHost.NewProcesarCola();
        var mensaje = JsonSerializer.Serialize(new
        {
            pedidoId = "ped-001",
            clienteId = "cliente-A",
            total = 150m,
            encolado = DateTimeOffset.UtcNow,
        });

        var resultado = fn.Procesar(mensaje);

        Assert.NotNull(resultado);
        Assert.Equal("ped-001", resultado!.PedidoId);
        Assert.Equal("cliente-A", resultado.ClienteId);
        Assert.Equal(150m, resultado.Total);
    }

    [Fact]
    public void Procesar_Mensaje_Vacio_Devuelve_Null_Sin_Excepcion()
    {
        var fn = TestHost.NewProcesarCola();

        Assert.Null(fn.Procesar(""));
        Assert.Null(fn.Procesar("   "));
    }

    [Fact]
    public void Procesar_Json_Malformado_Devuelve_Null_Sin_Crash()
    {
        // Slide 21 anti-pattern — leer la cola como string crudo nos
        // permite capturar JsonException y loguear el payload exacto.
        // Si bindáramos directo a un POCO, Functions reintentaría 5 veces
        // y mandaría a poison queue sin que veamos qué falló.
        var fn = TestHost.NewProcesarCola();

        var resultado = fn.Procesar("{ totally not json");

        Assert.Null(resultado);
    }

    [Fact]
    public void Procesar_Mensaje_Sin_PedidoId_Devuelve_Null()
    {
        // Defensive: aunque el JSON deserialize, si falta el campo clave
        // descartamos el mensaje. En producción iría a un dead-letter.
        var fn = TestHost.NewProcesarCola();
        var mensaje = JsonSerializer.Serialize(new { clienteId = "cliente-A", total = 10m });

        var resultado = fn.Procesar(mensaje);

        Assert.Null(resultado);
    }

    [Fact]
    public void Procesar_Es_CaseInsensitive_Sobre_Property_Names()
    {
        // El binding de QueueTrigger normalmente acepta camelCase desde
        // el productor. Configuramos PropertyNameCaseInsensitive=true
        // para tolerar también PascalCase si otro productor publicara así.
        var fn = TestHost.NewProcesarCola();
        var mensaje = """
            {
              "PedidoId": "ped-002",
              "ClienteId": "cliente-B",
              "Total": 50.0,
              "Encolado": "2026-05-15T10:00:00Z"
            }
            """;

        var resultado = fn.Procesar(mensaje);

        Assert.NotNull(resultado);
        Assert.Equal("ped-002", resultado!.PedidoId);
    }
}
