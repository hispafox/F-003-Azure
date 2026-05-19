using System.Text;

namespace Entra.Demo.Api.Tests;

// Helper de test: fabrica un JWT sin firma (alg=none) a partir del JSON
// del payload. Solo para alimentar a JwtInspector (que decodifica, no
// valida firma — slide 18).
internal static class Jwt
{
    private static string B64Url(string s) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(s))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');

    public static string Crear(string payloadJson) =>
        $"{B64Url("{\"alg\":\"none\",\"typ\":\"JWT\"}")}.{B64Url(payloadJson)}.";
}
