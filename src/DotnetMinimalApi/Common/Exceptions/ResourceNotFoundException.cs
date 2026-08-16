namespace DotnetMinimalApi.Common.Exceptions;

/// <summary>
/// Exception thrown when an entity or resource is not found.
/// </summary>
public class ResourceNotFoundException : Exception
{
    public string ResourceName { get; }
    public object ResourceKey { get; }

    public ResourceNotFoundException(string resourceName, object resourceKey)
        : base($"{resourceName} with key '{resourceKey}' was not found.")
    {
        ResourceName = resourceName;
        ResourceKey = resourceKey;
    }
}
