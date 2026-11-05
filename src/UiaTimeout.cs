namespace WinAppMCP;

/// <summary>
/// Reads WINAPP_UIA_TIMEOUT_S. Some apps answer UI Automation slowly while staying alive, and the default
/// timeouts then fail a lookup with 0x80131505. Not set means: keep the defaults.
/// </summary>
public static class UiaTimeout
{
    public const string VariableName = "WINAPP_UIA_TIMEOUT_S";
    public const int MaxSeconds = 3600;

    /// Returns true with the seconds for a valid value. Returns false for an empty value (error is null)
    /// and for a bad one (error says why).
    public static bool TryParse(string? raw, out int seconds, out string? error)
    {
        seconds = 0;
        error = null;
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        if (!int.TryParse(raw.Trim(), out int parsed) || parsed < 1 || parsed > MaxSeconds)
        {
            error = $"{VariableName}='{raw}' ignored: expected whole seconds from 1 to {MaxSeconds}";
            return false;
        }

        seconds = parsed;
        return true;
    }
}
