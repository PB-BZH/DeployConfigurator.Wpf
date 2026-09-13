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
║  Nom de fichier : ProfileSerializer.cs												     
╚════════════════════════════════════════════════════════════════════════════════╝
*/
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using DeployConfigurator.Wpf.Core.Models;

namespace DeployConfigurator.Wpf.Core.Services;

public static class ProfileSerializer {
  private static readonly JsonSerializerOptions _options = new() {
    WriteIndented = true,
    Converters =
      {
            new JsonStringEnumConverter()
        }
  };

  public static void Save(string filePath,DeploymentProfile profile) {
    string json = JsonSerializer.Serialize(profile,_options);

    File.WriteAllText(filePath,json);
  }

  public static DeploymentProfile Load(string filePath) {
    string json = File.ReadAllText(filePath);

    DeploymentProfile? profile = JsonSerializer.Deserialize<DeploymentProfile>(
            json,
            _options
        );

    return profile ?? new DeploymentProfile();
  }
}