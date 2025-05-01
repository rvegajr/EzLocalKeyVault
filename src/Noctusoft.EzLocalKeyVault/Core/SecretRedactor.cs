using System.Text.RegularExpressions;

namespace Noctusoft.EzLocalKeyVault.Core;

/// <summary>
/// Helper class for redacting sensitive information in logs.
/// </summary>
public static class SecretRedactor
{
    private const string RedactionReplacement = "***";

    /// <summary>
    /// Determines if a key contains any of the specified redaction patterns.
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <param name="redactionPatterns">The patterns to check for.</param>
    /// <returns>True if the key contains any of the redaction patterns, otherwise false.</returns>
    public static bool IsSecret(string key, IEnumerable<string> redactionPatterns)
    {
        if (string.IsNullOrWhiteSpace(key))
            return false;

        return redactionPatterns.Any(pattern =>
            Regex.IsMatch(key, pattern, RegexOptions.IgnoreCase));
    }

    /// <summary>
    /// Redacts a value if the key contains any of the specified redaction patterns.
    /// </summary>
    /// <param name="key">The key associated with the value.</param>
    /// <param name="value">The value to potentially redact.</param>
    /// <param name="redactionPatterns">The patterns to check for in the key.</param>
    /// <returns>The redacted value if the key contains any of the redaction patterns, otherwise the original value.</returns>
    public static string RedactIfSecret(string key, string value, IEnumerable<string> redactionPatterns)
    {
        return IsSecret(key, redactionPatterns) ? RedactionReplacement : value;
    }
}