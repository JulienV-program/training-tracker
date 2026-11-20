using System.Net;

namespace MyTracker.Domain.Exceptions;

/// <summary>Échec d'appel à un service externe (Strava, Ollama) : indisponible, refus, réponse invalide.</summary>
public sealed class ExternalServiceException(
    string serviceName,
    string message,
    HttpStatusCode? statusCode = null,
    Exception? innerException = null)
    : TrainingTrackerException($"{serviceName} : {message}", innerException)
{
    public string ServiceName { get; } = serviceName;
    public HttpStatusCode? StatusCode { get; } = statusCode;
}
