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
║  Nom de fichier : UnattendConfiguration.cs												     
╚════════════════════════════════════════════════════════════════════════════════╝
*/
namespace DeployConfigurator.Wpf.Core.Models;

public class UnattendConfiguration {
  public string ProductKey { get; set; } = "XXXXX-XXXXX-XXXXX-XXXXX-XXXXX";
  public string UILanguage { get; set; } = "fr-FR";
  public string InputLocale { get; set; } = "fr-FR";
  public string SystemLocale { get; set; } = "fr-FR";
  public string UserLocale { get; set; } = "fr-FR";
  public string TimeZone { get; set; } = "Romance Standard Time";
  public string Organization { get; set; } = "Organisation";
  public string Owner { get; set; } = "Administrateur";
  public bool HideEulaPage { get; set; } = true;
  public bool HideOnlineAccountScreens { get; set; } = false;
  public bool HideWirelessSetupInOobe { get; set; } = false;
  public bool EnableFirstLogonDeployTools { get; set; } = true;
  public bool EnableFirstLogonShortcuts { get; set; } = true;
  public bool EnableAadJoinAssistant { get; set; } = true;
  public bool CreateLocalAccount { get; set; } = true;
  public string LocalUserName { get; set; } = "Administrateur";
  public string LocalUserGroup { get; set; } = "Administrators";
  public string LocalUserPassword { get; set; } = "";
  public bool EnableAutoLogon { get; set; } = true;
  public int AutoLogonCount { get; set; } = 1;
  public string UILanguageFallback { get; set; } = "fr-FR";
  public List<string> InputLocaleCodes { get; set; } = ["040c:0000040c"];
  public string ComputerName { get; set; } = "*";
}