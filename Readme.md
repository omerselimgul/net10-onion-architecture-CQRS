# ECommercialApi — Teknik Dokümantasyon

ASP.NET Core (.NET 10) tabanlı, Onion Architecture ile tasarlanmış e-ticaret REST API projesi.

---

## Mimari

İçteki katmanlar dışarıyı tanımaz. Bağımlılık yönü **her zaman dışarıdan içeriye** doğrudur.

```
┌──────────────────────────────────────┐
│         Presentation (Api)           │
│  ┌────────────────────────────────┐  │
│  │       Infrastructure           │  │
│  │  ┌──────────────────────────┐  │  │
│  │  │      Application         │  │  │
│  │  │  ┌────────────────────┐  │  │  │
│  │  │  │      Domain        │  │  │  │
│  │  │  └────────────────────┘  │  │  │
│  │  └──────────────────────────┘  │  │
│  └────────────────────────────────┘  │
└──────────────────────────────────────┘
```

| Katman | Proje | Görev |
|--------|-------|-------|
| Domain | `ECommercialApi.Domain` | Entity sınıfları — hiçbir şeye bağımlı değil |
| Application | `ECommercialApi.Application` | Repository interface'leri — sadece Domain'e bağımlı |
| Persistence | `ECommercialApi.Persistence` | EF Core implementasyonları — Domain + Application'a bağımlı |
| Infrastructure | `ECommercialApi.Infrastructure` | Harici servisler (şimdilik boş) |
| Presentation | `ECommercialApi.Api` | Controller'lar — Application interface'lerine bağımlı |

**Bağımlılık akışı:**
```
Api  →  Application (interface inject eder, somut sınıfı tanımaz)
Api  →  Persistence (sadece ServiceRegistration için)
Persistence  →  Application (interface'leri implement eder)
Persistence  →  Domain (entity'leri kullanır)
Application  →  Domain (BaseEntity constraint için)
Domain  →  Hiçbir şey
```

---

## Repository Pattern

### Neden böyle bir yapı kuruldu?

`ProductsController`, doğrudan `ProductReadRepository`'yi değil, `IProductReadRepository` interface'ini inject eder.
Yani controller, veritabanının PostgreSQL mi başka bir şey mi olduğunu bilmez. Yarın veritabanı değişse sadece `Persistence` katmanı değişir, controller'a dokunulmaz.

### Interface hiyerarşisi

```
IRepository<T>              → DbSet<T> Table property'si (ortak taban)
├── IReadRepository<T>      → okuma metodları
└── IWriteRepository<T>     → yazma metodları
      |
      ↓ Persistence katmanında implement edilir
      ReadRepository<T>     → generic implementasyon
      WriteRepository<T>    → generic implementasyon
            |
            ↓ entity-specific sınıflar köprü kurar
            ProductReadRepository  : ReadRepository<Product>, IProductReadRepository
            ProductWriteRepository : WriteRepository<Product>, IProductWriteRepository
            (Customer ve Order için de aynı yapı)
```

### Neden entity-specific interface'ler boş ama yine de var?

```csharp
public interface IProductReadRepository : IReadRepository<Product> { }
```

İki sebep:
1. DI kaydı için somut bir tip gerekir: `services.AddScoped<IProductReadRepository, ProductReadRepository>()`
2. İleride ürüne özel metod eklenecekse (ör. `GetByCategory()`), generic interface'i bozmadan buraya eklenir.

### Neden entity-specific sınıflar `internal`?

```csharp
internal class ProductReadRepository : ReadRepository<Product>, IProductReadRepository
```

Controller'ların doğrudan `ProductReadRepository`'ye erişmesini engeller. Sadece interface üzerinden erişilmeli. `internal` bunu zorla sağlar.

---

## Önemli Fonksiyonlar

### `DbSet<T> Table` — `_context.Set<T>()`

```csharp
public DbSet<T> Table => _context.Set<T>();
```

DbContext'te `DbSet<Product> Products` gibi property tanımlamak yerine `Set<T>()` kullanıldı.
Generic repository hangi entity ile çalışacağını derleme zamanında bilmez; `Set<T>()` runtime'da doğru `DbSet`'i döndürür.
Bu sayede her entity için ayrı DbContext property yazmak gerekmez.

---

### `GetAll` / `GetWhere` — `IQueryable<T>` döndürür

