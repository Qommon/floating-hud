namespace FloatingHud;

internal sealed record HudCommandOutputValidationResult(
    HudCommandOutputPatch Patch,
    List<string> Errors);
