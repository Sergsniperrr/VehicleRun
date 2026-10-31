namespace _Project.Features.Commands
{
    public readonly struct CommandResult
    {
        public CommandResult(FailureReason failureReason)
        {
            FailureReason = failureReason;
        }

        public FailureReason FailureReason { get; }
        public bool IsSuccess => FailureReason == FailureReason.None;
    }
}
