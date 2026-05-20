# Manual del alumno — S5.2 · Azure SQL Database

Esto **no** es el [`README.md`](README.md). El README es la ficha técnica del ejemplo: estructura, mapeo a slides, comandos de test, despliegue por Portal. Útil cuando vas a tocar código. Este manual va antes: te cuenta para qué existe el ejemplo, qué decisión silenciosa quiere enseñarte y cómo leerlo. Cuando termines, abre el README y todo encajará más rápido.

Tiempo de lectura: ~30 min. Submódulo de teoría: [M05-S5.2](../../../doc/M05-Almacenamiento-BBDD/v3-actual/M05-S5.2-azure-sql-database-v3.md) (~35 slides). Las primeras cuatro secciones son el marco mental; de la sección 5 a la sección 8 entras al detalle técnico; el resto es práctica, autoevaluación y un par de avisos antes de pasar a S5.3.

*Creado: 2026-05-19 23:50 +0200*

---

## 1. La idea en una frase

S5.1 te enseñó qué **no** meter en una base de datos. S5.2 entrena la otra mitad: cuando los datos **sí** piden base de datos relacional, qué decides y cómo configurarla para que no se caiga el primer día en Azure.

Y aquí está el matiz que casi nadie cuenta: la decisión importante en este submódulo no es "¿qué ORM uso?". Sabes SQL, EF Core es estándar y se aprende rápido. La decisión importante es **cómo conectas EF Core a Azure SQL para que aguante producción**. Pool de conexiones, retry de errores transitorios, migraciones que jamás aplicas al arrancar la app, Managed Identity sin contraseñas. Esa es la mitad del submódulo que se aprende a base de tropezar en producción si nadie te avisa antes.

Aquí te avisamos antes.

---

## 2. El problema real que hay detrás

Un cliente me llamó hace unos meses con un caso curioso. Su API funcionaba perfectamente en local. En staging también. La habían desplegado a Azure App Service contra una Azure SQL Database serverless y... cada hora, más o menos, el primer usuario que entraba se llevaba un error 500. Solo el primero. El segundo ya iba bien.

El log decía: `Database '<X>' on server '<Y>' is not currently available. Please retry`. Código de error **40613**.

¿Qué pasaba? La base de datos era Serverless con auto-pausa de 60 minutos. Una hora sin actividad → se pausaba (de ahí el "≈ 0 € parado"). La primera query que llegaba después la despertaba, y mientras despertaba — entre 10 y 30 segundos — devolvía 40613. Es un error **transitorio** documentado de Azure SQL: significa "ahora no, vuelve a intentarlo en un momento".

La app no reintentaba. Por eso ese primer usuario se llevaba el palo. Cuando le activamos `EnableRetryOnFailure` con los códigos de error transitorios — exactamente lo que hace este ejemplo en [`AzureSqlRetryPolicy.cs`](src/Sql.Demo.Api/Sql/AzureSqlRetryPolicy.cs) y [`Program.cs`](src/Sql.Demo.Api/Program.cs) — el problema desapareció. Cinco líneas de configuración. Ningún cambio de código de negocio.

Ahora ponte a pensar en lo que necesita la app de ventas que arrastramos desde S5.1, pero centrándote en lo que **no** podía hacer Storage:

| Necesidad real | ¿Sirve algo de S5.1? | Por qué SQL | Dónde lo verás |
| --- | --- | --- | --- |
| Catálogo de productos con búsqueda por nombre | No (Blob no es consultable, Table no tiene índices secundarios) | índice sobre `Nombre` + `OrderBy` | [`VentasDbContext.cs`](src/Sql.Demo.Api/Data/VentasDbContext.cs) |
| Crear pedido **descontando stock** atómicamente | No (Queue no garantiza nada; Table no es transaccional) | un único `SaveChangesAsync` = transacción ACID | [`IPedidoRepository.cs`](src/Sql.Demo.Api/Repositories/IPedidoRepository.cs) |
| Listar pedidos con el **nombre del producto** asociado | No (Table no hace JOINs; Cosmos: JOINs limitados cross-partition) | `Include` + proyección a DTO en una query | `PedidoRepository.ListarAsync` |
| Regla "no borres un producto que tiene pedidos" | No (sin FKs no se puede expresar) | `OnDelete(DeleteBehavior.Restrict)` | `VentasDbContext.OnModelCreating` |

Cada fila pide algo que solo un motor relacional con transacciones ACID te da fácil. Esa es la razón — no "que sea más conocido" — de elegir Azure SQL aquí.

---

## 3. Por qué esto importa en tu stack

EF Core sobre SQL Server es el patrón de acceso a datos más común en .NET. No se enseña porque sea nuevo, sino porque **el ORM oculta trampas que en Azure se cobran**: una `IQueryable` mal escrita carga toda la tabla, una migración mal aplicada bloquea un despliegue, una conexión sin pool agota el tier, un `DateTimeOffset` mal elegido te revienta los tests con SQLite.

Cambio respecto a S5.1: ahora hay **estado persistente con esquema**. El stack de la app sigue siendo Minimal API + `WebApplicationFactory`, pero aparecen capas nuevas que no existían en S5.1: **DbContext**, **migraciones**, **provider de base de datos** y una capa de tests Component con SQLite in-memory que cubre lo que SQLite *sí* puede ejercitar (el modelo, las queries, los repos) sin necesitar Docker.

