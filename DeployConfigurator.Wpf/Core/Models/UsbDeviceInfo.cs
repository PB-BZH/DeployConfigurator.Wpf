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
║  Nom de fichier : UsbDeviceInfo.cs												     
╚════════════════════════════════════════════════════════════════════════════════╝
*/
using System.Globalization;

namespace DeployConfigurator.Wpf.Core.Models;

public class UsbDeviceInfo {
  public int DiskNumber { get; set; }
  public string Model { get; set; } = "";
  public string InterfaceType { get; set; } = "";
  public string SizeText { get; set; } = "";
  public ulong SizeBytes { get; set; }
  public string DriveLetters { get; set; } = "";
  public string VolumeLabels { get; set; } = "";

  public override string ToString() {
    string drive =
        string.IsNullOrWhiteSpace(DriveLetters)
            ? "?"
            : DriveLetters;

    string label =
        string.IsNullOrWhiteSpace(VolumeLabels)
            ? "NO_LABEL"
            : VolumeLabels;

    double sizeGb =
        SizeBytes / 1024d / 1024d / 1024d;

    string sizeText =
        sizeGb.ToString(
            "F2",
            CultureInfo.GetCultureInfo("fr-FR")
        );

    return
        $"{drive} ({sizeText} GB) - {label}";
  }
}