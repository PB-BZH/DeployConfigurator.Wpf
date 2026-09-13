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
║  Le 22/5/2026 - 21:33                                                          ║                                                          
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
║  Nom de fichier : SynchronousCommandsBuilder.cs												     
╚════════════════════════════════════════════════════════════════════════════════╝
*/
using System.Security;
using System.Text;
using DeployConfigurator.Wpf.Core.Models;

namespace DeployConfigurator.Wpf.Core.Builders;

public static class SynchronousCommandsBuilder {
  public static string GenerateFirstLogonCommands(IEnumerable<SynchronousCommandConfiguration> commands) {
    StringBuilder sb = new();

    foreach (var command in commands
                 .Where(x => x.Enabled)
                 .OrderBy(x => x.Order)) {
      sb.AppendLine($"\t\t\t\t<SynchronousCommand wcm:action=\"add\">");
      sb.AppendLine($"\t\t\t\t\t<Order>{command.Order}</Order>");
      sb.AppendLine($"\t\t\t\t\t<Description>{EscapeXml(command.Description)}</Description>");
      sb.AppendLine($"\t\t\t\t\t<CommandLine>{EscapeXml(command.CommandLine)}</CommandLine>");
      sb.AppendLine("\t\t\t\t</SynchronousCommand>");
      sb.AppendLine();
    }

    return sb.ToString().TrimEnd();
  }

  public static string GenerateRunSynchronousCommands(
      IEnumerable<SynchronousCommandConfiguration> commands) {
    StringBuilder sb = new();

    foreach (var command in commands
                 .Where(x => x.Enabled)
                 .OrderBy(x => x.Order)) {
      sb.AppendLine($"<RunSynchronousCommand wcm:action=\"add\">");
      sb.AppendLine($"\t\t\t\t\t<Order>{command.Order}</Order>");
      sb.AppendLine($"\t\t\t\t\t<Path>{EscapeXml(command.CommandLine)}</Path>");
      sb.AppendLine($"\t\t\t\t\t<Description>{EscapeXml(command.Description)}</Description>");
      sb.AppendLine($"\t\t\t\t</RunSynchronousCommand>");
      sb.AppendLine();
    }

    return sb.ToString().TrimEnd();
  }

  private static string EscapeXml(string value) {
    return SecurityElement.Escape(value) ?? "";
  }
}