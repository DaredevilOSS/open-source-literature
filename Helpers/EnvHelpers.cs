namespace Extensions;

public static class EnvHelper
{
    public static string GetStringEnvOrDefault(string variableName, string defaultValue)
    {
        var value = Environment.GetEnvironmentVariable(variableName);
        return !string.IsNullOrEmpty(value) ? value : defaultValue;
    }

    public static int GetIntEnvOrDefault(string variableName, int defaultValue)
    {
        var success = int.TryParse(Environment.GetEnvironmentVariable(variableName), out var value);
        return success ? value : defaultValue;
    }
}