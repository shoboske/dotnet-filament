using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Fila.Tests;

public sealed class LoginTests(DemoAppFactory factory) : IClassFixture<DemoAppFactory>
{
    [Fact]
    public async Task ValidCredentials_SignsInAndRedirectsIntoThePanel()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.PostAsync("/admin/login", new FormUrlEncodedContent(
        [
            new("username", "admin"),
            new("password", "admin"),
        ]));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/admin", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task InvalidCredentials_RedirectsBackToLoginWithError()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.PostAsync("/admin/login", new FormUrlEncodedContent(
        [
            new("username", "admin"),
            new("password", "definitely-wrong"),
        ]));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/admin/login?error=true", response.Headers.Location?.OriginalString);
    }
}
