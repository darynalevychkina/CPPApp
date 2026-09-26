namespace Core.Dto;

public sealed record ImportStats(int Total, int Accepted, int Skipped, double ErrorRatePercent)
{
    public override string ToString() =>
        $"Total: {Total} | Accepted: {Accepted} | Skipped: {Skipped} | Errors: {ErrorRatePercent:F1}%";
}