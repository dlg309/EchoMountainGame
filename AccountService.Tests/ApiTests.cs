using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Xunit;

public sealed class ApiTests
{
    [Fact]
    public async Task CompleteAccountJourneyEnforcesOwnership()
    {
        var path = Path.Combine(Path.GetTempPath(), "echo-api-test-" + Guid.NewGuid().ToString("N") + ".db");
        try
        {
            using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(web =>
                web.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                    new Dictionary<string, string?> { ["ECHO_ACCOUNT_DB"] = path })));
            using var alice = factory.CreateClient();
            using var bob = factory.CreateClient();
            Assert.Equal(HttpStatusCode.OK, (await alice.PostAsJsonAsync("/v1/register",
                new Credentials("alice@example.com", "a-long-password-123", "Alice"))).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await bob.PostAsJsonAsync("/v1/register",
                new Credentials("bob@example.com", "a-long-password-123", "Bob"))).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await alice.GetAsync("/v1/runs")).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await alice.PostAsJsonAsync("/v1/login",
                new Login("alice@example.com", "wrong"))).StatusCode);
            var a = await (await alice.PostAsJsonAsync("/v1/login", new Login("alice@example.com", "a-long-password-123")))
                .Content.ReadFromJsonAsync<Session>();
            var b = await (await bob.PostAsJsonAsync("/v1/login", new Login("bob@example.com", "a-long-password-123")))
                .Content.ReadFromJsonAsync<Session>();
            alice.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", a!.Token);
            bob.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", b!.Token);
            var input = new RunInput(Guid.NewGuid().ToString(), "Pine", false, 75, 850, 60, 120,
                "180 Indy +120", DateTimeOffset.UtcNow.ToString("O"), "glacier-pass", 2, 5);
            Assert.Equal(HttpStatusCode.BadRequest, (await alice.PostAsJsonAsync("/v1/runs", input with { RulesVersion = 4 })).StatusCode);
            var first = await (await alice.PostAsJsonAsync("/v1/runs", input)).Content.ReadFromJsonAsync<SavedRun>();
            var retry = await (await alice.PostAsJsonAsync("/v1/runs", input with { Score = 5000 })).Content.ReadFromJsonAsync<SavedRun>();
            Assert.Equal(first!.Id, retry!.Id);
            Assert.Equal(120, retry.Score);
            Assert.Single((await alice.GetFromJsonAsync<SavedRun[]>("/v1/runs"))!);
            var bests = (await alice.GetFromJsonAsync<RunBests[]>("/v1/bests"))!;
            Assert.Equal(120, bests[0].HighestScore!.Score);
            Assert.Empty((await bob.GetFromJsonAsync<SavedRun[]>("/v1/runs"))!);
            Assert.All((await bob.GetFromJsonAsync<RunBests[]>("/v1/bests"))!, best => Assert.Null(best.Fastest));
            Assert.Equal(HttpStatusCode.NotFound, (await bob.GetAsync($"/v1/runs/{first.Id}")).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await alice.PutAsJsonAsync("/v1/me", new ProfileUpdate("x"))).StatusCode);
            var profile = await (await alice.PutAsJsonAsync("/v1/me", new ProfileUpdate("Updated Alice"))).Content.ReadFromJsonAsync<Account>();
            Assert.Equal("Updated Alice", profile!.DisplayName);
            Assert.Equal("Bob", (await bob.GetFromJsonAsync<Account>("/v1/me"))!.DisplayName);
            Assert.Equal(HttpStatusCode.NoContent, (await alice.PostAsync("/v1/logout", null)).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await alice.GetAsync("/v1/runs")).StatusCode);
            var relogin = await (await alice.PostAsJsonAsync("/v1/login", new Login("alice@example.com", "a-long-password-123")))
                .Content.ReadFromJsonAsync<Session>();
            alice.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", relogin!.Token);
            Assert.Equal("Updated Alice", (await alice.GetFromJsonAsync<Account>("/v1/me"))!.DisplayName);
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }
}
