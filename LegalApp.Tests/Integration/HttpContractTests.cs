using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;

namespace LegalApp.Tests.Integration;

public sealed class HttpContractTests
{
    private const string CorrelationId = "integration-correlation-id";

    [Fact]
    public async Task Protected_endpoint_without_token_returns_401_problem_details()
    {
        using var factory = new ControlLexApiFactory();
        using var client = CreateClient(factory);

        var response = await client.GetAsync("/api/Casos");

        await AssertProblemAsync(response, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Protected_endpoint_with_wrong_role_returns_403_problem_details()
    {
        using var factory = new ControlLexApiFactory();
        using var client = CreateClient(factory, "Soporte");

        var response = await client.GetAsync("/api/Casos");

        await AssertProblemAsync(response, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Missing_case_returns_404_problem_details()
    {
        using var factory = new ControlLexApiFactory();
        using var client = CreateClient(factory, "Admin");

        var response = await client.GetAsync("/api/Casos/999");

        await AssertProblemAsync(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Invalid_model_returns_400_problem_details_with_validation_errors()
    {
        using var factory = new ControlLexApiFactory();
        using var client = CreateClient(factory, "Admin");

        var response = await client.PostAsJsonAsync("/api/Usuarios", new
        {
            nombre = "",
            email = "invalid",
            password = "x"
        });

        var problem = await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.True(problem.TryGetProperty("errors", out _));
    }

    [Fact]
    public async Task Protected_demo_user_returns_409_problem_details()
    {
        using var factory = new ControlLexApiFactory();
        using var client = CreateClient(factory, "Admin");

        var response = await client.DeleteAsync("/api/Usuarios/2");

        await AssertProblemAsync(response, HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Unexpected_exception_returns_safe_500_problem_details()
    {
        using var factory = new ControlLexApiFactory(CasoRepositoryFailure.Unexpected);
        using var client = CreateClient(factory, "Admin");

        var response = await client.GetAsync("/api/Casos/1");

        var problem = await AssertProblemAsync(response, HttpStatusCode.InternalServerError);
        Assert.DoesNotContain(
            ControlLexApiFactory.SensitiveMarker,
            problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Dependency_timeout_returns_safe_503_problem_details()
    {
        using var factory = new ControlLexApiFactory(CasoRepositoryFailure.Dependency);
        using var client = CreateClient(factory, "Admin");

        var response = await client.GetAsync("/api/Casos/1");

        var problem = await AssertProblemAsync(response, HttpStatusCode.ServiceUnavailable);
        Assert.DoesNotContain(
            ControlLexApiFactory.SensitiveMarker,
            problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Global_rate_limit_returns_429_problem_details()
    {
        using var factory = new ControlLexApiFactory();
        using var client = CreateClient(factory);
        HttpResponseMessage? response = null;

        for (var request = 0; request < 21; request++)
        {
            response?.Dispose();
            response = await client.GetAsync("/health/live");
        }

        using (response)
        {
            await AssertProblemAsync(response!, HttpStatusCode.TooManyRequests);
        }
    }

    [Fact]
    public async Task Liveness_endpoint_returns_200()
    {
        using var factory = new ControlLexApiFactory();
        using var client = CreateClient(factory);

        var response = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static HttpClient CreateClient(ControlLexApiFactory factory, string? role = null)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Correlation-ID", CorrelationId);

        if (role is not null)
        {
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", CreateToken(role));
        }

        return client;
    }

    private static string CreateToken(string role)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ControlLexApiFactory.JwtKey));
        var token = new JwtSecurityToken(
            issuer: ControlLexApiFactory.JwtIssuer,
            audience: ControlLexApiFactory.JwtAudience,
            claims:
            [
                new Claim(ClaimTypes.NameIdentifier, "integration-user"),
                new Claim(ClaimTypes.Name, "integration@controllex.test"),
                new Claim(ClaimTypes.Role, role)
            ],
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static async Task<JsonElement> AssertProblemAsync(
        HttpResponseMessage response,
        HttpStatusCode expectedStatus)
    {
        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.True(response.Headers.TryGetValues("X-Correlation-ID", out var correlationHeaders));
        Assert.Equal(CorrelationId, Assert.Single(correlationHeaders));

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var problem = document.RootElement;
        Assert.Equal((int)expectedStatus, problem.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(problem.GetProperty("type").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(problem.GetProperty("title").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(problem.GetProperty("traceId").GetString()));
        Assert.Equal(CorrelationId, problem.GetProperty("correlationId").GetString());
        return problem.Clone();
    }
}
