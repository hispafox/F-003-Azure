using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Messaging.Demo.Api.Messaging;

// Slides 3-5 — filtros SQL de suscripción de Service Bus. El broker
// evalúa la expresión contra las ApplicationProperties del mensaje y
// descarta los que no cumplen ANTES de entregarlos a la suscripción.
//
// Implementa el subconjunto real que cubren las slides: comparaciones
// (= <> != > >= < <= LIKE), IS [NOT] NULL, AND/OR/NOT y paréntesis,
// con lógica de 3 valores (propiedad ausente → UNKNOWN → no entrega),
// igual que Service Bus / SQL-92.
public static class SqlFilterEvaluator
{
    // ¿Entrega el broker este mensaje a la suscripción con este filtro?
    public static bool Coincide(
        string filtroSql, IReadOnlyDictionary<string, object?> propiedades)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filtroSql);
        ArgumentNullException.ThrowIfNull(propiedades);

        var tokens = Tokenizar(filtroSql);
        var parser = new Parser(tokens);
        var resultado = parser.ParseOr().Evaluar(propiedades);
        parser.EsperarFin();
        // UNKNOWN (null) → no se entrega (regla de Service Bus).
        return resultado == true;
    }

    // ---- Tokenizer -------------------------------------------------

    private enum Tipo { Ident, Numero, Texto, Operador, ParenIzq, ParenDer, Fin }

    private readonly record struct Token(Tipo Tipo, string Valor);

    private static List<Token> Tokenizar(string s)
    {
        var tokens = new List<Token>();
        int i = 0;
        while (i < s.Length)
        {
            char c = s[i];
            if (char.IsWhiteSpace(c)) { i++; continue; }

            if (c == '(') { tokens.Add(new(Tipo.ParenIzq, "(")); i++; continue; }
            if (c == ')') { tokens.Add(new(Tipo.ParenDer, ")")); i++; continue; }

            if (c == '\'')                       // literal de texto
            {
                var sb = new StringBuilder();
                i++;
                while (i < s.Length)
                {
                    if (s[i] == '\'')
                    {
                        if (i + 1 < s.Length && s[i + 1] == '\'') { sb.Append('\''); i += 2; continue; }
                        i++; break;
                    }
                    sb.Append(s[i++]);
                }
                tokens.Add(new(Tipo.Texto, sb.ToString()));
                continue;
            }

            if (char.IsDigit(c) || (c == '-' && i + 1 < s.Length && char.IsDigit(s[i + 1])))
            {
                int j = i + 1;
                while (j < s.Length && (char.IsDigit(s[j]) || s[j] == '.')) j++;
                tokens.Add(new(Tipo.Numero, s[i..j]));
                i = j;
                continue;
            }

            if (c is '=' or '<' or '>' or '!')
            {
                if (i + 1 < s.Length && s[i + 1] == '=')
                {
                    tokens.Add(new(Tipo.Operador, s[i..(i + 2)])); i += 2; continue;
                }
                if (c == '<' && i + 1 < s.Length && s[i + 1] == '>')
                {
                    tokens.Add(new(Tipo.Operador, "<>")); i += 2; continue;
                }
                tokens.Add(new(Tipo.Operador, c.ToString())); i++;
                continue;
            }

            if (char.IsLetter(c) || c == '_')
            {
                int j = i;
                while (j < s.Length && (char.IsLetterOrDigit(s[j]) || s[j] is '_' or '.')) j++;
                tokens.Add(new(Tipo.Ident, s[i..j]));
                i = j;
                continue;
            }

            throw new FormatException($"Carácter inesperado '{c}' en el filtro.");
        }
        tokens.Add(new(Tipo.Fin, ""));
        return tokens;
    }

    // ---- AST + parser (recursive descent) --------------------------

    private abstract class Nodo
    {
        // Lógica de 3 valores: true, false o null (UNKNOWN).
        public abstract bool? Evaluar(IReadOnlyDictionary<string, object?> p);
    }

    private sealed class Literal(object? v) : Nodo
    {
        public object? Valor { get; } = v;
        public override bool? Evaluar(IReadOnlyDictionary<string, object?> p) =>
            Valor as bool?;
    }

    private sealed class Prop(string n) : Nodo
    {
        public string Nombre { get; } = n;
        public override bool? Evaluar(IReadOnlyDictionary<string, object?> p) =>
            Resolver(p) as bool?;
        public object? Resolver(IReadOnlyDictionary<string, object?> p) =>
            p.TryGetValue(Nombre, out var v) ? v : null;
    }

    private sealed class Comparacion(string op, Nodo izq, Nodo der) : Nodo
    {
        public override bool? Evaluar(IReadOnlyDictionary<string, object?> p)
        {
            object? a = Valor(izq, p);
            object? b = Valor(der, p);

            if (op is "ISNULL") return a is null;
            if (op is "ISNOTNULL") return a is not null;
            if (a is null || b is null) return null;       // UNKNOWN

            if (op is "LIKE" or "NOTLIKE")
            {
                bool m = Like(Convert.ToString(a, CultureInfo.InvariantCulture)!,
                              Convert.ToString(b, CultureInfo.InvariantCulture)!);
                return op == "LIKE" ? m : !m;
            }

            int cmp = Comparar(a, b);
            return op switch
            {
                "=" => cmp == 0,
                "<>" or "!=" => cmp != 0,
                ">" => cmp > 0,
                ">=" => cmp >= 0,
                "<" => cmp < 0,
                "<=" => cmp <= 0,
                _ => throw new FormatException($"Operador no soportado: {op}"),
            };
        }

        private static object? Valor(Nodo n, IReadOnlyDictionary<string, object?> p) =>
            n switch
            {
                Literal l => l.Valor,
                Prop pr => pr.Resolver(p),
                _ => n.Evaluar(p),
            };

        private static int Comparar(object a, object b)
        {
            if (a is bool || b is bool)
                return Convert.ToBoolean(a).CompareTo(Convert.ToBoolean(b));
            if (EsNumero(a) && EsNumero(b))
                return Convert.ToDouble(a, CultureInfo.InvariantCulture)
                    .CompareTo(Convert.ToDouble(b, CultureInfo.InvariantCulture));
            return string.Compare(
                Convert.ToString(a, CultureInfo.InvariantCulture),
                Convert.ToString(b, CultureInfo.InvariantCulture),
                StringComparison.Ordinal);
        }

        private static bool EsNumero(object o) =>
            o is byte or sbyte or short or ushort or int or uint
                or long or ulong or float or double or decimal;

        private static bool Like(string valor, string patron)
        {
            var rx = new StringBuilder("^");
            foreach (char c in patron)
                rx.Append(c switch
                {
                    '%' => ".*",
                    '_' => ".",
                    _ => Regex.Escape(c.ToString()),
                });
            rx.Append('$');
            return Regex.IsMatch(valor, rx.ToString(),
                RegexOptions.Singleline, TimeSpan.FromSeconds(1));
        }
    }

    private sealed class Y(Nodo a, Nodo b) : Nodo
    {
        public override bool? Evaluar(IReadOnlyDictionary<string, object?> p)
        {
            bool? x = a.Evaluar(p), y = b.Evaluar(p);
            if (x == false || y == false) return false;
            if (x is null || y is null) return null;
            return true;
        }
    }

    private sealed class O(Nodo a, Nodo b) : Nodo
    {
        public override bool? Evaluar(IReadOnlyDictionary<string, object?> p)
        {
            bool? x = a.Evaluar(p), y = b.Evaluar(p);
            if (x == true || y == true) return true;
            if (x is null || y is null) return null;
            return false;
        }
    }

    private sealed class No(Nodo n) : Nodo
    {
        public override bool? Evaluar(IReadOnlyDictionary<string, object?> p)
        {
            bool? v = n.Evaluar(p);
            return v is null ? null : !v;
        }
    }

    private sealed class Parser(List<Token> tokens)
    {
        private int _i;
        private Token Actual => tokens[_i];

        private bool EsPalabra(string kw) =>
            Actual.Tipo == Tipo.Ident &&
            string.Equals(Actual.Valor, kw, StringComparison.OrdinalIgnoreCase);

        public void EsperarFin()
        {
            if (Actual.Tipo != Tipo.Fin)
                throw new FormatException($"Token inesperado tras el filtro: '{Actual.Valor}'.");
        }

        public Nodo ParseOr()
        {
            var n = ParseAnd();
            while (EsPalabra("OR")) { _i++; n = new O(n, ParseAnd()); }
            return n;
        }

        private Nodo ParseAnd()
        {
            var n = ParseNot();
            while (EsPalabra("AND")) { _i++; n = new Y(n, ParseNot()); }
            return n;
        }

        private Nodo ParseNot()
        {
            if (EsPalabra("NOT")) { _i++; return new No(ParseNot()); }
            return ParsePrimary();
        }

        private Nodo ParsePrimary()
        {
            if (Actual.Tipo == Tipo.ParenIzq)
            {
                _i++;
                var n = ParseOr();
                if (Actual.Tipo != Tipo.ParenDer)
                    throw new FormatException("Falta ')' en el filtro.");
                _i++;
                return n;
            }
            return ParseComparacion();
        }

        private Nodo ParseComparacion()
        {
            var izq = ParseOperando();

            if (EsPalabra("IS"))
            {
                _i++;
                bool negado = EsPalabra("NOT");
                if (negado) _i++;
                if (!EsPalabra("NULL"))
                    throw new FormatException("Se esperaba NULL tras IS [NOT].");
                _i++;
                return new Comparacion(negado ? "ISNOTNULL" : "ISNULL", izq, izq);
            }

            string op;
            if (EsPalabra("LIKE")) { op = "LIKE"; _i++; }
            else if (EsPalabra("NOT"))
            {
                _i++;
                if (!EsPalabra("LIKE"))
                    throw new FormatException("Se esperaba LIKE tras NOT.");
                op = "NOTLIKE"; _i++;
            }
            else if (Actual.Tipo == Tipo.Operador) { op = Actual.Valor; _i++; }
            else
                throw new FormatException(
                    $"Se esperaba un operador de comparación, se encontró '{Actual.Valor}'.");

            return new Comparacion(op, izq, ParseOperando());
        }

        private Nodo ParseOperando()
        {
            var t = Actual;
            switch (t.Tipo)
            {
                case Tipo.Numero:
                    _i++;
                    return new Literal(double.Parse(t.Valor, CultureInfo.InvariantCulture));
                case Tipo.Texto:
                    _i++;
                    return new Literal(t.Valor);
                case Tipo.Ident:
                    _i++;
                    if (string.Equals(t.Valor, "TRUE", StringComparison.OrdinalIgnoreCase))
                        return new Literal(true);
                    if (string.Equals(t.Valor, "FALSE", StringComparison.OrdinalIgnoreCase))
                        return new Literal(false);
                    if (string.Equals(t.Valor, "NULL", StringComparison.OrdinalIgnoreCase))
                        return new Literal(null);
                    return new Prop(t.Valor);
                default:
                    throw new FormatException(
                        $"Se esperaba propiedad o literal, se encontró '{t.Valor}'.");
            }
        }
    }
}
