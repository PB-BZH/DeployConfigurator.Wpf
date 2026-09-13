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
║  Nom de fichier : DiskConfiguration.cs												     
╚════════════════════════════════════════════════════════════════════════════════╝
*/
using DeployConfigurator.Wpf.Core.Enums;

namespace DeployConfigurator.Wpf.Core.Models;

public class DiskConfiguration {
  public FirmwareMode FirmwareMode { get; set; } = FirmwareMode.Auto;
  public DiskLayoutMode LayoutMode { get; set; } = DiskLayoutMode.SingleDisk;
  public int OsDisk { get; set; } = 0;
  public int? DataDisk { get; set; } = null;
  public int WindowsSizeMb { get; set; } = 60000;
  public int EfiSizeMb { get; set; } = 100;
  public int MsrSizeMb { get; set; } = 16;
  public int SystemSizeMb { get; set; } = 100;
  public int RecoverySizeMb { get; set; } = 800;
  public bool CreateData1 { get; set; } = true;
  public string BootLetter { get; set; } = "S";
  public string WindowsLetter { get; set; } = "W";
  public string RecoveryLetter { get; set; } = "R";
  public string Data1Letter { get; set; } = "L";
  public string LabelBootBios { get; set; } = "System";
  public string LabelBootUefi { get; set; } = "EFI";
  public string LabelWindows { get; set; } = "Windows";
  public string LabelRecovery { get; set; } = "Recovery";
  public string LabelData1 { get; set; } = "Data-1";
  public int? Data2Disk { get; set; } = null;
  public bool CreateData2 { get; set; } = false;
  public string Data2Letter { get; set; } = "M";
  public string LabelData2 { get; set; } = "Data-2";
}