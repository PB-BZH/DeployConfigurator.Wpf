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
║  Nom de fichier : BuildValidationService.cs												     
╚════════════════════════════════════════════════════════════════════════════════╝
*/
using System.IO;
using System.Text.RegularExpressions;
using DeployConfigurator.Wpf.Core.Models;

namespace DeployConfigurator.Wpf.Core.Services;

public sealed partial class BuildValidationService {
  public static List<ValidationIssue> Validate(
      DeploymentProfile profile) {
    List<ValidationIssue> issues = [];

    ValidateOutput(profile,issues);
    ValidateMediaFolder(profile,issues);
    ValidateTools(issues);
    ValidateTemplates(issues);

    ValidatePackage(profile,issues);
    ValidateMediaFolder(profile,issues);
    ValidateIsoOutput(profile,issues);
    ValidateUnresolvedTokens(profile,issues);

    return issues;
  }

  private static void AddOk(
    List<ValidationIssue> issues,
    string category,
    string message) {
    issues.Add(new ValidationIssue {
      Category = category,
      Severity = "OK",
      Message = message
    });
  }

  private static void AddWarning(
      List<ValidationIssue> issues,
      string category,
      string message) {
    issues.Add(new ValidationIssue {
      Category = category,
      Severity = "WARNING",
      Message = message
    });
  }

  private static void AddError(
      List<ValidationIssue> issues,
      string category,
      string message) {
    issues.Add(new ValidationIssue {
      Category = category,
      Severity = "ERROR",
      Message = message
    });
  }

  private static void ValidatePackage(
    DeploymentProfile profile,
    List<ValidationIssue> issues) {
    const string category = "GENERATED PACKAGE VALIDATION";

    string packageRoot = Path.Combine(
        profile.Package.OutputDirectory,
        profile.Package.PackageName
    );

    if (!Directory.Exists(packageRoot)) {
      AddWarning(issues,category,"Deploy package not generated yet. It will be created by Build All.");
      return;
    }

    AddOk(
        issues,
        category,
        "Deploy package folder found."
    );

    if (File.Exists(Path.Combine(packageRoot,"autounattend.xml")))
      AddOk(issues,category,"autounattend.xml found.");
    else
      AddError(issues,category,"autounattend.xml missing.");

    if (Directory.Exists(Path.Combine(packageRoot,"Deploy")))
      AddOk(issues,category,"Deploy folder found.");
    else
      AddError(issues,category,"Deploy folder missing.");
  }

  private static void ValidateMediaFolder(
      DeploymentProfile profile,
      List<ValidationIssue> issues) {
    const string category = "WINDOWS MEDIA VALIDATION";

    if (!File.Exists(profile.WindowsMedia.WindowsIsoPath)) {
      AddError(
          issues,
          category,
          "Windows ISO not found."
      );
    }
    else {
      AddOk(
          issues,
          category,
          "Windows ISO found."
      );
    }

    if (string.IsNullOrWhiteSpace(profile.WindowsMedia.MediaFolder)) {
      AddError(
          issues,
          category,
          "Media folder not defined."
      );
    }
    else {
      AddOk(
          issues,
          category,
          "Media folder defined."
      );
    }
  }

  private static void ValidateIsoOutput(
    DeploymentProfile profile,
    List<ValidationIssue> issues) {
    if (!profile.WindowsMedia.BuildIso)
      return;

    string isoPath = profile.WindowsMedia.IsoOutputPath;

    if (string.IsNullOrWhiteSpace(isoPath)) {
      issues.Add(new ValidationIssue {
        Severity = "ERROR",
        Message = "ISO output path not defined."
      });

      return;
    }

    string? isoDir = Path.GetDirectoryName(isoPath);

    if (string.IsNullOrWhiteSpace(isoDir)) {
      issues.Add(new ValidationIssue {
        Severity = "ERROR",
        Message = "Invalid ISO output path."
      });

      return;
    }

    if (!Directory.Exists(isoDir)) {
      issues.Add(new ValidationIssue {
        Severity = "ERROR",
        Message = "ISO output directory does not exist."
      });
    }

    if (File.Exists(isoPath)) {
      issues.Add(new ValidationIssue {
        Severity = "WARNING",
        Message = "ISO output file already exists and may be overwritten."
      });
    }
  }

  private static void ValidateUnresolvedTokens(
      DeploymentProfile profile,
      List<ValidationIssue> issues) {
    string packageRoot = Path.Combine(
        profile.Package.OutputDirectory,
        profile.Package.PackageName
    );

    if (!Directory.Exists(packageRoot))
      return;

    string[] files =
    [
        Path.Combine(packageRoot, "autounattend.xml"),
        Path.Combine(packageRoot, "Deploy", "Setup", "Config", "unattend.xml"),
        Path.Combine(packageRoot, "Deploy", "WinPE", "Scripts", "diskprep.cmd"),
        Path.Combine(packageRoot, "Deploy", "Setup", "Scripts", "SetupComplete.cmd"),
        Path.Combine(packageRoot, "Deploy", "Setup", "Scripts", "orchestrator_resume.cmd"),
        Path.Combine(packageRoot, "profile.deploy.json")
    ];

    foreach (string file in files) {
      if (!File.Exists(file))
        continue;

      string content = File.ReadAllText(file);

      MatchCollection matches = TokenPlaceholderRegex().Matches(content);

      foreach (Match match in matches) {
        issues.Add(new ValidationIssue {
          Severity = "WARNING",
          Message =
                "Unresolved token found in " +
                Path.GetFileName(file) +
                " : " +
                match.Value
        });
      }
    }
  }

  private static void ValidateOutput(
      DeploymentProfile profile,
      List<ValidationIssue> issues) {
    const string category = "CONFIGURATION VALIDATION";

    if (string.IsNullOrWhiteSpace(profile.Package.OutputDirectory)) {
      AddError(
          issues,
          category,
          "Output directory not defined."
      );

      return;
    }

    AddOk(
        issues,
        category,
        "Output directory defined."
    );
  }




  private static void ValidateTools(
      List<ValidationIssue> issues) {
    string sevenZip =
        @"C:\Program Files\7-Zip\7z.exe";

    if (!File.Exists(sevenZip)) {
      issues.Add(new ValidationIssue {
        Severity = "ERROR",
        Message =
              "7-Zip not found."
      });
    }

    string oscdimg =
        @"C:\Program Files (x86)\Windows Kits\10\Assessment and Deployment Kit\Deployment Tools\amd64\Oscdimg\oscdimg.exe";

    if (!File.Exists(oscdimg)) {
      issues.Add(new ValidationIssue {
        Severity = "WARNING",
        Message =
              "oscdimg.exe not found."
      });
    }
  }

  private static void ValidateTemplates(
      List<ValidationIssue> issues) {
    string templatesRoot = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory,
        "Templates"
    );

    string[] requiredTemplates = [
            "autounattend.template.xml",
            "unattend.template.xml",
            "diskprep.template.cmd",
            "SetupComplete.template.cmd",
            "orchestrator_resume.template.cmd"
        ];

    foreach (string template in requiredTemplates) {
      string path =
          Path.Combine(
              templatesRoot,
              template
          );

      if (!File.Exists(path)) {
        issues.Add(new ValidationIssue {
          Severity = "ERROR",
          Message =
                "Template missing : " +
                template
        });
      }
    }
  }

  [GeneratedRegex(@"__[A-Z0-9_]+__")]
  private static partial Regex TokenPlaceholderRegex();
}