using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// The connection string comes from configuration. In Docker it will be set
// with the environment variable ConnectionStrings__Default.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

var app = builder.Build();

// Create the database and its seed rows if they don't exist yet. This is wrapped
// in try/catch so the API still starts when no database is available (Part 2).
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}
catch (Exception ex)
{
    app.Logger.LogWarning("Database not available at startup: {Message}", ex.Message);
}

app.MapGet("/api/health", () => Results.Ok(new
{
    status = "healthy",
    host = Environment.MachineName   // in Docker this is the container ID
}));

app.MapGet("/api/items", async (AppDbContext db) => await db.Items.ToListAsync());

app.Run();

public class Item
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Item> Items => Set<Item>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.Entity<Item>().HasData(
            new Item { Id = 1, Name = "Keyboard" },
            new Item { Id = 2, Name = "Mouse" },
            new Item { Id = 3, Name = "Monitor" });
}