---

## 4. El modelo mental: el archivador con cajones, etiquetas y reglas

Piensa en uno de esos archivadores grandes de oficina antigua. Cuatro o cinco cajones metálicos, cada uno con etiqueta — *Clientes*, *Productos*, *Pedidos*. Dentro de cada cajón, carpetas con orden alfabético; dentro de cada carpeta, fichas con campos fijos. Hay una llave que abre el mueble entero y reglas escritas en una pegatina amarilla: *"No tires una carpeta de Clientes si tiene pedidos asociados"*. *"Cada ficha de pedido tiene que tener un cliente válido"*. *"El campo precio se escribe siempre con dos decimales, hasta tres ceros incluso si no hace falta"*.

Eso es una base de datos relacional. Cajones = tablas. Carpetas y fichas = filas. Etiquetas y pestañas = índices. Llaves = claves primarias y foráneas. Reglas = el esquema (tipos, longitudes, FKs, `decimal(18,2)`). Y el conserje que se asegura de que las reglas se cumplan es **el motor**: pase lo que pase, no te deja meter una ficha rota.

Azure SQL Database es ese archivador, pero en versión PaaS. El motor es el mismo SQL Server de toda la vida (Slide 2). Lo que cambia es que Azure se encarga del OS, los parches, los backups, la alta disponibilidad y la replicación. Tú te encargas del esquema, las queries, los índices y el firewall.

Tres frases para quedarte con la imagen:

- **Es SQL Server.** No es "un SQL parecido": es el mismo motor, el mismo Transact-SQL.
- **El "servidor lógico" no es una VM.** Es un endpoint de gestión que agrupa una o varias bases de datos. Pagas por las DBs, no por el servidor.
- **Hay tres parientes** (Slide 19): Azure SQL **Database** (PaaS, donde estás, ~95% SQL Server), **Managed Instance** (PaaS de instancia completa, ~99%, ideal para migrar legacy) y **SQL en VM** (IaaS, 100% pero tú lo gestionas). Para apps nuevas, casi siempre Database.

```
Servidor lógico (sql-ventas-prod)   ← endpoint de gestión, NO una VM
   ├── Base de datos: db-ventas
   │     ├── Tablas: Productos, Pedidos        (esquema fijo, migrado)
   │     ├── Índices: IX_Producto_Nombre, IX_Pedido_Fecha
   │     ├── FK: Pedido.ProductoId → Producto.Id
   │     └── Backups automáticos: 7-35 días (point-in-time restore)
   └── Firewall · TDE · Auditing · Threat Detection · Auto-tuning
```

Vuelve a esta imagen en los próximos minutos. Cuando se hable de migraciones, son las reglas escritas en la pegatina amarilla — y se cambian con cuidado. Cuando se hable de transacción ACID, es que el conserje no te deja añadir una ficha de pedido si la de producto no existe. Cuando se hable de retry, es que el archivador se cierra de vez en cuando para mantenimiento y conviene esperar treinta segundos antes de renunciar.

---

## 5. El modelo de datos con EF Core, en detalle

El corazón del ejemplo. Cada decisión del `DbContext` y de los repositorios responde a una slide de la teoría — y a un anti-patrón fácil de evitar si sabes mirar.

### 5.1 El DbContext: el mapa C# ↔ tablas

[`VentasDbContext.cs`](src/Sql.Demo.Api/Data/VentasDbContext.cs) configura el esquema explícitamente en `OnModelCreating`, sin atributos sobre las entidades (Slide 7):

```csharp
modelBuilder.Entity<Producto>(e =>
{
    e.HasKey(p => p.Id);
    e.Property(p => p.Nombre).HasMaxLength(200).IsRequired();
    e.Property(p => p.Precio).HasColumnType("decimal(18,2)");
    e.HasIndex(p => p.Nombre);                  // búsqueda por nombre (slide 9)
});

modelBuilder.Entity<Pedido>(e =>
{
    e.HasKey(p => p.Id);
    e.Property(p => p.Total).HasColumnType("decimal(18,2)");
    e.HasIndex(p => p.Fecha);
    e.HasOne(p => p.Producto)
        .WithMany(pr => pr.Pedidos)
        .HasForeignKey(p => p.ProductoId)
        .OnDelete(DeleteBehavior.Restrict);     // no borrar producto con pedidos
});
```

Lo más fácil de pasar por alto es `decimal(18,2)`. El tipo por defecto de EF para `decimal` no precisa escala, y SQL Server termina guardando 89.9 cuando subiste 89.90. Lo pillas en producción, con la primera reclamación de un cliente. La regla: para precios, **siempre** fija el tipo de columna.

Los índices van sobre los campos por los que vas a ordenar o filtrar (`Nombre`, `Fecha`). Sin índice, un `OrderBy(p => p.Nombre)` es un table scan: ridículo con 50 productos, ruinoso con 50.000. Y `OnDelete(DeleteBehavior.Restrict)` es la regla de la pegatina amarilla del archivador: no se puede tirar un producto que tiene pedidos. SQL la hace cumplir incluso si alguien hace el `DELETE` desde el portal — el motor es el conserje.

