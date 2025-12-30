namespace ArchoCybo.Domain.Common;

public class RepositoryResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;

    public static RepositoryResult Ok(string message)
        => new() { Success = true, Message = message };

    public static RepositoryResult Fail(string message)
        => new() { Success = false, Message = message };
}

public class RepositoryResult<T> : RepositoryResult
{
    public T? Data { get; init; }

    public static RepositoryResult<T> Ok(T data, string message)
        => new() { Success = true, Data = data, Message = message };

    public new static RepositoryResult<T> Fail(string message)
        => new() { Success = false, Message = message };
}
