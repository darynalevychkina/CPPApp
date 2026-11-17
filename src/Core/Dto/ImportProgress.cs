namespace Core.Dto;

/// <summary>Import progress report. Total is null when it is not known in advance (streaming import).</summary>
public sealed record ImportProgress(int Processed, int? Total)
{
    public int? Percent => Total is > 0 ? Processed * 100 / Total : null;
}
