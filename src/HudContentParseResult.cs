namespace FloatingHud;

public sealed class HudContentParseResult
{
    public HudContentParseResult(HudContent content, List<string> errors)
    {
        Content = content;
        Errors = errors;
    }

    public HudContent Content { get; }

    public List<string> Errors { get; }
}
