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
║  Nom de fichier : BuildPipelineService.cs												     
╚════════════════════════════════════════════════════════════════════════════════╝
*/
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using DeployConfigurator.Wpf.Core.Builders;
using DeployConfigurator.Wpf.Core.Models;

namespace DeployConfigurator.Wpf.Core.Services;

public partial class BuildPipelineService {
  public Action<int,string>? ProgressCallback { get; set; }

  public event Action<int,string>? ProgressChanged;
  public Action<string>? LiveLogCallback { get; set; }

  public sealed class PipelineProgress(
      int percent,
      string message) {
    public int Percent { get; } = percent;
    public string Message { get; } = message;
  }

  private void OnProgressChanged(int percent,string message) {
    ProgressChanged?.Invoke(percent,message);
  }

  private static void CleanDirectory(
    string path,
    PipelineLogger log) {
    if (!Directory.Exists(path))
      return;

    foreach (string file in Directory.GetFiles(
                 path,
                 "*",
                 SearchOption.AllDirectories)) {
      try {
        File.SetAttributes(
            file,
            FileAttributes.Normal
        );

        File.Delete(file);
      }
      catch (Exception ex) {
        log.Warning(
            "Unable to delete file : " +
            file +
            " -> " +
            ex.Message
        );
      }
    }

    foreach (string dir in Directory.GetDirectories(
                 path,
                 "*",
                 SearchOption.AllDirectories)
                 .OrderByDescending(x => x.Length)) {
      try {
        Directory.Delete(dir,true);
      }
      catch (Exception ex) {
        log.Warning(
            "Unable to delete folder : " +
            dir +
            " -> " +
            ex.Message
        );
      }
    }
  }

  public static PipelineResult BuildPackage(DeploymentProfile profile) {
    PipelineLogger log = new();

    try {
      log.Info("BUILD PACKAGE START");

      new DeploymentPackageBuilder().GeneratePackage(profile);

      log.Success("Package generated successfully.");

      PipelineResult result = PipelineResult.Ok(
          "Package généré avec succès.",
          [.. log.Lines]
      );

      //SavePipelineLog(profile,result);

      return result;
    }
    catch (Exception ex) {
      log.Error(ex.Message);
      PipelineResult result = PipelineResult.Fail(
          ex.Message,
          [.. log.Lines]
      );

      //SavePipelineLog(profile,result);

      result = PipelineResult.Ok(
          "Package généré avec succès.",
          [.. log.Lines]
      );

      //SavePipelineLog(profile,result);

      return result;
    }
  }

  public PipelineResult BuildWindowsMedia(DeploymentProfile profile) {
    PipelineLogger log = new();

    try {
      log.Info("BUILD WINDOWS MEDIA START");

      string isoPath = profile.WindowsMedia.WindowsIsoPath;
      string mediaPath = profile.WindowsMedia.MediaFolder;

      string deployPackage = Path.Combine(profile.Package.OutputDirectory,profile.Package.PackageName);

      if (!File.Exists(isoPath)) {
        PipelineResult result = PipelineResult.Fail(
            "ISO Windows introuvable.",
            [.. log.Lines]
        );

        //SavePipelineLog(profile,result);
        return result;
      }

      if (!Directory.Exists(deployPackage)) {
        PipelineResult result = PipelineResult.Fail(
            "Package Deploy introuvable : " + deployPackage,
            [.. log.Lines]
        );

        //SavePipelineLog(profile,result);
        return result;
      }

      Directory.CreateDirectory(mediaPath);

      if (profile.WindowsMedia.CleanMediaFolderBeforeBuild) {
        log.Info("Cleaning media folder...");

        CleanDirectory(mediaPath,log);
      }

      if (profile.WindowsMedia.ExtractWindowsIso) {
        ExtractArchiveWith7ZipProgress(
            source: isoPath,
            destination: mediaPath,
            log: log,
            progressStart: 10,
            progressSpan: 30,
            progressLabel: "Extraction ISO Windows",
            reportProgress: (percent,message) => ProgressCallback?.Invoke(percent,message),
            liveLog: LiveLogCallback,
            cancellationToken: CancellationToken.None
        );
      }

      if (profile.WindowsMedia.InjectAutounattend) {
        log.Info("Injecting autounattend.xml...");

        string sourceAutoUnattend = Path.Combine(deployPackage,"autounattend.xml");
        string targetAutoUnattend = Path.Combine(mediaPath,"autounattend.xml");

        if (!File.Exists(sourceAutoUnattend))
          throw new FileNotFoundException(
              "autounattend.xml source introuvable.",
              sourceAutoUnattend
          );

        File.Copy(sourceAutoUnattend,targetAutoUnattend,true);

        log?.Info("autounattend.xml copied.");
      }

      if (profile.WindowsMedia.InjectDeployFolder) {
        log?.Info("Injecting Deploy folder...");

        string sourceDeploy = Path.Combine(deployPackage,"Deploy");
        string targetDeploy = Path.Combine(mediaPath,"Deploy");

        if (!Directory.Exists(sourceDeploy))
          throw new DirectoryNotFoundException(
              "Deploy source introuvable : " + sourceDeploy
          );

        CopyDirectory(sourceDeploy,targetDeploy);

        log?.Info("Deploy folder copied.");
      }

      // MEDIA_MARKER.txt injection

      string markerPath = Path.Combine(
          mediaPath,
          "MEDIA_MARKER.txt"
      );

      StringBuilder marker = new();

      marker.AppendLine("DEPLOY CONFIGURATOR MEDIA");
      marker.AppendLine("Version=1.5");
      marker.AppendLine(
          $"BuildDate={DateTime.Now:yyyy-MM-dd HH:mm:ss}"
      );
      marker.AppendLine(
          $"Profile={profile.Name}"
      );

      File.WriteAllText(
          markerPath,
          marker.ToString(),
          EncodingHelper.Utf8NoBom
      );

      log?.Info("MEDIA_MARKER.txt injected");
      log?.Success("Windows media generated successfully.");

      PipelineResult success = PipelineResult.Ok(
          "Média Windows préparé avec succès.",
          log?.Lines.ToList()
      );

      //SavePipelineLog(profile,success);
      return success;
    }
    catch (Exception ex) {
      log?.Error(ex.Message);

      PipelineResult failure = PipelineResult.Fail(
          "Erreur Build Windows Media : " + ex.Message,
          log?.Lines.ToList()
      );

      //SavePipelineLog(profile,failure);
      return failure;
    }
  }