> 🎓 **Por qué el DbContext NO llama a `UseSqlServer` ni `UseSqlite`.** La elección del *provider* la hace `Program.cs` (SQL Server) o el test (SQLite). El mismo `DbContext` se ejercita en los tres escenarios: producción contra SQL Server, CAPA 2 contra SQLite, CAPA 3 contra SQL Server en Docker — sin tocar el modelo. Esa es la "magia" del provider pattern de EF Core, y la razón de que la CAPA 2 sea rápida.

### 5.2 Las entidades: dónde se esconden las trampas

[`Modelos.cs`](src/Sql.Demo.Api/Domain/Modelos.cs) define `Producto`, `Pedido`, los DTOs y un `enum CrearPedidoResultado`. Una decisión sutil que parece trivial y no lo es:

```csharp
public DateTime Fecha { get; set; }   // NO DateTimeOffset
```

> ⚠️ **Trampa de EF Core documentada.** `Pedido.Fecha` es `DateTime` (UTC), no `DateTimeOffset`. **SQLite no soporta `ORDER BY` sobre `DateTimeOffset`** (`NotSupportedException`). Como la query de `ListarAsync` ordena por fecha, usar `DateTimeOffset` rompería la CAPA 2 de tests (SQLite in-memory). `DateTime` UTC funciona en SQL Server (`datetime2`) **y** en SQLite. Es un ejemplo perfecto de decisión técnica que se entiende solo cuando alguien la pisa.

Otro detalle: `CrearPedidoResultado` es un `enum` para representar el "no hay stock" como un resultado de negocio normal, no como una excepción. El repo devuelve `(resultado, pedidoDto?)` y el endpoint mapea a `201` / `404` / `409`. Sin excepciones para el flujo esperado: las excepciones se reservan para lo inesperado.

### 5.3 CRUD y queries: el SDK por dentro

[`IProductoRepository.cs`](src/Sql.Demo.Api/Repositories/IProductoRepository.cs) es el patrón limpio:

```csharp
public async Task<IReadOnlyList<Producto>> ListarAsync()
    => await db.Productos.AsNoTracking().OrderBy(p => p.Nombre).ToListAsync();
```

`AsNoTracking` es el ajuste que más rendimiento gratuito te regala. El tracker de EF guarda una copia de cada entidad para detectar cambios; útil cuando vas a `Update`, pesado y lento si solo lees. La regla práctica: tracking solo cuando vas a modificar. Para listados, `AsNoTracking` siempre.

Y luego está la joya del repositorio, donde está la lógica de negocio real — [`IPedidoRepository.cs`](src/Sql.Demo.Api/Repositories/IPedidoRepository.cs):

```csharp
public async Task<(CrearPedidoResultado, PedidoDto?)> CrearAsync(CrearPedidoDto dto)
{
    var producto = await db.Productos.FindAsync(dto.ProductoId);
    if (producto is null) return (CrearPedidoResultado.ProductoNoExiste, null);
    if (dto.Cantidad <= 0 || producto.Stock < dto.Cantidad)
        return (CrearPedidoResultado.StockInsuficiente, null);

    var pedido = new Pedido { /* … */ Total = producto.Precio * dto.Cantidad };
    producto.Stock -= dto.Cantidad;
    db.Pedidos.Add(pedido);
    await db.SaveChangesAsync();   // ← UN solo SaveChanges = UNA transacción ACID
    /* … */
}
```

> 🧠 **La idea que justifica usar SQL aquí.** El `Add(pedido)` y el `producto.Stock -= dto.Cantidad` viajan en el **mismo** `SaveChangesAsync`. Si algo falla a mitad — timeout, conflicto, reinicio del servidor — la transacción se aborta entera: ni hay pedido fantasma, ni stock descontado sin pedido. Eso es ACID, y eso es exactamente lo que ninguna tecnología de S5.1 te daba fácil. Si moviera esto a Queue+Table, tendría que escribir compensaciones a mano y nunca quedaría perfecto.

La query de listado evita el clásico N+1:

```csharp
public async Task<IReadOnlyList<PedidoDto>> ListarAsync()
    => await db.Pedidos
        .AsNoTracking()
        .Include(p => p.Producto)               // un JOIN en SQL, no N queries
        .OrderByDescending(p => p.Fecha)
        .Select(p => new PedidoDto(/* … proyección … */))
        .ToListAsync();
```

> 🧠 **Anti-patrón N+1** (Slide 31). Si listaras pedidos sin `Include` y luego entraras a `pedido.Producto.Nombre` en un bucle, EF haría **una query extra por pedido**. Con mil pedidos, mil y una queries contra Azure. Con `Include`, un solo `LEFT JOIN`. Aprende a olerlo: si el log de EF te muestra varias SELECTs idénticas con id distinto, tienes un N+1.

### 5.4 Migraciones: el esquema versionado

```bash
dotnet ef migrations add InitialCreate         # genera C# + Designer
dotnet ef database update                      # aplica a la cs configurada
dotnet ef migrations script -o init.sql        # genera SQL para revisión
```

[`Migrations/`](src/Sql.Demo.Api/Migrations) ya trae `InitialCreate`. Las migraciones son **código versionado** que describe cambios al esquema — no las escribes a mano, las genera EF comparando tu modelo con la última migración aplicada.

