namespace FloatingHud;

public readonly record struct CommandResult(
    int ExitCode,
    string StandardOutput,
    string StandardError,
    bool TimedOut);