```csharp
public IQueryable<T> GetAll(bool tracking = true)
{
    var query = Table.AsQueryable();
    if (!tracking)
        query = query.AsNoTracking();
    return query;
}

public IQueryable<T> GetWhere(Expression<Func<T, bool>> method, bool tracking = true)
{
    var query = Table.Where(method);
    if (!tracking)
        query = query.AsNoTracking();
    return query;
}
```

**Neden `List<T>` değil `IQueryable<T>`?**
`IQueryable` sorguyu hemen çalıştırmaz (deferred execution). Controller tarafında `.Where()`, `.Select()`, `.OrderBy()`, `.Take()` zincirlenebilir ve tüm bunlar tek bir SQL olarak veritabanına gider. `List<T>` dönseydi tüm tablo belleğe çekilir, filtreleme C#'ta yapılırdı.

---

### `tracking` parametresi

Her okuma metodunda `bool tracking = true` parametresi var.

| Durum | Kullanım |
|-------|----------|
| Sadece görüntüleme (liste, detay) | `tracking: false` → `AsNoTracking()` → daha az bellek, daha hızlı |
| Güncelleme yapılacak | `tracking: true` (default) → EF nesneyi izler, `SaveAsync()` yeterli |

---

### `GetByIdAsync` — neden `FindAsync` değil?

```csharp
public async Task<T?> GetByIdAsync(string id, bool tracking = true)
{
    var query = Table.AsQueryable();
    if (!tracking)
        query = query.AsNoTracking();
    return await query.FirstOrDefaultAsync(data => data.Id == Guid.Parse(id));
}
```

`FindAsync` önce Change Tracker'a bakar, sonra DB'ye gider; `AsNoTracking()` ile birlikte tutarsız davranır.
`FirstOrDefaultAsync` her durumda aynı şekilde çalışır. `id` parametresi `string` çünkü HTTP route'lardan gelen değerler string'tir — dönüşüm burada yapılır.

---

### `AddAsync` / `Remove` / `Update` — neden veritabanına yazmaz?

```csharp
public async Task<bool> AddAsync(T entity)
{
    EntityEntry<T> entityEntry = await Table.AddAsync(entity);
    return entityEntry.State == EntityState.Added;
}

public bool Remove(T entity)
{
    EntityEntry<T> entityEntry = Table.Remove(entity);
    return entityEntry.State == EntityState.Deleted;
}

public bool Update(T entity)
{
    EntityEntry<T> entityEntry = Table.Update(entity);
    return entityEntry.State == EntityState.Modified;
}

public async Task<int> SaveAsync()
    => await _context.SaveChangesAsync();
```

Bu **Unit of Work** pattern'idir. `Add`, `Remove`, `Update` sadece nesnenin state'ini bellekte işaretler. `SaveAsync()` çağrılana kadar veritabanına hiçbir şey yazılmaz. Bu şekilde birden fazla işlem toplanıp tek bir SQL transaction'ında commit edilebilir.

**Önemli not — `tracking=false` ile update:**
```csharp
// YANLIŞ: tracking=false ile alınan nesneyi Update() çağırmadan kaydetmek çalışmaz
var p = await _read.GetByIdAsync(id, false);
p.Name = "yeni";
await _write.SaveAsync(); // EF izlemediği için değişikliği görmez

// DOĞRU — 1. yol: tracking=true ile al
var p = await _read.GetByIdAsync(id, true);
p.Name = "yeni";
await _write.SaveAsync(); // EF izlediği için değişikliği görür

// DOĞRU — 2. yol: tracking=false + Update() çağır
var p = await _read.GetByIdAsync(id, false);
p.Name = "yeni";
_write.Update(p);
await _write.SaveAsync();
```

---

### `RemoveAsync` — neden iki adım?

```csharp
public async Task<bool> RemoveAsync(string id)
{
    T model = await Table.FirstOrDefaultAsync(data => data.Id == Guid.Parse(id));
    return Remove(model);
}
```

`Remove(entity)` nesneyi parametre olarak alır, ID'yi değil. Önce nesneyi bul, sonra sil. İki adım zorunlu.

---

## EF Core Konfigürasyonu — `ApplyConfigurationsFromAssembly`

```csharp
// ECommercialApiDbContext.cs
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.ApplyConfigurationsFromAssembly(typeof(Product).Assembly);
}
```

`Domain` assembly'sindeki tüm `IEntityTypeConfiguration<T>` implementasyonlarını otomatik bulur ve uygular.
Yeni entity eklendiğinde `DbContext`'e dokunmak gerekmez — entity kendi konfigürasyonunu taşır.

