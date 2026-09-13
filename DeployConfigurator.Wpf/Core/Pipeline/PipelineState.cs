namespace DeployConfigurator.Wpf.Core.Pipeline;

public enum PipelineState {
  Pending,
  Running,
  Success,
  Warning,
  Error,
  Cancelled
}