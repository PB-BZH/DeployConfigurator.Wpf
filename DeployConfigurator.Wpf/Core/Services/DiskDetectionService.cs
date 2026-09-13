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
║  Nom de fichier : DiskDetectionService.cs												     
╚════════════════════════════════════════════════════════════════════════════════╝
*/
using System.Management;
using DeployConfigurator.Wpf.Core.Models;

namespace DeployConfigurator.Wpf.Core.Services;

public static class DiskDetectionService {
  public static List<PhysicalDiskInfo> GetDisks() {
    List<PhysicalDiskInfo> disks = [];

    using ManagementObjectSearcher searcher = new("SELECT * FROM Win32_DiskDrive");

    foreach (ManagementObject disk in searcher.Get().Cast<ManagementObject>()) {
      PhysicalDiskInfo info = new() {
        DiskNumber = Convert.ToInt32(disk["Index"]),
        Model = disk["Model"]?.ToString() ?? "Unknown",
        SizeBytes = Convert.ToInt64(disk["Size"] ?? 0),
        BusType = disk["InterfaceType"]?.ToString() ?? ""
      };

      disks.Add(info);
    }

    return [.. disks.OrderBy(d => d.DiskNumber)];
  }
}