namespace MyTracker.Domain.Configurations;

public class OllamaSettings
{
    public string BaseUrl { get; set; } = "http://localhost:11434";
    public string Model { get; set; } = "qwen2.5:7b-instruct";
    public int TimeoutSeconds { get; set; } = 120;
}
