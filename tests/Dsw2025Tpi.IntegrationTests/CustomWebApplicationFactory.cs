using System.Data.Common;
using System.Net.Http.Json;
using Dsw2025Tpi.Api;
using Dsw2025Tpi.Application.Dtos;
using Dsw2025Tpi.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dsw2025Tpi.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString = $"Data Source=Dsw2025Tpi_{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
    private DbConnection? _sqliteConnection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // Open master connection so in-memory SQLite remains alive across requests
        if (_sqliteConnection == null)
        {
            _sqliteConnection = new SqliteConnection(_connectionString);
            _sqliteConnection.Open();
        }

        builder.UseSetting("DB_CONNECTION_STRING", _connectionString);
        builder.UseSetting("ConnectionStrings:Dsw2025TpiEntities", _connectionString);
    }

    public async Task ExecuteDbContextAsync(Func<Dsw2025TpiContext, Task> action)
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<Dsw2025TpiContext>();
        await action(context);
    }

    public async Task<string> GetAdminTokenAsync(HttpClient client)
    {
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginModel("admin", "Admin123!"));
        loginResponse.EnsureSuccessStatusCode();
        var content = await loginResponse.Content.ReadFromJsonAsync<LoginResultDto>();
        return content?.Token ?? throw new InvalidOperationException("No token returned for admin");
    }

    private class LoginResultDto
    {
        public string Token { get; set; } = string.Empty;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _sqliteConnection?.Close();
            _sqliteConnection?.Dispose();
        }
    }
}
