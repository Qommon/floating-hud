using System.Globalization;
using System.IO;
using System.Text;

namespace FloatingHud;

public static class RefreshErrorLogger
{
    public static void Clear()
    {
        try
        {
            string logDirectory = GetLogDirectory();
            if (!Directory.Exists(logDirectory))
            {
                return;
            }

            foreach (string logPath in Directory.EnumerateFiles(logDirectory, "*.log"))
            {
                File.Delete(logPath);
            }
        }
        catch
        {
        }
    }

    public static void Write(string commandLine, CommandResult? result, IEnumerable<string> errors)
    {
        try
        {
            string logDirectory = GetLogDirectory();
            Directory.CreateDirectory(logDirectory);

            string timestamp = DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture);
            string logPath = CreateUniqueLogPath(logDirectory, timestamp);
            File.WriteAllText(logPath, CreateLogContent(commandLine, result, errors), Encoding.UTF8);
        }
        catch
        {
        }
    }

    private static string CreateUniqueLogPath(string logDirectory, string timestamp)
    {
        string logPath = Path.Combine(logDirectory, $"{timestamp}.log");
        if (!File.Exists(logPath))
        {
            return logPath;
        }

        for (int index = 1; index < 1000; index++)
        {
            logPath = Path.Combine(logDirectory, $"{timestamp}-{index}.log");
            if (!File.Exists(logPath))
            {
                return logPath;
            }
        }

        return Path.Combine(logDirectory, $"{timestamp}-{Guid.NewGuid():N}.log");
    }

    private static string GetLogDirectory()
    {
        return Path.Combine(HudSettingsStore.SettingsDirectory, "errors");
    }

    private static string CreateLogContent(string commandLine, CommandResult? result, IEnumerable<string> errors)
    {
        StringBuilder builder = new();
        builder.AppendLine("Command line:");
        builder.AppendLine(commandLine);
        builder.AppendLine();
        builder.AppendLine("Refresh:");
        builder.AppendLine(FormattableString.Invariant($"Time: {DateTimeOffset.Now:O}"));
        builder.AppendLine(FormattableString.Invariant($"Exit code: {(result is { } value ? value.ExitCode.ToString(CultureInfo.InvariantCulture) : "N/A")}"));
        builder.AppendLine(FormattableString.Invariant($"Timed out: {(result is { } timeoutValue ? timeoutValue.TimedOut : false)}"));
        builder.AppendLine();
        builder.AppendLine("Main process errors:");
        foreach (string error in errors)
        {
            builder.AppendLine(FormattableString.Invariant($"- {error}"));
        }

        builder.AppendLine();
        builder.AppendLine("Standard output:");
        builder.AppendLine(result?.StandardOutput ?? string.Empty);
        builder.AppendLine();
        builder.AppendLine("Standard error:");
        builder.AppendLine(result?.StandardError ?? string.Empty);
        return builder.ToString();
    }
}
