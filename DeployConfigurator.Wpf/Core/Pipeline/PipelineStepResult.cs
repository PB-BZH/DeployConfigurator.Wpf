namespace DeployConfigurator.Wpf.Core.Pipeline;

public sealed class PipelineStepResult {
  public bool Success { get; init; }

  public bool Warning { get; init; }

  public string Message { get; init; } = "";

  public Exception? Exception { get; init; }

  public static PipelineStepResult Ok(string message = "") {
    return new PipelineStepResult {
      Success = true,
      Message = message
    };
  }

  public static PipelineStepResult Warn(string message) {
    return new PipelineStepResult {
      Success = true,
      Warning = true,
      Message = message
    };
  }

  public static PipelineStepResult Fail(string message,Exception? exception = null) {
    return new PipelineStepResult {
      Success = false,
      Message = message,
      Exception = exception
    };
  }
}