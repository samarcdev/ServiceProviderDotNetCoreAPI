using APIServiceManagement.API.Models;
using APIServiceManagement.Domain.Exceptions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace APIServiceManagement.API.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;
        private readonly IWebHostEnvironment _environment;

        public ExceptionHandlingMiddleware(
            RequestDelegate next, 
            ILogger<ExceptionHandlingMiddleware> logger,
            IWebHostEnvironment environment)
        {
            _next = next;
            _logger = logger;
            _environment = environment;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred. Request Path: {Path}", context.Request.Path);
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var (statusCode, message) = GetStatusCodeAndMessage(exception);
            context.Response.StatusCode = (int)statusCode;

            var response = BuildExceptionResponse(exception, statusCode, message);

            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = _environment.IsDevelopment(),
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(response, jsonOptions);
            await context.Response.WriteAsync(json);
        }

        private static (HttpStatusCode statusCode, string message) GetStatusCodeAndMessage(Exception exception)
        {
            return exception switch
            {
                ArgumentException argEx => (HttpStatusCode.BadRequest, argEx.Message),
                InvalidOperationException => (HttpStatusCode.BadRequest, exception.Message),
                DomainException => (HttpStatusCode.BadRequest, exception.Message),
                UnauthorizedAccessException => (HttpStatusCode.Unauthorized, exception.Message),
                KeyNotFoundException => (HttpStatusCode.NotFound, exception.Message),
                NotImplementedException => (HttpStatusCode.NotImplemented, exception.Message),
                TimeoutException => (HttpStatusCode.RequestTimeout, exception.Message),
                _ => (HttpStatusCode.InternalServerError, "An error occurred while processing your request.")
            };
        }

        private ExceptionResponse BuildExceptionResponse(Exception exception, HttpStatusCode statusCode, string message)
        {
            var response = new ExceptionResponse
            {
                StatusCode = (int)statusCode,
                Message = message,
                ExceptionType = exception.GetType().FullName ?? exception.GetType().Name,
                Details = exception.Message,
                StackTrace = _environment.IsDevelopment() ? exception.StackTrace : null,
                Source = exception.Source,
                HelpLink = exception.HelpLink,
                StackFrames = ParseStackTrace(exception.StackTrace),
                InnerExceptions = ExtractInnerExceptions(exception)
            };

            // Add additional data from exception if available
            if (exception.Data != null && exception.Data.Count > 0)
            {
                foreach (var key in exception.Data.Keys)
                {
                    if (key != null)
                    {
                        response.AdditionalData[key.ToString()!] = exception.Data[key] ?? string.Empty;
                    }
                }
            }

            return response;
        }

        private static List<ExceptionDetail> ExtractInnerExceptions(Exception exception)
        {
            var innerExceptions = new List<ExceptionDetail>();
            var currentException = exception.InnerException;

            while (currentException != null)
            {
                innerExceptions.Add(new ExceptionDetail
                {
                    ExceptionType = currentException.GetType().FullName ?? currentException.GetType().Name,
                    Message = currentException.Message,
                    StackTrace = currentException.StackTrace,
                    Source = currentException.Source,
                    StackFrames = ParseStackTrace(currentException.StackTrace)
                });

                currentException = currentException.InnerException;
            }

            return innerExceptions;
        }

        private static List<StackFrameInfo> ParseStackTrace(string? stackTrace)
        {
            if (string.IsNullOrWhiteSpace(stackTrace))
            {
                return new List<StackFrameInfo>();
            }

            var frames = new List<StackFrameInfo>();
            var lines = stackTrace.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            // Pattern to match stack trace lines like:
            //    at Namespace.Class.Method() in C:\path\to\file.cs:line 123
            //    at Namespace.Class.Method() in C:\path\to\file.cs:line 123:column 45
            var framePattern = new Regex(
                @"^\s*at\s+(?<method>.*?)\s+(?:in\s+(?<file>.*?):line\s+(?<line>\d+)(?::column\s+(?<column>\d+))?)?$",
                RegexOptions.Compiled | RegexOptions.IgnoreCase
            );

            foreach (var line in lines)
            {
                var match = framePattern.Match(line.Trim());
                if (match.Success)
                {
                    var frame = new StackFrameInfo
                    {
                        FullFrame = line.Trim(),
                        MethodName = match.Groups["method"].Value,
                        FileName = match.Groups["file"].Success ? match.Groups["file"].Value : null,
                        LineNumber = match.Groups["line"].Success && int.TryParse(match.Groups["line"].Value, out var lineNum) 
                            ? lineNum 
                            : null,
                        ColumnNumber = match.Groups["column"].Success && int.TryParse(match.Groups["column"].Value, out var colNum) 
                            ? colNum 
                            : null
                    };

                    frames.Add(frame);
                }
                else if (!string.IsNullOrWhiteSpace(line))
                {
                    // If pattern doesn't match, still include the frame for completeness
                    frames.Add(new StackFrameInfo
                    {
                        FullFrame = line.Trim()
                    });
                }
            }

            return frames;
        }
    }
}