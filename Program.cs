using Application_Camion_API.Data;
using Application_Camion_API.Services;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Npgsql;


var builder = WebApplication.CreateBuilder(args);

string? port = Environment.GetEnvironmentVariable("PORT");

if (!string.IsNullOrWhiteSpace(port))
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");


string connectionString = CreerConnectionStringDatabase(builder.Configuration);


builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(
        connectionString
    ));


builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto;

    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});


builder.Services.AddControllers();

builder.Services.AddHttpClient();

builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalDevelopmentOnly", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5201",
                "https://localhost:7282",
                "http://127.0.0.1:5201",
                "https://127.0.0.1:7282")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});



builder.Services.AddEndpointsApiExplorer();


builder.Services.AddSwaggerGen();



var app = builder.Build();

app.UseForwardedHeaders();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.Migrate();
    await DatabaseSeeder.SeedAsync(dbContext);
}



if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI();
}



app.UseHttpsRedirection();

app.UseCors("LocalDevelopmentOnly");


app.UseAuthorization();


app.MapControllers();


app.Run();


static string CreerConnectionStringDatabase(IConfiguration configuration)
{
    string? databaseUrl =
        Environment.GetEnvironmentVariable("DATABASE_URL");

    if (!string.IsNullOrWhiteSpace(databaseUrl))
        return ConvertirDatabaseUrl(databaseUrl);

    string? connectionString =
        configuration.GetConnectionString("DefaultConnection");

    if (string.IsNullOrWhiteSpace(connectionString))
        throw new InvalidOperationException(
            "Aucune connexion PostgreSQL configuree.");

    return connectionString;
}

static string ConvertirDatabaseUrl(string databaseUrl)
{
    if (!Uri.TryCreate(databaseUrl, UriKind.Absolute, out Uri? uri) ||
        string.IsNullOrWhiteSpace(uri.Host))
    {
        return databaseUrl;
    }

    string[] userInfo =
        uri.UserInfo.Split(':', 2, StringSplitOptions.None);

    var builder = new NpgsqlConnectionStringBuilder
    {
        Host = uri.Host,
        Port = uri.Port > 0 ? uri.Port : 5432,
        Database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')),
        Username = userInfo.Length > 0
            ? Uri.UnescapeDataString(userInfo[0])
            : "",
        Password = userInfo.Length > 1
            ? Uri.UnescapeDataString(userInfo[1])
            : "",
        SslMode = SslMode.Require
    };

    return builder.ConnectionString;
}
