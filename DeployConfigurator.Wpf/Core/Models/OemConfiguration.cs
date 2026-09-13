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
║  Nom de fichier : OemConfiguration.cs												     
╚════════════════════════════════════════════════════════════════════════════════╝
*/
namespace DeployConfigurator.Wpf.Core.Models;

public class OemConfiguration {
  public bool EnableOemInformation { get; set; } = false;

  public string Manufacturer { get; set; } = "";
  public string Model { get; set; } = "";
  public string SupportPhone { get; set; } = "";
  public string SupportUrl { get; set; } = "";
  public string SupportHours { get; set; } = "";

  public bool EnableWallpaper { get; set; } = false;
  public string WallpaperPath { get; set; } = "";

  public bool EnableLogo { get; set; } = false;
  public string LogoPath { get; set; } = "";
  public bool HideEulaPage { get; set; } = true;

  public int ProtectYourPc { get; set; } = 1;

  public bool HideOnlineAccountScreens { get; set; } = false;

  public bool HideOEMRegistrationScreen { get; set; } = true;

  public bool HideWirelessSetupInOobe { get; set; } = false;
  public string SupportAppUrl { get; set; } = "";
  public string SupportProvider { get; set; } = "";
  public bool UseManufacturer { get; set; } = true;
  public bool UseModel { get; set; } = true;
  public bool UseSupportAppUrl { get; set; } = false;
  public bool UseSupportUrl { get; set; } = true;
  public bool UseSupportHours { get; set; } = true;
  public bool UseSupportProvider { get; set; } = false;
  public bool UseSupportPhone { get; set; } = true;
}