> 🎓 **Por qué `Program.cs` no migra al arrancar.** La tentación clásica: meter `db.Database.Migrate()` en `Program.cs` "para que la app se autogestione". Anti-patrón 8 (Slide 35). Las razones, todas vividas en producción: **race conditions** en deploy con varias réplicas (dos instancias intentan migrar a la vez, una se cuelga, la otra falla); **no es atómico con el deploy** (si la migración rompe y la app arranca, sirves 500 hasta el rollback); **no es revisable** (en producción quieres ver el SQL antes de aplicarlo). El test de integración (CAPA 3) sí migra, pero dentro de su scope, que es código de test controlado. En producción la migración la aplica el pipeline o un humano con aprobación.

---

## 6. Coste y tier: cuál eliges y por qué

| Modelo | Cuándo | Cuánto | Cómo lo modela el ejemplo |
| --- | --- | --- | --- |
| **DTU — Basic** | dev/test diminuto (2 GB) | ~4 €/mes | `SqlTier.Basic` |
| **DTU — S0** | curso, dev estándar | ~13 €/mes | `SqlTier.S0` (default) |
| **vCore — General Purpose** | producción con control fino, > 60 conexiones | ~160 €/mes (2 vCore) | `SqlTier.GeneralPurpose` |
| **Serverless** | tráfico intermitente / staging | **≈ 0 €** parada | `SqlTier.GeneralPurposeServerless` |
| **Hyperscale** | > 1 TB y creciendo | ~495 €/mes (1 TB) | `SqlTier.Hyperscale` |
| **Business Critical** | apps críticas con failover rápido + read replicas | ~410 €/mes (2 vCore) | — |

[`SqlTierAdvisor.cs`](src/Sql.Demo.Api/Sql/SqlTierAdvisor.cs) es esa tabla expresada como **función pura**:

```csharp
public static SqlTier Sugerir(bool intermitente, int maxConexiones, int datosGb)
{
    if (datosGb > LimiteGbHyperscale) return SqlTier.Hyperscale;        // > 1 TB
    if (intermitente)                  return SqlTier.GeneralPurposeServerless;
    if (maxConexiones > MaxConexionesS0) return SqlTier.GeneralPurpose; // > 60
    if (datosGb <= 2 && maxConexiones <= 5) return SqlTier.Basic;
    return SqlTier.S0;
}
```

Lo expone el endpoint `GET /sql/tier-sugerido?intermitente=…&…` para que juegues con la curva **sin tocar Azure**. Es la misma idea que el `AccessTierPolicy` de S5.1: la decisión modelada como lógica pura y testeable.

> 💡 **Serverless es el truco de coste.** Para prácticas y entornos de staging, Serverless con auto-pausa cuesta ≈ 0 € parado (Slide 5). El precio es el cold start: 10-30 segundos cuando llega la primera query tras la pausa. Pero ese cold start es exactamente lo que provocaba el error de la historia de sección 2 — y la solución no es renunciar a Serverless, es activar el retry (sección 7).

---

## 7. Resiliencia y rendimiento: lo que te salva en Azure

Tres clases puras en `Sql/` que parecen configuración pero son **decisiones**. Cada una está ahí porque sin ella tu app se cae el primer día. Volvemos al cliente de sección 2.

### 7.1 Connection pooling (Slide 10)

[`SqlConnectionTuning.cs`](src/Sql.Demo.Api/Sql/SqlConnectionTuning.cs) toma la cadena base y le **fuerza** los ajustes correctos:

```csharp
MaxPoolSize = 100, MinPoolSize = 5, Pooling = true, ConnectTimeout = 30
Encrypt = true                        // si el alumno no lo desactivó
ConnectRetryCount = 3
```

Y es **idempotente**: re-afinar una cadena ya afinada no la rompe. Eso permite componerla con seguridad en cualquier punto.

> 🧠 **El error más caro:** abrir una `SqlConnection` por petición sin pool. EF Core ya gestiona el pool, pero el tamaño se configura en la cadena. Azure SQL S0 admite unas 60 conexiones simultáneas; si agotas el pool, las queries empiezan a fallar con *"Login failed due to resource limit"*. Si tu workload pasa de 60 conexiones, el advisor sugiere General Purpose (vCore), no más S0.

### 7.2 Retry de errores transitorios (Slide 13)

[`AzureSqlRetryPolicy.cs`](src/Sql.Demo.Api/Sql/AzureSqlRetryPolicy.cs) es la lista de **códigos de error transitorios documentados** de Azure SQL:

```csharp
4060   // no se pudo abrir la base de datos
40197  // el servicio encontró un error procesando
40501  // servicio ocupado (throttling)
40613  // base de datos no disponible (despertando / failover)
49918, 49919, 49920  // throttling / capacidad
```

[`Program.cs`](src/Sql.Demo.Api/Program.cs) los enchufa al `EnableRetryOnFailure` de EF Core: cinco reintentos, hasta 30 s de delay máximo, backoff exponencial automático.

Vuelve a la historia del principio. El cliente del que hablaba estaba mirando exactamente este código mucho antes de saberlo. Cuando llegaba el cold start de Serverless, la base devolvía 40613 — uno de los códigos de la lista de arriba. El `EnableRetryOnFailure` lo reintenta unas cuantas veces con backoff. La diferencia entre con retry y sin retry, para el alumno, es: un fallo invisible cada cierto tiempo en vez de un 500 cada hora.

### 7.3 CommandTimeout

`sql.CommandTimeout(60)` en `Program.cs`. El default de SqlClient es 30 segundos. Para queries de reporting (Slide 9), 60 segundos deja margen sin esconder problemas serios. Si una query tarda más, no es que necesite más timeout: es que necesita un índice.

