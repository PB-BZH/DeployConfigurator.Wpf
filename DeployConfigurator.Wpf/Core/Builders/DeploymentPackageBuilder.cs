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
║  Nom de fichier : DeploymentPackageBuilder.cs												     
╚════════════════════════════════════════════════════════════════════════════════╝
*/
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using DeployConfigurator.Wpf.Core.Enums;
using DeployConfigurator.Wpf.Core.Models;
using DeployConfigurator.Wpf.Core.Services;

namespace DeployConfigurator.Wpf.Core.Builders;

public class DeploymentPackageBuilder {

  private readonly List<string> _copiedFiles = [];

  public void GeneratePackage(DeploymentProfile profile,Action<int,string>? progress = null) {

    // ============================================================
    // INITIALISATION
    // ============================================================

    progress?.Invoke(10,"Preparing package...");

    string outputRoot = Path.Combine(profile.Package.OutputDirectory,profile.Package.PackageName);
    string templatesRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Templates");

    Directory.CreateDirectory(outputRoot);


    // ============================================================
    // STRUCTURE DEPLOY
    // ============================================================

    progress?.Invoke(20,"Creating Deploy structure...");

    GenerateDeployStructure(outputRoot,profile);


    // ============================================================
    // FICHIERS DE CONFIGURATION
    // ============================================================

    progress?.Invoke(65,"Generating configuration files...");

    GenerateConfigurationFiles(outputRoot,templatesRoot,profile);


    // ============================================================
    // MANIFEST
    // ============================================================

    progress?.Invoke(90,"Generating package manifest...");

    GenerateManifest(outputRoot,profile);
  }

  private void GenerateDeployStructure(
    string outputRoot,
    DeploymentProfile profile) {
    string deployRoot =
        Path.Combine(outputRoot,"Deploy");

    Directory.CreateDirectory(deployRoot);

    if (profile.Package.IncludeDrivers) {
      CopyDirectory(
          profile.Package.DriversSourcePath,
          Path.Combine(deployRoot,"Drivers")
      );
    }

    if (profile.Package.IncludeApplications) {
      CopyDirectory(
          profile.Package.ApplicationsSourcePath,
          Path.Combine(deployRoot,"Logiciels")
      );
    }

    if (profile.Package.IncludeSetupScripts) {
      CopyDirectory(
          profile.Package.SetupScriptsSourcePath,
          Path.Combine(deployRoot,"Setup","Scripts")
      );
    }

    if (profile.Package.IncludeSetupConfig) {
      CopyDirectory(
          profile.Package.SetupConfigSourcePath,
          Path.Combine(deployRoot,"Setup","Config")
      );
    }
  }

  private static void GenerateConfigurationFiles(string outputRoot,string templatesRoot,DeploymentProfile profile) {
    string deployRoot = Path.Combine(outputRoot,"Deploy");
    string winpeScripts = Path.Combine(deployRoot,"WinPE","Scripts");
    string setupConfig = Path.Combine(deployRoot,"Setup","Config");
    string setupScripts = Path.Combine(deployRoot,"Setup","Scripts");

    Directory.CreateDirectory(winpeScripts);
    Directory.CreateDirectory(setupConfig);
    Directory.CreateDirectory(setupScripts);

    File.WriteAllText(
        Path.Combine(outputRoot,"autounattend.xml"),
        PreviewDocumentBuilder.Generate(
            PreviewDocumentType.AutoUnattend,
            profile,
            templatesRoot
        ),
        EncodingHelper.Utf8NoBom
    );
    File.WriteAllText(
        Path.Combine(winpeScripts,"diskprep.cmd"),
        PreviewDocumentBuilder.Generate(
            PreviewDocumentType.Diskprep,
            profile,
            templatesRoot
        ),
        EncodingHelper.Utf8NoBom
    );
    File.WriteAllText(
        Path.Combine(setupConfig,"unattend.xml"),
        PreviewDocumentBuilder.Generate(
            PreviewDocumentType.Unattend,
            profile,
            templatesRoot
        ),
        EncodingHelper.Utf8NoBom
    );
    File.WriteAllText(
        Path.Combine(setupScripts,"SetupComplete.cmd"),
        PreviewDocumentBuilder.Generate(
            PreviewDocumentType.SetupComplete,
            profile,
            templatesRoot
        ),
        EncodingHelper.Utf8NoBom
    );
    File.WriteAllText(
        Path.Combine(setupScripts,"orchestrator_resume.cmd"),
        PreviewDocumentBuilder.Generate(
            PreviewDocumentType.Orchestrator,
            profile,
            templatesRoot
        ),
        EncodingHelper.Utf8NoBom
    );
    File.WriteAllText(
        Path.Combine(outputRoot,"profile.deploy.json"),
        PreviewDocumentBuilder.Generate(
            PreviewDocumentType.ProfileJson,
            profile,
            templatesRoot
        ),
        EncodingHelper.Utf8NoBom
    );
  }

  private void CopyDirectory(string sourceDir,string destinationDir) {
    if (string.IsNullOrWhiteSpace(sourceDir))
      return;

    if (!Directory.Exists(sourceDir))
      throw new DirectoryNotFoundException(sourceDir);

    Directory.CreateDirectory(destinationDir);

    foreach (string file in Directory.GetFiles(sourceDir,"*",SearchOption.AllDirectories)) {
      string relativePath = Path.GetRelativePath(sourceDir,file);
      string destinationFile = Path.Combine(destinationDir,relativePath);

      Directory.CreateDirectory(Path.GetDirectoryName(destinationFile)!);
      File.Copy(file,destinationFile,true);

      _copiedFiles.Add(destinationFile);
    }
  }

  private void GenerateManifest(string outputRoot,DeploymentProfile profile) {
    var manifest = new {
      GeneratedAt = DateTime.Now,
      profile.Package.PackageName,
      profile.Package.Version,
      profile.Package.Author,

      profile.Image,
      profile.Disk,
      profile.Unattend,

      Included = new {
        profile.Package.IncludeDrivers,
        profile.Package.IncludeApplications,
        profile.Package.IncludeScripts,
        profile.Package.IncludeWinPE
      },

      CopiedFiles = _copiedFiles.Select(f => new {
        Path = f,
        SizeBytes = new FileInfo(f).Length
      }).ToList()
    };

    JsonSerializerOptions serializerOptions = new() {
      WriteIndented = true,
      Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
    JsonSerializerOptions jsonSerializerOptions = serializerOptions;
    string json = JsonSerializer.Serialize(
        manifest,
        jsonSerializerOptions
    );

    File.WriteAllText(
        Path.Combine(outputRoot,"deploy.manifest.json"),
        json,
        EncodingHelper.Utf8NoBom
    );
  }
}