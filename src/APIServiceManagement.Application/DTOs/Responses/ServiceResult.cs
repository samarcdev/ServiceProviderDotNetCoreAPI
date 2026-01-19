using System.Net;

namespace APIServiceManagement.Application.DTOs.Responses;

public sealed class ServiceResult
{
    public HttpStatusCode StatusCode { get; init; } = HttpStatusCode.OK;
    public object? Payload { get; init; }

    public static ServiceResult Ok(object payload)
    {
        return new ServiceResult { StatusCode = HttpStatusCode.OK, Payload = payload };
    }

    public static ServiceResult Created(object payload)
    {
        return new ServiceResult { StatusCode = HttpStatusCode.Created, Payload = payload };
    }

    public static ServiceResult BadRequest(object payload)
    {
        return new ServiceResult { StatusCode = HttpStatusCode.BadRequest, Payload = payload };
    }

    public static ServiceResult BadRequest(string message)
    {
        return BadRequest(new MessageResponse { Message = message });
    }

    public static ServiceResult Unauthorized()
    {
        return new ServiceResult { StatusCode = HttpStatusCode.Unauthorized };
    }

    public static ServiceResult Forbidden()
    {
        return new ServiceResult { StatusCode = HttpStatusCode.Forbidden };
    }

    public static ServiceResult Forbidden(string message)
    {
        return new ServiceResult
        {
            StatusCode = HttpStatusCode.Forbidden,
            Payload = new MessageResponse { Message = message }
        };
    }

    public static ServiceResult NotFound(object payload)
    {
        return new ServiceResult { StatusCode = HttpStatusCode.NotFound, Payload = payload };
    }
}