---

## 8. Seguridad y conexión: el if de Program.cs

Tres modos de autenticarse (Slide 6/20):

**1. SQL auth con `User Id` + `Password`.** Funciona, pero la contraseña vive en config. Para desarrollo y para Testcontainers, vale. Para producción, no.

**2. SAS / firma temporal.** No aplica a SQL Database — es cosa de Storage.

**3. Entra ID / Managed Identity.** `Authentication=Active Directory Default`, **sin password**. La identidad de la app la verifica Entra ID y le asignas el rol mínimo en la base. Es lo que exige el checklist de producción (sección 12).

Mira [`Program.cs`](src/Sql.Demo.Api/Program.cs):

```csharp
var rawCs = builder.Configuration["SqlConnection"];
var connectionString = string.IsNullOrWhiteSpace(rawCs)
    ? "Server=(localdb)\\MSSQLLocalDB;Database=VentasDemo;Trusted_Connection=True;"
    : SqlConnectionTuning.Afinar(rawCs);  // pool + Encrypt
```

> 🎓 **Por qué un placeholder LocalDB y no un fallo duro.** Sin `SqlConnection` configurada, la app no conecta a nada — pero EF necesita conocer el provider para generar migraciones (`dotnet ef`) y para que los tests de DI resuelvan el grafo. Ese placeholder está justo para eso: que `dotnet ef migrations add` y la CAPA 0 de DI funcionen sin BD. En cuanto pongas tu cadena (Docker, Azure SQL), la real gana.

`SqlConnectionTuning.UsaManagedIdentity(cs)` detecta si la cadena usa Entra ID sin password — lo expone el endpoint `/sql/conn-info` (sin filtrar secretos, solo el booleano y los límites de pool). Es el seam del **checklist** (sección 12).

> ⚠️ **Encrypt vs TrustServerCertificate.** `Afinar` fuerza `Encrypt=true` si el alumno no lo desactivó. Testcontainers usa un certificado autofirmado → la cadena de Testcontainers lleva `TrustServerCertificate=True` y la respeta. Para Azure SQL real: **siempre Encrypt y nunca TrustServerCertificate**.

---

## 9. Recorrido guiado: vender un producto en una transacción

Lanza la API (ver sección 11) y abre [`api.http`](src/Sql.Demo.Api/api.http). No ejecutes por ejecutar: predice qué va a pasar antes de mirar.

| # | Petición | Respuesta esperada | Qué demuestra |
| --- | --- | --- | --- |
| 1 | `POST /productos` con un teclado, precio 89.90, stock 50 | `201 Created` con `id`, `precio: 89.90` exacto | Esquema explícito con `decimal(18,2)` (sección 5.1). El precio no se trunca. |
| 2 | `GET /productos` | lista ordenada por `Nombre`, sin tracking | `AsNoTracking` + índice en `Nombre` (sección 5.3). |
| 3 | `POST /pedidos` `{ productoId:1, cantidad:3 }` | `201 Created`, pedido con `total = 269.70` | **Una transacción ACID**: crea pedido + descuenta stock en un solo `SaveChanges` (sección 5.3). |
| 4 | `GET /productos/1` | `stock: 47` (era 50, restó 3) | El stock cambió dentro de la misma operación. |
| 5 | `POST /pedidos` `{ productoId:1, cantidad:9999 }` | `409 Conflict` `{ "error": "Stock insuficiente" }` | Negocio sin excepciones: `CrearPedidoResultado.StockInsuficiente` → 409. El stock **no se mueve**. |
| 6 | `POST /pedidos` `{ productoId:999, cantidad:1 }` | `404 Not Found` | Mismo patrón con `ProductoNoExiste`. |
| 7 | `GET /pedidos` | lista con `productoNombre` ya incluido | `Include` evita N+1 (sección 5.3). |
| 8 | `DELETE /productos/1` (con pedidos asociados) | error del provider (FK Restrict) | La regla del modelo (`OnDelete(Restrict)`) impide huérfanos. |
| 9 | `GET /sql/tier-sugerido?intermitente=true&maxConexiones=10&datosGb=5` | `{ tier: "GeneralPurposeServerless" }` | Lógica pura, sin tocar SQL. Prueba `intermitente=false`, `maxConexiones=80`, `datosGb=2000`. |
| 10 | `GET /sql/conn-info` | `{ configurada, usaManagedIdentity, maxPoolSize:100, minPoolSize:5 }` | El estado de la cadena efectiva, sin filtrar secretos. |

Un experimento que vale más que la teoría: en el paso 5, mira `/productos/1` **antes** y **después** del `409`. Si la transacción fallara y descontara stock sin crear pedido, verías cambio. No lo verás. La transacción se aborta entera. Acabas de *ver* la A de ACID — atomicidad — en directo. El ejemplo no te lo cuenta; te deja descubrirlo.

Los pasos 9 y 10 son los únicos que no llaman a SQL: lógica pura. Por eso la CAPA 1 de tests corre en milisegundos y sin Docker (sección 10).

---

## 10. Por qué el código y los tests están así

La organización es la misma de S5.1 con un eslabón extra: una **CAPA 2** con SQLite in-memory que no existía allí.

