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
║  Nom de fichier : DiskprepBuilder.cs												     
╚════════════════════════════════════════════════════════════════════════════════╝
*/
using DeployConfigurator.Wpf.Core.Enums;
using DeployConfigurator.Wpf.Core.Models;

namespace DeployConfigurator.Wpf.Core.Builders;

public class DiskprepBuilder {
  public static string Generate(string templateText,DeploymentProfile profile) {
    var config = profile.Disk;
    var image = profile.Image;
    var markers = profile.Markers;
    var values = new Dictionary<string,string> {
      ["FIRMWARE_MODE"] = config.FirmwareMode.ToString().ToUpperInvariant(),
      ["LAYOUT_MODE"] = config.LayoutMode.ToString().ToUpperInvariant(),

      ["OS_DISK"] = config.OsDisk.ToString(),
      ["DATA_DISK"] = config.DataDisk?.ToString() ?? "",
      ["DATA2_DISK"] = config.Data2Disk?.ToString() ?? "",

      ["SEPARATE_DATA_DISK"] = config.LayoutMode == DiskLayoutMode.SeparateDataDisk ? "YES" : "NO",
      ["DUAL_DATA_DISK"] = config.LayoutMode == DiskLayoutMode.DualDataDisk ? "YES" : "NO",


      ["BOOT_MARKER"] = markers.BootMarker,
      ["WINDOWS_MARKER"] = markers.WindowsMarker,
      ["RECOVERY_MARKER"] = markers.RecoveryMarker,
      ["DATA1_MARKER"] = markers.Data1Marker,
      ["DATA2_MARKER"] = markers.Data2Marker,


      ["WINDOWS_SIZE_MB"] = config.WindowsSizeMb.ToString(),
      ["EFI_SIZE_MB"] = config.EfiSizeMb.ToString(),
      ["MSR_SIZE_MB"] = config.MsrSizeMb.ToString(),
      ["SYSTEM_SIZE_MB"] = config.SystemSizeMb.ToString(),
      ["RECOVERY_SIZE_MB"] = config.RecoverySizeMb.ToString(),

      ["CREATE_DATA1"] = config.CreateData1 ? "YES" : "NO",
      ["CREATE_DATA2"] = config.CreateData2 ? "YES" : "NO",

      ["BOOT_LETTER"] = config.BootLetter,
      ["WINDOWS_LETTER"] = config.WindowsLetter,
      ["RECOVERY_LETTER"] = config.RecoveryLetter,
      ["DATA1_LETTER"] = config.Data1Letter,
      ["DATA2_LETTER"] = config.Data2Letter,

      ["LABEL_BOOT_BIOS"] = config.LabelBootBios,
      ["LABEL_BOOT_UEFI"] = config.LabelBootUefi,
      ["LABEL_WINDOWS"] = config.LabelWindows,
      ["LABEL_RECOVERY"] = config.LabelRecovery,
      ["LABEL_DATA2"] = config.LabelData2,
      ["LABEL_DATA1"] = config.LabelData1,

      ["IMAGE_TYPE"] = image.ImageType.ToString().ToUpperInvariant(),
      ["INSTALL_IMAGE"] = image.InstallImage,
      ["IMAGE_INDEX"] = image.ImageIndex.ToString()
    };

    return TemplateEngine.ReplaceTokens(templateText,values);
  }
}