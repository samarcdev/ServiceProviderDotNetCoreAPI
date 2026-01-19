using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace APIServiceManagement.API.Models
{
    public class ExceptionResponse
    {
        public int StatusCode { get; set; }
        public string Message { get; set; } = string.Empty;
        public string ExceptionType { get; set; } = string.Empty;
        public string? Details { get; set; }
        public string? StackTrace { get; set; }
        public List<ExceptionDetail> InnerExceptions { get; set; } = new();
        public List<StackFrameInfo> StackFrames { get; set; } = new();
        public string? Source { get; set; }
        public string? HelpLink { get; set; }
        public Dictionary<string, object> AdditionalData { get; set; } = new();
    }

    public class ExceptionDetail
    {
        public string ExceptionType { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? StackTrace { get; set; }
        public string? Source { get; set; }
        public List<StackFrameInfo> StackFrames { get; set; } = new();
    }

    public class StackFrameInfo
    {
        public string? FileName { get; set; }
        public string? MethodName { get; set; }
        public int? LineNumber { get; set; }
        public int? ColumnNumber { get; set; }
        public string? FullFrame { get; set; }
    }
}
