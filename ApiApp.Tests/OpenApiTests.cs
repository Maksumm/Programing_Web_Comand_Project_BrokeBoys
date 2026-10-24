using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ApiApp.Tests;

public class OpenApiTests
{
    public static IEnumerable<object[]> Operations()
    {
        // (ресурс, чи може видалення дати 409)
        foreach (var (resource, deleteConflicts) in new[] { ("categories", true), ("products", false), ("prices", false), ("subscriptions", false) })
        {
            yield return [$"/api/{resource}", "get", "200"];
            yield return [$"/api/{resource}", "post", "201,400,409"];
            yield return [$"/api/{resource}/{{id}}", "get", "200,404"];
            yield return [$"/api/{resource}/{{id}}", "put", "204,400,404,409"];
            yield return [$"/api/{resource}/{{id}}", "delete", deleteConflicts ? "204,404,409" : "204,404"];
        }
    }

    private static string Normalize(string path) => Regex.Replace(path, @"\{(\w+):\w+\}", "{$1}").TrimEnd('/');

    [Theory]
    [MemberData(nameof(Operations))]
    public async Task Document_ListsEveryCodeOfEveryEndpoint(string path, string method, string codes)
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var operation = document.RootElement.GetProperty("paths").EnumerateObject()
            .Where(p => Normalize(p.Name) == path && p.Value.TryGetProperty(method, out _))
            .Select(p => p.Value.GetProperty(method))
            .FirstOrDefault();

        Assert.True(operation.ValueKind == JsonValueKind.Object, $"{method.ToUpperInvariant()} {path} is missing from the OpenAPI document.");

        var responses = operation.GetProperty("responses");
        foreach (var code in codes.Split(','))
        {
            Assert.True(responses.TryGetProperty(code, out _), $"{method.ToUpperInvariant()} {path} does not document {code}.");
        }
    }

    [Fact]
    public async Task Document_IsNotPublished_OutsideDevelopment()
    {
        using var factory = new ApiFactory { EnvironmentName = "Production" };
        using var client = factory.CreateClient();

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/openapi/v1.json")).StatusCode);
    }
}
