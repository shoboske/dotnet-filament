namespace Demo;

public static class LoginPage
{
    public static string Render(bool error) => $$"""
        <!DOCTYPE html>
        <html lang="en">
        <head>
            <meta charset="utf-8" />
            <meta name="viewport" content="width=device-width, initial-scale=1" />
            <title>Sign in · Demo Admin</title>
            <link rel="stylesheet" href="/_content/Fila/fila/fila.css" />
        </head>
        <body class="fi-body">
            <div class="fi-login">
                <div class="fi-login-card">
                    <h1 class="fi-login-heading">Demo Admin</h1>
                    {{(error ? """<p class="fi-login-error">Invalid username or password.</p>""" : "")}}
                    <form method="post" action="/login">
                        <label class="fi-login-label" for="username">Username</label>
                        <input class="fi-input fi-login-input" id="username" name="username" autocomplete="username" required />
                        <label class="fi-login-label" for="password">Password</label>
                        <input class="fi-input fi-login-input" id="password" name="password" type="password" autocomplete="current-password" required />
                        <button class="fi-btn fi-btn-primary fi-login-submit" type="submit">Sign in</button>
                    </form>
                    <p class="fi-login-hint">admin / admin</p>
                </div>
            </div>
        </body>
        </html>
        """;
}
