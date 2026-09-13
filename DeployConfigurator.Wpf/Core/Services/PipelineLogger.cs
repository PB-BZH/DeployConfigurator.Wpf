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
║  Nom de fichier : PipelineLogger.cs												     
╚════════════════════════════════════════════════════════════════════════════════╝
*/
using System.Diagnostics;
using System.Text;

namespace DeployConfigurator.Wpf.Core.Services;

public class PipelineLogger {
  private readonly List<string> _lines = [];

  public IReadOnlyList<string> Lines => _lines;

  public void Info(string message) {
    Add("INFO",message);
  }

  public void Warning(string message) {
    Add("WARN",message);
  }

  public void Error(string message) {
    Add("ERROR",message);
  }

  public void Success(string message) {
    Add("SUCCESS",message);
  }

  private void Add(string level,string message) {
    string line =
        $"[{level}] {message}";

    _lines.Add(line);

    Debug.WriteLine(line);
  }

  public string GetText() {
    StringBuilder sb = new();

    foreach (string line in _lines)
      sb.AppendLine(line);

    return sb.ToString();
  }
  public void AddLines(IEnumerable<string> lines) {
    foreach (string line in lines)
      _lines.Add(line);
  }
}