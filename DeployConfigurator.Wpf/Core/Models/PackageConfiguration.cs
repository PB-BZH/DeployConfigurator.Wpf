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
║  Nom de fichier : PackageConfiguration.cs												     
╚════════════════════════════════════════════════════════════════════════════════╝
*/
namespace DeployConfigurator.Wpf.Core.Models;

public class PackageConfiguration {
  public string PackageName { get; set; } = "Deploy";
  public string Version { get; set; } = "1.0.0";
  public string Author { get; set; } = Environment.UserName;
  public string OutputDirectory { get; set; } = "";
  public bool IncludeDrivers { get; set; } = true;
  public bool IncludeApplications { get; set; } = true;
  public bool IncludeScripts { get; set; } = true;
  public bool IncludeWinPE { get; set; } = false;
  public string DriversSourcePath { get; set; } = "";
  public string ApplicationsSourcePath { get; set; } = "";
  public bool IncludeSetupScripts { get; set; } = true;
  public bool IncludeSetupConfig { get; set; } = true;

  public string SetupScriptsSourcePath { get; set; } = "";
  public string SetupConfigSourcePath { get; set; } = "";
}