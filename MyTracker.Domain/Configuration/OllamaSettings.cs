using System.ComponentModel.DataAnnotations;

namespace MyTracker.Domain.Configurations;

public class OllamaSettings
{
    public const string SectionName = "Ollama";

    [Required, Url]
    public string BaseUrl { get; set; } = "http://localhost:11434";

    [Required(AllowEmptyStrings = false)]
    public string Model { get; set; } = "qwen2.5:7b-instruct";

    [Range(5, 1800)]
    public int TimeoutSeconds { get; set; } = 120;
}