  public PipelineResult BuildIso(DeploymentProfile profile) {
    PipelineLogger log = new();
    ProgressCallback?.Invoke(10,"Preparing ISO build...");
    try {
      log.Info("BUILD ISO START");
      string mediaPath = profile.WindowsMedia.MediaFolder;
      string isoPath = profile.WindowsMedia.IsoOutputPath;
      if (profile.WindowsMedia.DeleteExistingIsoBeforeBuild) {
        if (File.Exists(isoPath)) {
          File.SetAttributes(isoPath,FileAttributes.Normal);

          File.Delete(isoPath);

          log.Info("Previous ISO deleted.");
        }
      }
      if (!Directory.Exists(mediaPath)) {
        log.Error("WindowsMedia folder not found : " + mediaPath);
        return PipelineResult.Fail("Dossier WindowsMedia introuvable.",[.. log.Lines]);
      }
      string oscdimg = @"C:\Program Files (x86)\Windows Kits\10\Assessment and Deployment Kit\Deployment Tools\amd64\Oscdimg\oscdimg.exe";
      if (!File.Exists(oscdimg)) {
        log.Error("oscdimg.exe not found.");
        return PipelineResult.Fail("oscdimg.exe introuvable.",[.. log.Lines]);
      }
      string biosBoot = Path.Combine(mediaPath,@"boot\etfsboot.com");
      string uefiBoot = Path.Combine(mediaPath,@"efi\microsoft\boot\efisys.bin");
      if (!File.Exists(biosBoot)) {
        log.Error("BIOS boot file missing : " + biosBoot);
        return PipelineResult.Fail("Fichier BIOS boot introuvable.",[.. log.Lines]);
      }
      if (!File.Exists(uefiBoot)) {
        log.Error("UEFI boot file missing : " + uefiBoot);
        return PipelineResult.Fail("Fichier UEFI boot introuvable.",[.. log.Lines]);
      }
      log.Info("Launching oscdimg...");

      string args = $"-m -o -u2 -udfver102 -bootdata:2#p0,e,b\"{biosBoot}\"#pEF,e,b\"{uefiBoot}\" \"{mediaPath}\" \"{isoPath}\"";

      ProgressCallback?.Invoke(40,"Launching oscdimg...");
      ProcessStartInfo psi = new() {
        FileName = oscdimg,
        Arguments = args,
        UseShellExecute = false,
        CreateNoWindow = true,
        RedirectStandardOutput = true,
        RedirectStandardError = true
      };

      using Process process = Process.Start(psi)!;
      process.WaitForExit();
      log.Info(
          "oscdimg exit code = " +
          process.ExitCode
      );
      ProgressCallback?.Invoke(90,"Finalizing ISO...");

      if (process.ExitCode != 0) {
        log.Error("ISO generation failed.");
        return PipelineResult.Fail("Erreur pendant la génération ISO.",[.. log.Lines]);
      }

      log.Success("ISO generated successfully.");
      ProgressCallback?.Invoke(100,"ISO generated.");
      return PipelineResult.Ok("ISO générée avec succès.",[.. log.Lines]);
    }
    catch (Exception ex) {
      log.Error(ex.Message);
      return PipelineResult.Fail("Erreur Build ISO : " + ex.Message,[.. log.Lines]);
    }
  }

