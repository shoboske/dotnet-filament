namespace Fila.Tests;

internal static class TestAuth
{
    /// <summary>Logs into a panel with the demo app's built-in credential check (see
    /// samples/Demo/Program.cs). The client's cookie container then carries the resulting auth
    /// cookie on every subsequent request made with the same HttpClient. Each panel registers
    /// its own cookie scheme, so logging into /admin does not sign you into /staff.</summary>
    public static async Task LoginAsync(
        HttpClient client, string username = "admin", string password = "admin", string panel = "admin")
    {
        var response = await client.PostAsync($"/{panel}/login", new FormUrlEncodedContent(
        [
            new("username", username),
            new("password", password),
        ]));

        response.EnsureSuccessStatusCode();
    }
}
