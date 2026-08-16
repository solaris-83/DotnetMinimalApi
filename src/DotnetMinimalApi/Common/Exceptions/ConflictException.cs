namespace DotnetMinimalApi.Common.Exceptions;

/// <summary>
/// Exception thrown when a conflict occurs (e.g. duplicate unique key).
/// </summary>
public class ConflictException : Exception
{
    public ConflictException(string message) : base(message)
    {
    }
}
