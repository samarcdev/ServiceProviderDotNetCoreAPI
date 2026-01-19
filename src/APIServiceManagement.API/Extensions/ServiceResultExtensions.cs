using APIServiceManagement.Application.DTOs.Responses;
using Microsoft.AspNetCore.Mvc;

namespace APIServiceManagement.API.Extensions;

public static class ServiceResultExtensions
{
    public static IActionResult ToActionResult(this ServiceResult result)
    {
        return new ObjectResult(result.Payload) { StatusCode = (int)result.StatusCode };
    }
}
