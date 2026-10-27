using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using HrEmployeeManagementSystem.Api;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace HrEmployeeManagementSystem.Api.Tests;

public class HrApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=(localdb)\\mssqllocaldb;Database=HrTests;Trusted_Connection=True;TrustServerCertificate=True",
                ["Jwt:Issuer"] = "HrEmployeeManagementSystem",
                ["Jwt:Audience"] = "HrEmployeeManagementSystem",
                ["Jwt:Key"] = "HrEmployeeManagementSystem-dev-signing-key-change-in-production-32bytes-min",
                ["Jwt:ExpiresMinutes"] = "720"
            });
        });
    }
}

public class ApiTests : IClassFixture<HrApiFactory>
{
    private readonly HttpClient _client;

    public ApiTests(HrApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_is_public()
    {
        var response = await _client.GetAsync("/api/health");
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Dashboard_requires_a_token()
    {
        var response = await _client.GetAsync("/api/dashboard");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Admin_can_sign_in_and_read_the_dashboard()
    {
        await SignIn("amila@corehr.lk", "Admin@123456");
        var response = await _client.GetAsync("/api/dashboard");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(154, json.GetProperty("totalEmployees").GetInt32());
        Assert.Equal(12, json.GetProperty("openPositions").GetInt32());
        Assert.True(json.GetProperty("pendingApprovals").GetInt32() >= 2);
        Assert.Equal(3, json.GetProperty("birthdayCount").GetInt32());
    }

    [Fact]
    public async Task Attendance_check_in_and_check_out_round_trip()
    {
        await SignIn("employee@corehr.lk", "Employee@123456");
        var checkIn = await _client.PostAsJsonAsync("/api/attendance/check-in", new { notes = "On site" });
        checkIn.EnsureSuccessStatusCode();
        var body = await checkIn.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Kasun Silva", body.GetProperty("employeeName").GetString());
        Assert.True(body.GetProperty("checkIn").GetString()!.Length > 0);

        var checkOut = await _client.PostAsJsonAsync("/api/attendance/check-out", new { });
        checkOut.EnsureSuccessStatusCode();
        var done = await checkOut.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(done.GetProperty("hours").GetDouble() >= 0);
    }

    [Fact]
    public async Task Leave_can_be_submitted_and_approved()
    {
        await SignIn("employee@corehr.lk", "Employee@123456");
        var types = await _client.GetFromJsonAsync<JsonElement>("/api/leave/types");
        var typeId = types.EnumerateArray().First().GetProperty("id").GetInt32();
        var created = await _client.PostAsJsonAsync("/api/leave", new
        {
            leaveTypeId = typeId,
            startDate = "2027-04-12",
            endDate = "2027-04-13",
            reason = "Family function"
        });
        created.EnsureSuccessStatusCode();
        var leave = await created.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Pending", leave.GetProperty("status").GetString());
        Assert.Equal(2, leave.GetProperty("days").GetInt32());
        var id = leave.GetProperty("id").GetInt32();

        await SignIn("amila@corehr.lk", "Admin@123456");
        var approved = await _client.PostAsJsonAsync($"/api/leave/{id}/approve", new { comment = "Approved for the tests." });
        approved.EnsureSuccessStatusCode();
        var reviewed = await approved.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Approved", reviewed.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Headcount_csv_downloads()
    {
        await SignIn("hr@corehr.lk", "Hr@123456");
        var response = await _client.GetAsync("/api/reports/headcount.csv");
        response.EnsureSuccessStatusCode();
        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);
        var csv = await response.Content.ReadAsStringAsync();
        Assert.Contains("Finance", csv);
        Assert.Contains("Total,154", csv);
    }

    private async Task SignIn(string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", json.GetProperty("token").GetString());
    }
}
