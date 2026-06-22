using System.Text.Json.Serialization;

namespace PRM.Application.DTOs.Common;

public class ApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<ValidationErrorDto>? Errors { get; set; }

    public ApiResponse() { }

    public ApiResponse(bool success, string message, List<ValidationErrorDto>? errors = null)
    {
        Success = success;
        Message = message;
        Errors = errors;
    }
}

public class ApiResponse<T> : ApiResponse
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public T? Data { get; set; }

    public ApiResponse() : base() { }

    public ApiResponse(bool success, string message, T? data = default, List<ValidationErrorDto>? errors = null)
        : base(success, message, errors)
    {
        Data = data;
    }
}

public class ValidationErrorDto
{
    public string Field { get; set; }
    public string Message { get; set; }

    public ValidationErrorDto(string field, string message)
    {
        Field = field;
        Message = message;
    }
}
