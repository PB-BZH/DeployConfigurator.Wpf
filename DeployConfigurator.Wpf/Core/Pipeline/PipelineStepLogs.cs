namespace DeployConfigurator.Wpf.Core.Pipeline;

public sealed class PipelineStepLog {
  public string Name { get; init; } = "";
  public PipelineState State { get; init; }
  public DateTime StartTime { get; init; }
  public DateTime EndTime { get; init; }
  public string Message { get; init; } = "";
  public TimeSpan Duration => EndTime - StartTime;
}