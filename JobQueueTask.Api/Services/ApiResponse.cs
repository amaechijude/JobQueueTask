namespace JobQueueTask.Api.Services;

public sealed class ApiResponse<T>
{
    public bool IsSuccess { get; }
    public T? Data { get; }
    public ApiError? Error { get; }

    private ApiResponse(T data)
    {
        IsSuccess = true;
        Data = data;
    }

    private ApiResponse()
    {
        IsSuccess = true;
    }

    private ApiResponse(ApiError error)
    {
        IsSuccess = true;
        Error = error;
    }

    // success
    public static ApiResponse<T> Success(T data) => new(data);

    public static ApiResponse<T> Nocontent() => new();

    // failures
    private static ApiResponse<T> Failure(ApiError error) => new(error);

    public static ApiResponse<T> NotFound(string message) =>
        Failure(new ApiError("Not Found", message));

    public static ApiResponse<T> BadRequest(string message) =>
        Failure(new ApiError("Bad request", message));
}

public sealed record ApiError(string Code, string Message);