---

## DbContextFactory — Migration İçin

```csharp
public class ECommercialApiDbContextFactory : IDesignTimeDbContextFactory<ECommercialApiDbContext>
{
    public ECommercialApiDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ECommercialApiDbContext>();
        optionsBuilder.UseNpgsql(Configuration.ConnectionString);
        return new ECommercialApiDbContext(optionsBuilder.Options);
    }
}
```

`dotnet ef migrations add` komutu çalıştırıldığında EF Core araçları bir `DbContext` instance'ı oluşturması gerekir. Ama `Persistence` projesi startup projesi değil, DI container'ı yok. Bu factory olmasa migration komutları hata verir.

---

## ServiceRegistration — DI ve `AddScoped`

```csharp
public static void AddPersistenceServices(this IServiceCollection services)
{
    services.AddDbContext<ECommercialApiDbContext>(opt =>
        opt.UseNpgsql(Configuration.ConnectionString));

    services.AddScoped<IProductReadRepository, ProductReadRepository>();
    services.AddScoped<IProductWriteRepository, ProductWriteRepository>();
    // ... Customer, Order
}
```

**Neden `AddScoped`?**
DbContext ve repository'ler aynı HTTP isteği içinde aynı instance'ı paylaşmalıdır ki Unit of Work çalışsın.
- `AddTransient` → her inject'te yeni instance → aynı istek içinde farklı context'ler → veri tutarsızlığı
- `AddSingleton` → thread-safety sorunu, DbContext thread-safe değil
- `AddScoped` → istek başına bir instance → doğru seçim

Extension method olarak tanımlandığı için `Program.cs`'de tek satır:
```csharp
builder.Services.AddPersistenceServices();
```

---

## Configuration — Bağlantı Dizesi

```csharp
static class Configuration
{
    public static string ConnectionString
    {
        get
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "../../Presentation/ECommercialApi.Api"))
                .AddJsonFile("appsettings.json")
                .Build();
            return configuration.GetConnectionString("PostgreSQL");
        }
    }
}
```

`Persistence` katmanında bulunur çünkü `ServiceRegistration` buradan okur. Migration komutu çalıştırılırken CWD `Persistence` klasörüdür, `appsettings.json` ise `Api` projesindedir — göreli yol bu yüzden `../../Presentation/ECommercialApi.Api`.

`appsettings.Development.json` içinde şu yapı olmalı (git'e eklenmez, her geliştirici kendi oluşturur):
```json
{
  "ConnectionStrings": {
    "PostgreSQL": "User ID=postgres;Password=SIFREN;Host=localhost;Port=5432;Database=ECommercialApiDb;Pooling=true;"
  }
}
```

---

## Tam Bağlantı Haritası

```
HTTP GET /api/products/{id}
        │
        ▼
ProductsController
  constructor inject → IProductReadRepository
                              │
                    DI Container çözer
                              │
                              ▼
                    ProductReadRepository   (internal, Persistence)
                      : ReadRepository<Product>
                      : IProductReadRepository
                              │
                              ▼
                    ReadRepository<Product>
                      inject → ECommercialApiDbContext
                              │
                              ▼
                    _context.Set<Product>()
                              │
                              ▼
                    .FirstOrDefaultAsync(...)
                              │
                              ▼
                         PostgreSQL
```

---

## Kurulum

**Gereksinimler:** .NET 10 SDK, PostgreSQL

```bash
git clone repourl
cd net10-onion-architecture-CQRS
```

`Presentation/ECommercialApi.Api/` klasörüne `appsettings.Development.json` dosyası oluştur:

```json
{
  "ConnectionStrings": {
    "PostgreSQL": "User ID=postgres;Password=SIFREN;Host=localhost;Port=5432;Database=ECommercialApiDb;Pooling=true;"
  }
}
```

```bash
# Migration uygula
dotnet ef database update \
  --project Infrastructure/ECommercialApi.Persistence \
  --startup-project Presentation/ECommercialApi.Api

# Projeyi başlat
dotnet run --project Presentation/ECommercialApi.Api
```

Swagger: `https://localhost:{port}/swagger`

---

## Diğer Komutlar

```bash
# Yeni migration ekle
dotnet ef migrations add MigrationAdi \
  --project Infrastructure/ECommercialApi.Persistence \
  --startup-project Presentation/ECommercialApi.Api
```
