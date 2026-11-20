using System.Globalization;
using CsvHelper;
using MyTracker.Domain.Interfaces;

namespace MyTracker.Infrastructure.Csv;

/// <summary>Sérialisation CSV (CsvHelper, culture invariante : point décimal).</summary>
public class CsvExportService : ICsvExportService
{
    public byte[] GetCsvBytes<T>(IEnumerable<T> records)
    {
        using var memoryStream = new MemoryStream();
        using var writer = new StreamWriter(memoryStream);
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

        csv.WriteRecords(records);
        writer.Flush();
        return memoryStream.ToArray();
    }
}