  private static int? TryParse7ZipTotalFiles(string line) {
    if (string.IsNullOrWhiteSpace(line))
      return null;

    Match match = SevenZipFilesFoldersRegex().Match(line);

    if (!match.Success)
      return null;

    if (int.TryParse(match.Groups["files"].Value,out int files))
      return files;

    return null;
  }

  public static void ExtractArchiveWith7ZipProgress(
      string source,
      string destination,
      PipelineLogger log,
      int progressStart,
      int progressSpan,
      string progressLabel,
      Action<int,string>? reportProgress,
      Action<string>? liveLog,
      CancellationToken cancellationToken) {
    log?.Info($"{progressLabel} start...");

    reportProgress?.Invoke(
        progressStart,
        progressLabel + "..."
    );

    ExtractIsoWith7Zip(
        isoPath: source,
        destination: destination,
        log: log,
        progress: (percent,message) => {
          cancellationToken.ThrowIfCancellationRequested();

          int mappedPercent =
              progressStart +
              (int)(percent * progressSpan / 100.0);

          int progressEnd =
              progressStart + progressSpan;

          mappedPercent =
              Math.Clamp(
                  mappedPercent,
                  progressStart,
                  progressEnd
              );

          reportProgress?.Invoke(
              mappedPercent,
              message
          );
        });

    log?.Info($"{progressLabel} completed.");
  }

  private static int Get7ZipFileCount(
      string isoPath,
      string sevenZip) {
    ProcessStartInfo psi = new() {
      FileName = sevenZip,
      Arguments = $"l \"{isoPath}\"",
      UseShellExecute = false,
      CreateNoWindow = true,
      RedirectStandardOutput = true,
      RedirectStandardError = true
    };

    using Process process = Process.Start(psi)!;

    string output = process.StandardOutput.ReadToEnd();
    string error = process.StandardError.ReadToEnd();

    process.WaitForExit();

    string allOutput = output + Environment.NewLine + error;

    foreach (string line in allOutput.Split('\n')) {
      if (!line.Contains("folders",StringComparison.OrdinalIgnoreCase))
        continue;

      Match match = SevenZipFilesFoldersRegexSimple().Match(line);

      if (match.Success &&
          int.TryParse(match.Groups["files"].Value,out int files) &&
          int.TryParse(match.Groups["folders"].Value,out int folders)) {
        return files + folders;
      }
    }

    return 0;
  }

  internal static void ExtractIsoWith7Zip(string isoPath,string destination,PipelineLogger? log = null,Action<int,string>? progress = null) {
    string sevenZip = @"C:\Program Files\7-Zip\7z.exe";

    if (!File.Exists(sevenZip))
      throw new FileNotFoundException(
          "7-Zip introuvable.",
          sevenZip
      );

    int totalFiles =
        Get7ZipFileCount(
            isoPath,
            sevenZip
        );

    ProcessStartInfo psi = new() {
      FileName = sevenZip,

      Arguments = $"x \"{isoPath}\" -o\"{destination}\" -y -bsp2",

      UseShellExecute = false,
      CreateNoWindow = true,

      RedirectStandardOutput = true,
      RedirectStandardError = true,

      StandardOutputEncoding = Encoding.GetEncoding(850),
      StandardErrorEncoding = Encoding.GetEncoding(850)
    };

    using Process process = new() {
      StartInfo = psi,
      EnableRaisingEvents = true
    };

    process.OutputDataReceived += (_,e) => {
      if (string.IsNullOrWhiteSpace(e.Data))
        return;

      Handle7ZipLine(
          e.Data.Trim(),
          log,
          ref totalFiles,
          progress
      );
    };

    process.ErrorDataReceived += (_,e) => {
      if (string.IsNullOrWhiteSpace(e.Data))
        return;

      Handle7ZipLine(
          e.Data.Trim(),
          log,
          ref totalFiles,
          progress
      );
    };
    process.Start();

    process.BeginOutputReadLine();
    process.BeginErrorReadLine();

    process.WaitForExit();

    if (process.ExitCode != 0) {
      throw new Exception(
          "7-Zip failed with exit code : " +
          process.ExitCode
      );
    }
  }
  private static SevenZipProgressInfo? TryParse7ZipProgressLine(string line) {
    if (string.IsNullOrWhiteSpace(line))
      return null;

    line = line.Trim();

    Match match = SevenZipProgressRegex().Match(line);

    if (!match.Success)
      return null;

    if (!int.TryParse(match.Groups["percent"].Value,out int percent))
      return null;

    if (!int.TryParse(match.Groups["index"].Value,out int fileIndex))
      return null;

    percent = Math.Clamp(percent,0,100);

    return new SevenZipProgressInfo {
      Percent = percent,
      FileIndex = fileIndex,
      FilePath = match.Groups["file"].Value.Trim()
    };
  }

