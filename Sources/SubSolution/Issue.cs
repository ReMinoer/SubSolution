using System;

namespace SubSolution
{
    public class Issue
    {
        public IssueLevel Level { get; }
        public string Message { get; }
        public Exception? Exception { get; }

        public Issue(IssueLevel level, string message, Exception? exception = null)
        {
            Level = level;
            Message = message;
            Exception = exception;
        }
    }
}