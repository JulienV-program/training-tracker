namespace MyTracker.Domain.Interfaces;

public interface ICsvExportService
{
    byte[] GetCsvBytes<T>(IEnumerable<T> records);
}
