namespace Fila.Tests;

internal static class TestAuth
{
    /// <summary>Logs into the /admin panel with the demo app's built-in admin/admin credential
    /// check (see samples/Demo/Program.cs). The client's cookie container then carries the
    /// resulting auth cookie on every subsequent request made with the same HttpClient.</summary>
    public static async Task LoginAsync(HttpClient client, string username = "admin", string password = "admin")
    {
        var response = await client.PostAsync("/admin/login", new FormUrlEncodedContent(
        [
            new("username", username),
            new("password", password),
        ]));

        response.EnsureSuccessStatusCode();
    }
}
