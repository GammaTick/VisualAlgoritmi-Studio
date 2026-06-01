namespace VisualAlgoritmi_Studio.Execution
{
    internal readonly struct UserCodeExecutionResult
    {
        public UserCodeExecutionStatus Status { get; }

        public int? ExitCode { get; }

        public string StandardOutput { get; }

        public string StandardError { get; }

        public string? FailureMessage { get; }

        public bool IsSuccess => Status == UserCodeExecutionStatus.Success;

        private UserCodeExecutionResult(
            UserCodeExecutionStatus status,
            int? exitCode,
            string standardOutput,
            string standardError,
            string? failureMessage)
        {
            Status = status;
            ExitCode = exitCode;
            StandardOutput = standardOutput;
            StandardError = standardError;
            FailureMessage = failureMessage;
        }

        public static UserCodeExecutionResult Success(
            int exitCode,
            string standardOutput,
            string standardError)
        {
            return new UserCodeExecutionResult(
                UserCodeExecutionStatus.Success,
                exitCode,
                standardOutput,
                standardError,
                null);
        }

        public static UserCodeExecutionResult RuntimeError(
            int exitCode,
            string standardOutput,
            string standardError)
        {
            return new UserCodeExecutionResult(
                UserCodeExecutionStatus.RuntimeError,
                exitCode,
                standardOutput,
                standardError,
                "User program exited with an error.");
        }

        public static UserCodeExecutionResult FailedToStart(string message)
        {
            return new UserCodeExecutionResult(
                UserCodeExecutionStatus.FailedToStart,
                null,
                string.Empty,
                string.Empty,
                message);
        }

        public static UserCodeExecutionResult Cancelled(
            string standardOutput,
            string standardError)
        {
            return new UserCodeExecutionResult(
                UserCodeExecutionStatus.Cancelled,
                null,
                standardOutput,
                standardError,
                "User program was stopped.");
        }
    }

    internal enum UserCodeExecutionStatus
    {
        Success,
        RuntimeError,
        FailedToStart,
        Cancelled
    }
}
