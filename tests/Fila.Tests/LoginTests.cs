using System.Net;
using AngleSharp.Html.Parser;
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

    [Fact]
    public async Task LoginPage_HasTheSignInHeadingAndAPasswordRevealToggle()
    {
        using var client = factory.CreateClient();

        var html = await client.GetStringAsync("/admin/login");
        var document = await new HtmlParser().ParseDocumentAsync(html);

        Assert.Equal("Sign in", document.QuerySelector(".fi-login-heading")?.TextContent.Trim());
        Assert.NotNull(document.QuerySelector("button[aria-label='Show password']"));
        Assert.NotNull(document.QuerySelector("button[aria-label='Hide password']"));
        Assert.NotNull(document.QuerySelector("input[name='remember'][type='checkbox']"));
        Assert.NotNull(document.QuerySelector("label[for='username'] .fi-required-mark"));
    }

    [Fact]
    public async Task RememberMe_Checked_IssuesAPersistentCookie()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.PostAsync("/admin/login", new FormUrlEncodedContent(
        [
            new("username", "admin"),
            new("password", "admin"),
            new("remember", "true"),
        ]));

        var setCookie = response.Headers.TryGetValues("Set-Cookie", out var values) ? string.Join(' ', values) : "";
        Assert.Contains("expires=", setCookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RememberMe_Unchecked_IssuesASessionCookie()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.PostAsync("/admin/login", new FormUrlEncodedContent(
        [
            new("username", "admin"),
            new("password", "admin"),
        ]));

        var setCookie = response.Headers.TryGetValues("Set-Cookie", out var values) ? string.Join(' ', values) : "";
        Assert.DoesNotContain("expires=", setCookie, StringComparison.OrdinalIgnoreCase);
    }
}
