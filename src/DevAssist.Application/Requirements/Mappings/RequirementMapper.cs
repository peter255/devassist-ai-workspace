using System.Text.Json;
using DevAssist.Contracts.Requirements;
using DevAssist.Domain.Entities;

namespace DevAssist.Application.Requirements.Mappings;

public static class RequirementMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static BreakdownRequirementResponse ToBreakdownResponse(RequirementAnalysis analysis) =>
        new(
            analysis.Id,
            analysis.FunctionalSummary,
            FromJson(analysis.BackendTasksJson),
            FromJson(analysis.FrontendTasksJson),
            FromJson(analysis.TestingChecklistJson),
            FromJson(analysis.RisksJson),
            FromJson(analysis.AssumptionsJson),
            FromJson(analysis.AcceptanceCriteriaJson),
            analysis.CreatedAt);

    public static RequirementAnalysisListItemDto ToListItem(RequirementAnalysis analysis) =>
        new(analysis.Id, analysis.FunctionalSummary, analysis.CreatedAt);

    public static string ToJson(IReadOnlyList<string> items) =>
        JsonSerializer.Serialize(items);

    private static IReadOnlyList<string> FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        return JsonSerializer.Deserialize<List<string>>(json, JsonOptions) ?? [];
    }
}
