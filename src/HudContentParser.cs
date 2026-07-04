using System.Text.Json;
namespace FloatingHud;

public static class HudContentParser
{
    public static HudContentParseResult ParseCommandOutput(string output, HudContent currentContent)
    {
        List<string> errors = new();
        HudContent content = currentContent;
        string trimmedOutput = output.Trim();
        if (string.IsNullOrEmpty(trimmedOutput))
        {
            errors.Add("Standard output is empty. Expected a JSON object, at least {}.");
            return new HudContentParseResult(content, errors);
        }

        content = TryParseJsonOutput(trimmedOutput, currentContent, errors);

        return new HudContentParseResult(content, errors);
    }

    private static HudContent TryParseJsonOutput(string output, HudContent currentContent, List<string> errors)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(output);
            JsonElement root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                errors.Add($"Standard output root is {root.ValueKind}. Expected a JSON object.");
                return currentContent;
            }

            HudCommandOutputDto dto = HudCommandOutputDto.FromJsonObject(root);
            HudCommandOutputValidationResult validationResult = HudCommandOutputValidator.Validate(dto);
            errors.AddRange(validationResult.Errors);
            return HudContentMerger.Merge(currentContent, validationResult.Patch);
        }
        catch (JsonException exception)
        {
            errors.Add($"Standard output is not valid JSON: {exception.Message}");
        }

        return currentContent;
    }
}
