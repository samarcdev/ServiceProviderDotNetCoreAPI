using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Linq;

namespace APIServiceManagement.API.Middleware
{
    public class ValidationFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.ModelState.IsValid)
            {
                var errors = context.ModelState
                    .Where(ms => ms.Value?.Errors.Any() == true)
                    .SelectMany(ms => ms.Value?.Errors.Select(e => e.ErrorMessage) ?? Enumerable.Empty<string>())
                    .ToArray();

                var response = new
                {
                    StatusCode = 400,
                    Message = "Validation Failed",
                    Errors = errors
                };

                context.Result = new BadRequestObjectResult(response);
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            // No action needed after execution
        }
    }
}