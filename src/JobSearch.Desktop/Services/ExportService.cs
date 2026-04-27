using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using JobSearch.Desktop.Models;

namespace JobSearch.Desktop.Services;

public sealed class ExportService
{
    public async Task ExportToCsvAsync(string filePath, IEnumerable<RecommendationItem> items)
    {
        var sb = new StringBuilder();
        
        // UTF-8 BOM for Excel compatibility
        sb.Append('\uFEFF');
        
        // Header
        sb.AppendLine("기업명,공고제목,연령대,플랫폼,지역,경력,고용형태,산업군,연봉(백만원),적합도,공고URL");
        
        foreach (var item in items)
        {
            sb.AppendLine($"{EscapeCsv(item.CompanyName)},{EscapeCsv(item.JobTitle)},{EscapeCsv(item.AgeGroupLabel)},{EscapeCsv(item.Platform)},{EscapeCsv(item.Region)},{EscapeCsv(item.ExperienceLevel)},{EscapeCsv(item.EmploymentType)},{EscapeCsv(item.Industry)},{item.SalaryMillionKrw},{item.SuitabilityScore:P0},{EscapeCsv(item.JobUrl)}");
        }
        
        await File.WriteAllTextAsync(filePath, sb.ToString(), Encoding.UTF8).ConfigureAwait(false);
    }

    private static string EscapeCsv(string? value)
    {
        if (value == null) return string.Empty;
        if (value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
        return value;
    }
}