- **`Sql/` — lógica pura.** `SqlTierAdvisor`, `SqlConnectionTuning`, `AzureSqlRetryPolicy`. Decisiones modeladas como funciones puras, testeables en milisegundos.
- **`Data/VentasDbContext.cs`.** El modelo, sin provider. Sirve para los tres escenarios (SQL Server, SQLite, Testcontainers).
- **`Repositories/`.** Los repos delgados con `AsNoTracking` / `Include`.
- **`Endpoints/`.** Minimal API fina. Mapea resultados de negocio a status codes (`201` / `404` / `409`).
- **`Migrations/`.** `InitialCreate` versionada.

Los tests, en **cuatro capas** (no tres como S5.1):

- **CAPA 1 · Unit** — `Unit_SqlTierAdvisorTests`, `Unit_SqlConnectionTuningTests`, `Unit_AzureSqlRetryPolicyTests`. La lógica pura. Sin SQL.
- **CAPA 2 · Component (SQLite in-memory)** — `Component_RepositoriosSqliteTests`. El **DbContext real** y los **repos reales** contra una base relacional de verdad, **sin Docker**. Aquí se valida el modelo, el `Include`, el `OrderBy` (de ahí el `DateTime` vs `DateTimeOffset` de sección 5.2) y la regla de descontar stock.
- **CAPA 0 · DI** — `DiContainer_Tests`. Resuelve `VentasDbContext` y los repos del `WebApplicationFactory` real en un scope. **No toca la base**. Cubre la lección DI sin Docker — porque sin esto, un servicio sin registrar pasa los unit tests y rompe en runtime.
- **CAPA 3 · Integration** — `Integration_SqlServerTests`. Round-trip **real** contra **SQL Server en Docker** (Testcontainers.MsSql) por la API completa. **Aplica la migración** `InitialCreate` de verdad (Slide 8), ejercita el provider SqlServer y el retry. `SkippableFact`: si no hay Docker, se salta y la suite sigue verde.

> 🎓 **Por qué CAPA 2 con SQLite.** SQLite *no* es SQL Server: las migraciones SQL Server-specific no se aplican; CAPA 2 usa `EnsureCreated()`. Lo que **sí** ejercita es el modelo EF Core real, los `Include`, los `OrderBy`, los `decimal(18,2)`, las reglas de FK y la lógica del repo — sin necesidad de Docker. Es la capa que cubre el 80% del valor de tests con el 5% del coste de la CAPA 3.

---

## 11. Puesta en marcha, ejecución y pruebas

Sección operativa. Datos verificados contra el repo.

### 11.1 Requisitos

| Requisito | Versión / cómo | Para qué | ¿Obligatorio? |
| --- | --- | --- | --- |
| .NET SDK | **10.x** — fijado en [`global.json`](global.json) (`10.0.300-preview…`, `rollForward: latestFeature`) | compilar y ejecutar | Sí |
| `dotnet-ef` | `dotnet tool install --global dotnet-ef --version 10.0.2` | aplicar migración (`dotnet ef database update`) | Sí (para arrancar contra SQL Server) |
| SQL Server (Docker) | imagen `mcr.microsoft.com/mssql/server:2022-latest` | base de datos para la API y CAPA 3 | Sí (CAPA 3) / opcional para correr la API |
| Docker | Docker Desktop | levantar SQL Server y el contenedor de Testcontainers | Sí (para CAPA 3) |
| Cliente REST | extensión *REST Client* de VS Code, o `curl` | lanzar las peticiones de [`api.http`](src/Sql.Demo.Api/api.http) | Recomendado |

La versión de los paquetes EF Core debe **coincidir** con la del CLI `dotnet-ef`. El csproj fija EF Core 10.0.2; instala la 10.0.2 del tool. Es el primer "no compila / no migra" más común.

### 11.2 Compilar (verificación rápida sin BD)

```bash
cd examples/M05-Almacenamiento-BBDD/S5.2-azure-sql-database
dotnet build Sql.Demo.slnx
```

Debe terminar con **0 errores y 0 warnings** (`TreatWarningsAsErrors=true`).

### 11.3 Arrancar SQL Server local

```bash
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=Tu_Password123" \
  -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest
```

[`appsettings.Development.json`](src/Sql.Demo.Api/appsettings.Development.json) ya viene apuntando a ese contenedor (`Server=localhost,1433;…;User Id=sa;Password=Tu_Password123;TrustServerCertificate=True;Encrypt=False`). Ajusta el password al tuyo si lo cambias en `docker run`. Para Azure SQL real, **nunca** uses `Encrypt=False` ni `TrustServerCertificate=True`.

### 11.4 Aplicar la migración

```bash
dotnet ef database update --project src/Sql.Demo.Api
```

Esto crea las tablas `Productos` y `Pedidos` aplicando `InitialCreate`. Es el paso **explícito** que `Program.cs` deja fuera del arranque (sección 5.4). En producción haces lo mismo, contra la cadena de Azure SQL.

Si `dotnet ef migrations remove` se cuelga, suele ser porque la cadena configurada apunta a una base que no contesta. Regenera borrando `Migrations/` y vuelve a `dotnet ef migrations add` (`add` no conecta a la base).

### 11.5 Lanzar la API

```bash
dotnet run --project src/Sql.Demo.Api
```

- Escucha en **`http://localhost:5082`** ([`launchSettings.json`](src/Sql.Demo.Api/Properties/launchSettings.json), perfil `http`, entorno `Development`).
- Prueba de vida: `GET http://localhost:5082/health` → `{ "status": "ok" }`.

