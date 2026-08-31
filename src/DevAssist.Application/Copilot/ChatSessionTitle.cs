using System.Text.RegularExpressions;

namespace DevAssist.Application.Copilot;

public static class ChatSessionTitle
{
    public const int MaxLength = 200;
    private const int PreferredLength = 60;

    public static string FromFirstQuestion(string question)
    {
        var normalized = Regex.Replace(question.Trim(), @"\s+", " ");
        if (string.IsNullOrWhiteSpace(normalized))
            return "New chat";

        if (normalized.Length <= PreferredLength)
            return TrimToMax(normalized);

        var truncated = normalized[..PreferredLength];
        var lastSpace = truncated.LastIndexOf(' ');
        if (lastSpace > PreferredLength / 2)
            truncated = truncated[..lastSpace];

        return TrimToMax(truncated.TrimEnd() + "…");
    }

    private static string TrimToMax(string title) =>
        title.Length <= MaxLength ? title : title[..MaxLength].TrimEnd();
}
