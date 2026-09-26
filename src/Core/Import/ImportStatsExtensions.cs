using Core.Dto;

namespace Core.Import;

public static class ImportStatsExtensions
{
    public static ImportStats GetStats<T>(this ImportResult<T> result)
    {
        int accepted = result.Items.Count;
        int skipped = result.Errors.Count;
        int total = accepted + skipped;
        double errorRate = total == 0 ? 0 : skipped * 100.0 / total;

        return new ImportStats(total, accepted, skipped, errorRate);
    }
}