El curso nunca lanza la app por ti. Este `dotnet run` lo ejecutas tú; la verificación automatizada se queda en *build + test*.

### 11.6 Ejercitar el ejemplo

[`api.http`](src/Sql.Demo.Api/api.http) trae el guion completo. Por línea de comandos, los más útiles:

```bash
# Crear un producto
curl -X POST http://localhost:5082/productos -H "Content-Type: application/json" \
  -d '{ "nombre": "Teclado mecánico", "precio": 89.90, "stock": 50 }'

# Crear un pedido (descuenta stock atómicamente)
curl -X POST http://localhost:5082/pedidos -H "Content-Type: application/json" \
  -d '{ "productoId": 1, "cantidad": 3 }'

# Recomendación de tier (lógica pura, no necesita BD)
curl "http://localhost:5082/sql/tier-sugerido?intermitente=true&maxConexiones=10&datosGb=5"
```

Sigue el recorrido de sección 9 para saber *qué mirar*.

### 11.7 Pasar los tests

```bash
dotnet test Sql.Demo.slnx
```

| Sin Docker | Con Docker corriendo |
| --- | --- |
| **31 pass · 1 skip · 0 fail** | **32 pass · 0 skip · 0 fail** |

- **CAPA 1 (unit)** y **CAPA 2 (SQLite in-memory)** corren siempre — son rápidas y no necesitan Docker.
- **CAPA 0 (DI container)** también corre siempre: resuelve el grafo contra el `WebApplicationFactory` real sin tocar BD.
- **CAPA 3** es un `SkippableFact`: levanta SQL Server con Testcontainers, aplica la migración real y ejercita el provider. Sin Docker se salta — *no es fallo*. Con Docker pasa a verde.

### 11.8 Problemas frecuentes

| Síntoma | Causa | Solución |
| --- | --- | --- |
| `A network-related or instance-specific error` | SQL Server no está corriendo | arranca el contenedor (sección 11.3) |
| `Login failed for user 'sa'` | password distinto al del `docker run` | sincroniza la cadena de `appsettings.Development.json` con tu password |
| `The certificate chain was issued by an authority that is not trusted` | falta `TrustServerCertificate=True` en local | ya está en `appsettings.Development.json`; comprueba que no se sobrescribió |
| `No DbContextOptionsBuilder.Use…SqlServer/Sqlite called` | falta el provider o tool con versión distinta | `dotnet tool install -g dotnet-ef --version 10.0.2` |
| El CLI `dotnet ef` falla por SDK | `global.json` fija 10.x; tu SDK es otro | instala .NET SDK 10 |
| CAPA 3 sale como *skip* | no hay Docker | esperado; arranca Docker si quieres correrla |
| `NU1605` Azure.Identity | versión < 1.14.2 | el csproj ya fija `1.14.2`, no la bajes |

### 11.9 Contra Azure SQL real (opcional)

Configura `SqlConnection` con la cadena de Azure SQL — recomendado en modo Managed Identity (Slide 6/20):

```
Server=tcp:<srv>.database.windows.net,1433;Database=<db>;Authentication=Active Directory Default;Encrypt=true;
```

`SqlConnectionTuning.Afinar` añadirá pooling y respetará `Encrypt=true`. El detalle del aprovisionamiento por **Portal** (servidor lógico, DB serverless, firewall, Entra-only auth) y los scripts `az` están en el [`README.md`](README.md) — este manual no los repite a propósito.

---

## 12. Checklist de producción (y de qué te protege cada línea)

| Casilla (Slide 20) | De qué te protege |
| --- | --- |
| Autenticación con Entra ID (no SQL auth) | Que un password filtrado dé acceso total |
| Managed Identity desde App Service / Functions | Tener secretos en config (sección 8) |
| Firewall: solo IPs y servicios necesarios | Acceso desde redes no autorizadas |
| TDE habilitado (cifrado at-rest) | Datos legibles si alguien roba el disco subyacente — Azure lo activa por defecto |
| Auditing habilitado | No enterarte de un `SELECT *` de quien no debía |
| Threat Detection activado | SQL injection, brute force, accesos anómalos |
| Backup retention 7-35 días + LTR si aplica | El `DELETE` sin `WHERE` a las 15:00 (Slide 11: PITR a las 14:59) |
| Auto-tuning habilitado | Que tu carga degrade sin que nadie lo vea |
| Connection pooling en la app | Agotar el pool y caer con *resource limit* (sección 7.1) |
| Retry on transient failure | El cold start de Serverless y los blips (sección 7.2) — la historia de sección 2 |
| Performance Insight revisado semanalmente | Una query mal indexada consumiendo el 80% de tus DTU |

---

## 13. Ideas para llevarte

Lo primero, la pregunta-eje del submódulo: SQL es por las **relaciones**, los **JOINs** y la **transacción ACID**. No por costumbre, no porque suene serio, no porque sea lo que pone en el currículum. Si tus datos no necesitan ninguna de las tres, lo correcto vivirá en S5.1 o en S5.3 según el caso. Pero cuando las necesitan — y la app de ventas las necesita — SQL es la respuesta.

