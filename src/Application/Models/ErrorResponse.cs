namespace Application.Models;

public class ErrorResponse
{
    /// <summary>
    /// A URI reference that identifies the problem type (e.g. https://httpstatuses.com/400).
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// A short, human-readable description of the problem.
    /// </summary>
    public string? Description { get; set; }
}