  private sealed class SevenZipProgressInfo {
    public int Percent { get; set; }
    public int FileIndex { get; set; }
    public string FilePath { get; set; } = "";
  }

  private static void CopyDirectory(string sourceDir,string destinationDir) {
    if (!Directory.Exists(sourceDir))
      throw new DirectoryNotFoundException(sourceDir);

    Directory.CreateDirectory(destinationDir);

    foreach (string file in Directory.GetFiles(sourceDir,"*",SearchOption.AllDirectories)) {
      string relative = Path.GetRelativePath(sourceDir,file);
      string destFile = Path.Combine(destinationDir,relative);

      Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);
      File.Copy(file,destFile,true);
    }
  }

  private static void Handle7ZipLine(
    string line,
    PipelineLogger? log,
    ref int totalFiles,
    Action<int,string>? progress) {
    // ==================================================
    // TOTAL FILES
    // ==================================================

    int? detectedTotal =
        TryParse7ZipTotalFiles(line);

    if (detectedTotal.HasValue) {
      totalFiles = detectedTotal.Value;

      log?.Info(
          "[7Z] Total files : " +
          totalFiles
      );

      return;
    }

    // ==================================================
    // PROGRESS LINE
    // ==================================================

    SevenZipProgressInfo? info =
        TryParse7ZipProgressLine(line);

    if (info != null) {
      string fileName =
          Path.GetFileName(info.FilePath);

      string countText =
          totalFiles > 0
              ? $"{info.FileIndex}/{totalFiles}"
              : info.FileIndex.ToString();

      progress?.Invoke(
          info.Percent,
          $"Fichier {countText} : {fileName}"
      );

      return;
    }

    // ==================================================
    // OTHER OUTPUT
    // ==================================================

    log?.Info("[7Z] " + line);
  }

  public async Task<PipelineResult> BuildAllAsync(DeploymentProfile profile,CancellationToken cancellationToken = default) {
    PipelineLogger log = new();

    try {
      cancellationToken.ThrowIfCancellationRequested();

      PipelineResult packageResult =
          await Task.Run(
              () => BuildPackage(profile),
              cancellationToken
          );

      log.AddLines(packageResult.Logs);

      if (!packageResult.Success)
        return PipelineResult.Fail(packageResult.Message,[.. log.Lines]);

      cancellationToken.ThrowIfCancellationRequested();

      PipelineResult mediaResult =
          await Task.Run(
              () => BuildWindowsMedia(profile),
              cancellationToken
          );

      log.AddLines(mediaResult.Logs);

      if (!mediaResult.Success)
        return PipelineResult.Fail(mediaResult.Message,[.. log.Lines]);

      if (profile.WindowsMedia.BuildIso) {

        cancellationToken.ThrowIfCancellationRequested();

        PipelineResult isoResult =
            await Task.Run(
                () => BuildIso(profile),
                cancellationToken
            );

        log.AddLines(isoResult.Logs);

        if (!isoResult.Success)
          return PipelineResult.Fail(isoResult.Message,[.. log.Lines]);
      }

      log.Success("BUILD ALL SUCCESS");

      PipelineResult result = PipelineResult.Ok(
          "Build complet terminé avec succès.",
          [.. log.Lines]
      );

      //SavePipelineLog(profile,result);

      return result;
    }
    catch (Exception ex) {
      log.Error(ex.Message);

      return PipelineResult.Fail(
          ex.Message,
          [.. log.Lines]
      );
    }
  }

  [GeneratedRegex(@"\b(?<files>\d+)\s+files?,\s+(?<folders>\d+)\s+folders?\b",RegexOptions.IgnoreCase,"fr-FR")]
  private static partial Regex SevenZipFilesFoldersRegex();
  [GeneratedRegex(@"(?<files>\d+)\s+files?,\s+(?<folders>\d+)\s+folders?",RegexOptions.IgnoreCase,"fr-FR")]
  private static partial Regex SevenZipFilesFoldersRegexSimple();
  [GeneratedRegex(@"^(?<percent>\d{1,3})%\s+(?<index>\d+)\s+-\s+(?<file>.+)$"
  )]
  private static partial Regex SevenZipProgressRegex();
}