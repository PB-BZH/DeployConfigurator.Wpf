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
║  Nom de fichier : DeploymentProfile.cs												     
╚════════════════════════════════════════════════════════════════════════════════╝
*/
using System.Drawing;
using System.Text.Json.Serialization;
using DeployConfigurator.Wpf.Core.Services;

namespace DeployConfigurator.Wpf.Core.Models;

public class DeploymentProfile {
  public string Name { get; set; } = "Default Deployment Profile";
  public ProductOptions Product { get; set; } = new();
  public ImageConfiguration Image { get; set; } = new();
  public MarkerConfiguration Markers { get; set; } = new();
  public DiskConfiguration Disk { get; set; } = new();
  public UnattendConfiguration Unattend { get; set; } = new();
  public PackageConfiguration Package { get; set; } = new();
  public WindowsMediaConfiguration WindowsMedia { get; set; } = new();
  public OrchestrationConfiguration Orchestration { get; set; } = new();
  public UpdateManifest UpdateManifest { get; set; } = new();
  public WebInstallerOptions WebInstaller { get; set; } = new();
  public UploadOptions Upload { get; set; } = new();
  public OutputOptions Output { get; set; } = new();

  public sealed class OutputOptions {
    public string WixOutputDirectory { get; set; } = "Build\\Wix";
    public string MsiOutputDirectory { get; set; } = "Build\\Msi";
    public string MsiFileName { get; set; } = "MyApplication.msi";
  }

  public sealed class ProductOptions {
    public string ProductName { get; set; } = "Deploy Configurator";
    public string ProductId { get; set; } = "DeployConfigurator";
    public string ProductFolder { get; set; } = "DeployConfigurator";
    public string Manufacturer { get; set; } = "PB BZH Concept";
    public string Version { get; set; } = "1.0.0";
    public string Description { get; set; } = "Deployment Windows OEM Tool";
    public string UpgradeCode { get; set; } = "";
    public string IconPath { get; set; } = "";
    [JsonIgnore]
    public Image? LogoImage { get; set; } = Properties.Resources.Application;
    public string DownloadPageUrl { get; set; } = "https://wwww.pb-bzh-concept.fr";
    public string PrivacyPageUrl { get; set; } = "https://www.pb-bzh-concept.fr/softwares/privacy.php";
    public string Copyright { get; set; } = "© Copyright PB BZH Concept 2026";
    public string EmailContact { get; set; } = "admin@pb-bzh-concept.fr";
    public string DownloadCategory { get; set; } = "deploy-configurator-wpf";
  }

  public sealed class UploadOptions {
    public bool UploadWebFilesAfterBuild { get; set; } = false;
    public string Protocol { get; set; } = "SFTP";
    public string Host { get; set; } = "access-5020244126.webspace-host.com";
    public string UserName { get; set; } = "su185260";
    public string RemoteDirectory { get; set; } = "/";
    public string LocalDirectory { get; set; } = @"C:\wamp64\www\pb-bzh-concept.fr";
    public string WinScpPath { get; set; } = @"C:\Program Files (x86)\WinSCP\WinSCP.com";
    public string WebRemoteSite { get; set; } = "https://www.pb-bzh-concept.fr/softwares/";
    public string Password { get; set; } = "";
    public string CredentialTarget { get; set; } = "";
  }

  public sealed class WebInstallerOptions {
    public bool BuildWebInstaller { get; set; } = false;
    public string WebBundleName { get; set; } = "";
    public string WebOutputDirectory { get; set; } = "Build\\Web";
    public string WebSetupFileName { get; set; } = "WebSetup.exe";
    public string MsiDownloadUrl { get; set; } = "";
    public string WebBundleUpgradeCode { get; set; } = "";
    public bool PrepareWebPublishFolder { get; set; } = true;
    public string WebPublishDirectory { get; set; } = @"C:\wamp64\www\pb-bzh-concept.fr";
    public string WebProductFolder { get; set; } = "";
  }

  public List<SynchronousCommandConfiguration> FirstLogonCommands { get; set; } =
  [
    new()
    {
        Enabled = true,
        Type = SynchronousCommandType.Default,
        Order = 1,
        Description = "Resume deploy orchestrator",
        CommandLine = @"cmd /c C:\Deploy\Setup\Scripts\orchestrator_resume.cmd RESUMEONLY"
    },
    new()
    {
        Enabled = true,
        Type = SynchronousCommandType.Default,
        Order = 2,
        Description = "Create user shortcuts",
        CommandLine = @"cmd /c C:\Deploy\Setup\Scripts\create_user_shortcuts.cmd"
    },
    new()
    {
        Enabled = true,
        Type = SynchronousCommandType.Default,
        Order = 3,
        Description = "Install Deploy Tools registry",
        CommandLine = @"cmd /c C:\Deploy\Setup\Scripts\install_deploy_tools_reg.cmd"
    },
    new()
    {
        Enabled = true,
        Type = SynchronousCommandType.Default,
        Order = 4,
        Description = "Azure AD Join guided setup",
        CommandLine = @"cmd /c C:\Deploy\Setup\Scripts\aad_join_schedule.cmd"
    },
  ];


  public List<SynchronousCommandConfiguration> WindowsPeCommands { get; set; } =
  [
    new()
    {
        Enabled = true,
        Type = SynchronousCommandType.Default,
        Order = 1,
        Description = "Run disk preparation",
        CommandLine = @"cmd /c for %i in (C D E F G H I J K L M) do if exist %i:\deploy\winpe\scripts\diskprep.cmd call %i:\deploy\winpe\scripts\diskprep.cmd"
    }
  ];
  public OemConfiguration Oem { get; set; } = new();
}

public sealed class UpdateManifest {
  public string ProductName { get; set; } = "Deploy Configurator";
  public string Version { get; set; } = "";
  public string Publisher { get; set; } = "";
  public string DownloadPage { get; set; } = "https://www.pb-bzh-concept.fr/";
  public string PrivacyPage { get; set; } = "https://www.pb-bzh-concept.fr/softwares/privacy.php";
  public string MsiUrl { get; set; } = "";
  public string WebSetupUrl { get; set; } = "";
  public string UpdateManifestUrl { get; set; } = "";
  public string ReleaseDate { get; set; } = "";
  public string ApplicationId { get; set; } = "";
}
