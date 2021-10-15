namespace SubSolution.CommandLine
{
    public enum ErrorCode
    {
        // Results
        Success = 0,
        NotValidated = 10,

        // Errors
        FatalException = 100,
        FailParseCommandLine = 110,
        FileNotFound = 120,

        FailReadSolution = 200,
        FailWriteSolution = 201,
        FailBuildSolution = 210,
        FailInterpretSolution = 211,
        FailUpdateSolution = 212
    }
}