Lo segundo es el matiz que más se subestima: **el error más caro está en la conexión, no en el modelo**. Pool, Encrypt, retry de transitorios, Managed Identity. Sin esto tu app funciona en local y se cae en Azure en cuanto Serverless decide echarse una siesta. La historia del cliente de sección 2 es real y se repite cada semana en cualquier curso de Azure. No la subestimes.

Y tres reglas que te ahorran dolores de cabeza concretos: **`AsNoTracking` para leer**, **`Include` para evitar N+1**, **`decimal(18,2)` para precios**. Tres reglas pequeñas, ochenta por ciento de los problemas evitados.

Para terminar, una recomendación honesta: si vas a producción con Serverless en lugar de S0 — y para muchos casos compensa por coste —, no te olvides del retry el día del deploy. No es opcional. Lo que en local es transparente, en Azure se nota en cuanto la base se duerme.

---

## 14. Comprueba que lo has entendido

Sin mirar atrás. Si dudas, vuelve a la sección.

1. ¿Por qué `Pedido.Fecha` es `DateTime` y no `DateTimeOffset`? *(sección 5.2)*
2. Creas un pedido con cantidad > stock. ¿Qué ves en el `stock` del producto antes y después de la petición? ¿Y qué status code te devuelve? *(sección 5.3, sección 9 paso 5)*
3. Tu API funciona en local pero en Azure falla a veces con `"Database is not currently available"`. ¿Qué le falta y por qué? *(sección 7.2)*
4. Te piden meter `Database.Migrate()` en `Program.cs` "para automatizar despliegues". ¿Por qué dices que no? *(sección 5.4)*
5. Listas 1000 pedidos y luego, en un bucle, accedes a `pedido.Producto.Nombre`. EF Core lanza 1001 queries. ¿Cómo se llama eso, qué método lo evita y por qué? *(sección 5.3)*
6. Tu base ocupa 800 GB y crece despacio, sin ráfagas. ¿Qué tier sugiere `SqlTierAdvisor`? ¿Y si fueran 2 TB? *(sección 6)*
7. ¿Qué validan exactamente la CAPA 2 (SQLite) y la CAPA 3 (Testcontainers) que **no** valida la otra? *(sección 10)*

<details>
<summary>Respuestas</summary>

1. Porque **SQLite no soporta `ORDER BY` sobre `DateTimeOffset`** (`NotSupportedException`). La query de `ListarAsync` ordena por fecha; con `DateTimeOffset` la CAPA 2 (SQLite in-memory) rompería. `DateTime` UTC funciona en SQL Server (`datetime2`) y en SQLite. Es la decisión técnica que solo se entiende cuando alguien la pisa una vez.
2. El stock **no cambia** ni antes ni después: la validación `producto.Stock < dto.Cantidad` ocurre antes del decremento, y la transacción se aborta antes de tocar nada. El endpoint devuelve **`409 Conflict`** con `{ "error": "Stock insuficiente" }`.
3. Le falta el **retry de errores transitorios** (`EnableRetryOnFailure` con los códigos de `AzureSqlRetryPolicy.ErroresTransitorios`). Azure SQL Serverless se pausa tras una hora sin actividad y tarda 10-30 s en despertar — en ese intervalo devuelve **40613**. Sin retry, esa primera petición tras la pausa siempre falla. Era exactamente la historia del cliente de sección 2.
4. **Anti-patrón 8** (Slide 35). Race conditions con varias réplicas en deploy, no atómico con el despliegue, no revisable antes de aplicar. La migración se aplica por pipeline o script con aprobación humana. El test de integración sí migra dentro de su scope porque es código controlado.
5. **N+1**. Lo evita `.Include(p => p.Producto)` — un `LEFT JOIN` en SQL. Sin `Include`, EF carga la navegación de forma lazy o explícita y emite una query por cada pedido. Con 1000 pedidos, 1001 round-trips a la base.
6. **800 GB sin ráfagas → S0** (≤ 1 TB, no es intermitente, no pasa el límite de conexiones). **2 TB → Hyperscale** (supera `LimiteGbHyperscale = 1024`).
7. **CAPA 2 (SQLite)** valida el modelo EF, las queries (`Include`, `OrderBy`, proyecciones), las reglas de negocio del repo (descontar stock, calcular total) — sin Docker. **CAPA 3 (Testcontainers)** valida lo que SQLite no puede: el **provider SqlServer**, las migraciones SQL Server-specific, el retry de transitorios y el round-trip por la API real.

</details>

---

## 15. Hasta aquí

Vuelve un momento a la imagen del archivador de sección 4. Cajones, etiquetas, llaves, reglas en pegatinas amarillas, un conserje que no te deja meter una ficha rota. Esa imagen — más que cualquier diagrama de DBContext — es lo que tienes que tener en la cabeza la próxima vez que diseñes un esquema. Las reglas no se imponen "después" en el código de aplicación: se escriben en el esquema y el motor las hace cumplir incluso si alguien intenta saltárselas desde un cliente SQL.

S5.3 te lleva al otro lado de la decisión, pero dentro de las bases de datos: cuándo NoSQL en serio. Cosmos DB no es un archivador con cajones: es un repositorio distribuido por particiones, con su propio modelo de coste (RU/s) y un patrón mental completamente distinto. Lo aviso porque la tentación al venir de SQL es modelar Cosmos como si fueran tablas, y ese es el camino directo a la factura que se nos quedó pendiente en S5.1. Allí lo veremos.
