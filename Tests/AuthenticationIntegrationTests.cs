using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Tests;

public sealed class AuthenticationIntegrationTests : IClassFixture<DemoApplicationFactory>
{
    private readonly DemoApplicationFactory _factory;

    public AuthenticationIntegrationTests(DemoApplicationFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("admin@mess.local", "Admin123!", "/RoleDashboards/SystemAdministrator")]
    [InlineData("finance@mess.local", "Finance123!", "/RoleDashboards/FinanceOfficer")]
    [InlineData("student@mess.local", "Student123!", "/RoleDashboards/Student")]
    public async Task Login_form_authenticates_demo_user_and_redirects_by_role(
        string email,
        string password,
        string expectedLocation)
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var loginPage = await client.GetAsync("/Identity/Account/Login");
        var loginHtml = await loginPage.Content.ReadAsStringAsync();
        var token = Regex.Match(
            loginHtml,
            "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"|value=\"([^\"]+)\"[^>]*name=\"__RequestVerificationToken\"")
            .Groups
            .Cast<Group>()
            .Skip(1)
            .First(group => group.Success)
            .Value;

        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Input.Email"] = email,
            ["Input.Password"] = password,
            ["Input.RememberMe"] = "false",
            ["Input.ReturnUrl"] = expectedLocation
        });

        var response = await client.PostAsync(
            "/Identity/Account/Login?returnUrl=" + Uri.EscapeDataString(expectedLocation),
            form);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(expectedLocation, response.Headers.Location?.OriginalString);
    }
}

public sealed class DemoApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databasePath = Path.Combine(
        Path.GetTempPath(),
        "messforecast-auth-" + Guid.NewGuid().ToString("N") + ".db");

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("DatabaseProvider", "Sqlite");
        builder.UseSetting("ConnectionStrings:DefaultConnection", $"Data Source={_databasePath};Cache=Shared");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            try
            {
                File.Delete(_databasePath);
                File.Delete(_databasePath + "-shm");
                File.Delete(_databasePath + "-wal");
            }
            catch (IOException)
            {
                // The SQLite connection may still be releasing its file handles.
            }
        }
    }
}
