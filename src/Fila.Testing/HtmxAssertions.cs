using System.Text.Json;

namespace Fila.Testing;

/// <summary>Thrown by the assertions below on failure. Deliberately not tied to xUnit/NUnit/etc
/// — an unhandled exception fails a test under any runner, and this keeps Fila.Testing usable
/// from a consumer's test suite regardless of which framework they've chosen.</summary>
public sealed class FilaAssertionException(string message) : Exception(message);

/// <summary>Assertions against Fila's htmx wire format (see Fila.Panels' IHxTriggerDataReader),
/// so a resource author's own tests — and Fila's own — don't have to hand-parse the HX-Trigger
/// header. Mirrors what Filament's per-package Testing traits (packages/*/src/Testing) give
/// consumers, adapted to Fila's server-rendered-HTML-over-htmx model instead of Livewire.</summary>
public static class HtmxAssertions
{
    /// <summary>Asserts the response carries Fila's "fila-notify" HX-Trigger event with the given
    /// title and color (e.g. "Saved"/"success", "Deleted"/"danger").</summary>
    public static void AssertNotificationTriggered(this HttpResponseMessage response, string title, string color)
    {
        var trigger = GetTrigger(response);

        if (!trigger.TryGetProperty("fila-notify", out var notify))
            throw new FilaAssertionException("Expected an HX-Trigger 'fila-notify' event, but none was present.");

        var actualTitle = notify.GetProperty("title").GetString();
        var actualColor = notify.GetProperty("color").GetString();

        if (actualTitle != title || actualColor != color)
        {
            throw new FilaAssertionException(
                $"Expected notification (\"{title}\", \"{color}\"), got (\"{actualTitle}\", \"{actualColor}\").");
        }
    }

    /// <summary>Asserts the response carries Fila's "fila-modal-close" HX-Trigger event.</summary>
    public static void AssertModalClosed(this HttpResponseMessage response)
    {
        var trigger = GetTrigger(response);

        if (!trigger.TryGetProperty("fila-modal-close", out var closed) || !closed.GetBoolean())
            throw new FilaAssertionException("Expected the HX-Trigger header to include 'fila-modal-close': true.");
    }

    private static JsonElement GetTrigger(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("HX-Trigger", out var values))
            throw new FilaAssertionException("Response has no HX-Trigger header.");

        return JsonDocument.Parse(values.First()).RootElement;
    }
}
