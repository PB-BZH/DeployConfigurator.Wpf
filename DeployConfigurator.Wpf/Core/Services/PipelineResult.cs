/*
╔════════════════════════════════════════════════════════════════════════════════╗
║                                                                                ║
║                      ───────────────────────────────────                       ║
║                        © Copyright PB-BZH Concept 2025                         ║
║                      ───────────────────────────────────                       ║
║                                                                                ║
║                 contact : mailto:patrick.bourges@univ-brest.fr                 ║
╚════════════════════════════════════════════════════════════════════════════════╝

╔════════════════════════════════════════════════════════════════════════════════╗
║  Auteur : Patrick Bourges - PB-BZH Concept                                     ║
║  Le 22/5/2026 - 21:34                                                          ║                                                          
╟────────────────────────────────────────────────────────────────────────────────║
║     Projet VS_Pro_2022 C# 7.3 : DeployConfigurator                             ║                                      
╟────────────────────────────────────────────────────────────────────────────────║
║     Version : 3.0.1                                                            ║
╟────────────────────────────────────────────────────────────────────────────────║
║                Visual Studio Professional 2026 - Insiders                      ║
║                ──────────────────────────────────────────                      ║
║  Langage     : C# 7.3                                                          ║
║  Technologie : .Net Framework 4.8.1                                            ║
║  Encodage    : utf-8 : Unicode - Pages de codes 1200                           ║
╟────────────────────────────────────────────────────────────────────────────────║
║  Nom de fichier : PipelineResult.cs												     
╚════════════════════════════════════════════════════════════════════════════════╝
*/
namespace DeployConfigurator.Wpf.Core.Services;

public class PipelineResult {
  public bool Success { get; set; }

  public string Message { get; set; } = "";

  public List<string> Logs { get; set; } = [];

  public static PipelineResult Ok(
      string message,
      List<string>? logs = null) {
    return new PipelineResult {
      Success = true,
      Message = message,
      Logs = logs ?? []
    };
  }

  public static PipelineResult Fail(
      string message,
      List<string>? logs = null) {
    return new PipelineResult {
      Success = false,
      Message = message,
      Logs = logs ?? []
    };
  }
}