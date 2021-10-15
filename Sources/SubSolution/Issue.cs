namespace SubSolution
{
    public class Issue
    {
        public IssueLevel Level { get; }
        public string Message { get; }

        public Issue(IssueLevel level, string message)
        {
            Level = level;
            Message = message;
        }
    }
}