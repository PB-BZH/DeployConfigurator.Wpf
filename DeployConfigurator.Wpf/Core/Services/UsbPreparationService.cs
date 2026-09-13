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
║  Le 22/5/2026 - 21:34                                                          ║                                                          
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
║  Nom de fichier : UsbPreparationService.cs												     
╚════════════════════════════════════════════════════════════════════════════════╝
*/
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Text;
using DeployConfigurator.Wpf.Core.Models;

namespace DeployConfigurator.Wpf.Core.Services;

public class UsbPreparationService {
  private readonly ManualResetEventSlim _pauseEvent = new(true);
  public Action<string>? LiveLogCallback { get; set; }
  public Action<int,string>? ProgressCallback { get; set; }


  public void Pause() {
    _pauseEvent.Reset();
  }

  public void Resume() {
    _pauseEvent.Set();
  }

  public static UsbPreparationResult ValidatePreparedUsbUefi(string usbRoot) {
    UsbPreparationResult result = new();

    try {
      result.Logs.Add(
          LogLine("=== UEFI MEDIA VALIDATION ===")
      );

      string[] requiredPaths =
      [
            @"efi",
            @"efi\boot",
            @"efi\boot\bootx64.efi",
            @"sources\boot.wim",
            @"autounattend.xml",
            @"MEDIA_MARKER.txt"
        ];

      foreach (string relativePath in requiredPaths) {
        string fullPath = Path.Combine(
            usbRoot,
            relativePath
        );

        bool exists =
            Directory.Exists(fullPath)
            || File.Exists(fullPath);

        if (exists) {
          result.Logs.Add(
              LogLine("[OK] " + relativePath)
          );
        }
        else {
          result.Success = false;

          result.Errors.Add(
              LogLine("[MISSING] " + relativePath)
          );
        }
      }

      bool hasInstallImage =
          File.Exists(Path.Combine(
              usbRoot,
              @"sources\install.wim"))
          ||
          File.Exists(Path.Combine(
              usbRoot,
              @"sources\install.esd"))
          ||
          File.Exists(Path.Combine(
              usbRoot,
              @"sources\install.swm"));

      if (hasInstallImage) {
        result.Logs.Add(
            LogLine("[OK] Windows image found")
        );
      }
      else {
        result.Success = false;

        result.Errors.Add(
            LogLine("[MISSING] install.wim/esd/swm")
        );
      }

      if (result.Errors.Count == 0) {
        result.Success = true;

        result.Logs.Add(
            LogLine("UEFI validation successful")
        );
      }

      return result;
    }
    catch (Exception ex) {
      result.Success = false;
      result.Errors.Add(ex.ToString());

      return result;
    }
  }

  public static UsbPreparationResult DryRunPrepareUsb(DeploymentProfile profile) {
    UsbPreparationResult result = new();

    try {
      result.Logs.Add(LogLine("=== USB PREPARATION DRY RUN ==="));

      string mediaPath = profile.WindowsMedia.MediaFolder;

      result.Logs.Add(LogLine("Media folder : " + mediaPath));

      if (!Directory.Exists(mediaPath)) {
        result.Errors.Add(LogLine(
            "Media folder not found."
        ));

        return result;
      }

      ValidateMediaStructure(mediaPath,result);

      result.Logs.Add(LogLine(""));
      result.Logs.Add(LogLine("=== SELECTED USB TARGET ==="));

      string usbLetter =
          profile.WindowsMedia.UsbDriveLetter;

      result.Logs.Add(LogLine("Selected USB : " + usbLetter));

      if (string.IsNullOrWhiteSpace(usbLetter)) {
        result.Errors.Add(LogLine("No USB drive selected."));
      }
      else {
        DriveInfo drive = new(usbLetter);

        if (!drive.IsReady) {
          result.Errors.Add(LogLine("USB drive is not ready."));
        }
        else {
          result.Logs.Add(LogLine("Volume label : " + drive.VolumeLabel));
          result.Logs.Add(LogLine("File system  : " + drive.DriveFormat));
          result.Logs.Add(LogLine("Free space   : " + FormatBytes((ulong)drive.AvailableFreeSpace)));
          result.Logs.Add(LogLine("Total size   : " + FormatBytes((ulong)drive.TotalSize)));

          ulong mediaSize =
              GetDirectorySize(mediaPath);

          result.Logs.Add(LogLine("Media size   : " + FormatBytes(mediaSize)));

          if ((ulong)drive.AvailableFreeSpace < mediaSize)
            result.Errors.Add(LogLine("Not enough free space on USB drive."));
          else
            result.Logs.Add(LogLine("[OK] USB has enough free space."));
        }
      }

      result.Logs.Add(LogLine(""));
      result.Logs.Add(LogLine("=== PLANNED ACTIONS ==="));
      result.Logs.Add(LogLine("1. Validate WindowsMedia folder"));
      result.Logs.Add(LogLine("2. Validate selected USB drive"));
      result.Logs.Add(LogLine("3. Future: format USB drive"));
      result.Logs.Add(LogLine("4. Future: copy WindowsMedia content to USB"));
      result.Logs.Add(LogLine("5. Future: verify USB content"));

      result.Success = result.Errors.Count == 0;

      result.Logs.Add(result.Success ? "Dry run completed successfully." : "Dry run completed with errors.");

      return result;
    }
    catch (Exception ex) {
      result.Errors.Add(LogLine(ex.Message));
      return result;
    }
  }

  public UsbPreparationResult SplitInstallWimIfNeeded(string mediaFolder,string fileSystem,bool splitIfNeeded) {
    UsbPreparationResult result = new();

    try {
      ProgressCallback?.Invoke(0,"Split install.wim...");
      if (!fileSystem.Equals("FAT32",StringComparison.OrdinalIgnoreCase)) {
        result.Success = true;
        result.Logs.Add(LogLine("WIM split skipped: filesystem is not FAT32."));
        return result;
      }

      if (!splitIfNeeded) {
        result.Success = true;
        result.Logs.Add(LogLine("WIM split skipped by user option."));
        return result;
      }

      string sourcesFolder = Path.Combine(mediaFolder,"sources");
      string installWim = Path.Combine(sourcesFolder,"install.wim");

      if (!File.Exists(installWim)) {
        result.Success = true;
        result.Logs.Add(LogLine("WIM split skipped: install.wim not found."));
        return result;
      }

      long maxFat32FileSize = 4L * 1024 * 1024 * 1024;

      if (new FileInfo(installWim).Length < maxFat32FileSize) {
        result.Success = true;
        result.Logs.Add(LogLine("WIM split skipped: install.wim is smaller than 4 GB."));
        return result;
      }

      result.Logs.Add(LogLine("=== SPLIT INSTALL.WIM START ==="));

      ProgressCallback?.Invoke(20,"Split install.wim...");

      string installSwm = Path.Combine(sourcesFolder,"install.swm");

      foreach (string oldSwm in Directory.GetFiles(sourcesFolder,"install*.swm"))
        File.Delete(oldSwm);

      ProcessStartInfo psi = new() {
        FileName = "dism.exe",
        Arguments = $"/Split-Image /ImageFile:\"{installWim}\" /SWMFile:\"{installSwm}\" /FileSize:3800",
        UseShellExecute = false,
        CreateNoWindow = true,
        RedirectStandardOutput = true,
        RedirectStandardError = true,

        StandardOutputEncoding = Encoding.GetEncoding(850),
        StandardErrorEncoding = Encoding.GetEncoding(850)
      };

      using Process process = Process.Start(psi)!;

      string output = process.StandardOutput.ReadToEnd();
      string error = process.StandardError.ReadToEnd();

      process.WaitForExit();

      if (!string.IsNullOrWhiteSpace(output))
        result.Logs.Add(output);

      if (!string.IsNullOrWhiteSpace(error))
        result.Errors.Add(error);

      if (process.ExitCode != 0) {
        result.Success = false;
        result.Errors.Add(LogLine("DISM split failed with exit code : " + process.ExitCode));
        return result;
      }

      result.Logs.Add(
          LogLine("install.wim split to install.swm successfully.")
      );

      result.Logs.Add(
          LogLine("=== DELETE ORIGINAL INSTALL.WIM ===")
      );

      try {
        if (File.Exists(installWim)) {
          File.SetAttributes(
              installWim,
              FileAttributes.Normal
          );

          ProgressCallback?.Invoke(80,"Deleting original install.wim...");

          File.Delete(installWim);

          result.Logs.Add(
              LogLine("install.wim deleted after split.")
          );
        }
        else {
          result.Logs.Add(
              LogLine("install.wim not found after split deletion check.")
          );
        }
      }
      catch (Exception deleteEx) {
        result.Success = false;

        result.Errors.Add(
            LogLine("Failed to delete install.wim after split.")
        );

        result.Errors.Add(
            LogLine(deleteEx.ToString())
        );

        return result;
      }

      result.Success = true;

      result.Logs.Add(
          LogLine("=== SPLIT INSTALL.WIM SUCCESS ===")
      );
      ProgressCallback?.Invoke(100,"Split completed.");
      return result;
    }
    catch (Exception ex) {
      result.Success = false;
      result.Errors.Add(LogLine(ex.ToString()));
      return result;
    }
  }

  private static void ValidateInstallImage(
      string root,
      UsbPreparationResult result) {
    string sources =
        Path.Combine(root,"sources");

    string installWim =
        Path.Combine(sources,"install.wim");

    string installEsd =
        Path.Combine(sources,"install.esd");

    string installSwm =
        Path.Combine(sources,"install.swm");

    if (File.Exists(installWim)) {
      result.Logs.Add(LogLine("[OK] install.wim"));
      return;
    }

    if (File.Exists(installEsd)) {
      result.Logs.Add(LogLine("[OK] install.esd"));
      return;
    }

    if (File.Exists(installSwm)) {
      result.Logs.Add(LogLine("[OK] install.swm"));
      return;
    }

    result.Errors.Add(
        LogLine("[MISSING] install.wim, install.esd or install.swm")
    );
  }

  private static void ValidateUsbPath(
    string root,
    string relativePath,
    string label,
    UsbPreparationResult result) {
    string path = Path.Combine(root,relativePath);

    if (File.Exists(path) || Directory.Exists(path)) {
      result.Logs.Add(LogLine("[OK] " + label));
    }
    else {
      result.Errors.Add(LogLine("[MISSING] " + label));
    }
  }

  public static UsbPreparationResult ValidatePreparedUsb(string usbDriveLetter) {
    UsbPreparationResult result = new();

    try {
      string root = usbDriveLetter.TrimEnd('\\') + "\\";

      result.Logs.Add(LogLine("=== USB FINAL VALIDATION ==="));
      result.Logs.Add(LogLine("USB root : " + root));

      ValidateUsbPath(root,"boot","boot folder",result);
      ValidateUsbPath(root,"efi","efi folder",result);
      ValidateUsbPath(root,"sources","sources folder",result);

      ValidateUsbPath(root,@"sources\boot.wim","boot.wim",result);
      ValidateInstallImage(root,result);
      ValidateUsbPath(root,"Deploy","Deploy folder",result);
      ValidateUsbPath(root,"autounattend.xml","autounattend.xml",result);
      ValidateUsbPath(root,"MEDIA_MARKER.txt","MEDIA_MARKER.txt",result);

      result.Success = result.Errors.Count == 0;

      result.Logs.Add(
          LogLine(
              result.Success
                  ? "USB media validation successful."
                  : "USB media validation failed."
          )
      );

      return result;
    }
    catch (Exception ex) {
      result.Success = false;
      result.Errors.Add(LogLine(ex.ToString()));
      return result;
    }
  }


  private static string NormalizeDriveRoot(string drive) {
    if (string.IsNullOrWhiteSpace(drive))
      return drive;

    drive = drive.Trim();

    if (drive.EndsWith('\\'))
      return drive;

    if (drive.EndsWith(':'))
      return drive + @"\";

    return drive;
  }

  public static UsbPreparationResult InstallBootSector(
    string bootsectRoot,
    string usbDriveLetter) {
    UsbPreparationResult result = new();

    try {
      result.Logs.Add(
          LogLine("=== INSTALL BOOT SECTOR ===")
      );

      string usbRoot =
          NormalizeDriveRoot(usbDriveLetter);

      string bootsectPath =
          Path.Combine(
              NormalizeDriveRoot(bootsectRoot),
              "boot",
              "bootsect.exe"
          );

      if (!File.Exists(bootsectPath)) {
        result.Success = false;
        result.Errors.Add(
            LogLine("bootsect.exe not found : " + bootsectPath)
        );

        return result;
      }

      string arguments =
          $"/nt60 {usbRoot.TrimEnd('\\')} /force /mbr";

      result.Logs.Add(
          LogLine("bootsect : " + bootsectPath)
      );

      result.Logs.Add(
          LogLine("args     : " + arguments)
      );

      ProcessStartInfo psi = new() {
        FileName = bootsectPath,
        Arguments = arguments,
        UseShellExecute = false,
        CreateNoWindow = true,
        RedirectStandardOutput = true,
        RedirectStandardError = true,

        StandardOutputEncoding = Encoding.GetEncoding(850),
        StandardErrorEncoding = Encoding.GetEncoding(850)
      };

      using Process process = Process.Start(psi)!;

      string stdout =
          process.StandardOutput.ReadToEnd();

      string stderr =
          process.StandardError.ReadToEnd();

      process.WaitForExit();

      if (!string.IsNullOrWhiteSpace(stdout))
        result.Logs.Add(stdout);

      if (!string.IsNullOrWhiteSpace(stderr))
        result.Errors.Add(stderr);

      result.Logs.Add(
          LogLine("bootsect exit code : " + process.ExitCode)
      );

      result.Success =
          process.ExitCode == 0;

      return result;
    }
    catch (Exception ex) {
      result.Success = false;
      result.Errors.Add(
          LogLine(ex.ToString())
      );

      return result;
    }
  }

  private static string LogLine(string message) {
    return $"[{DateTime.Now:HH:mm:ss}] {message}";
  }

  public UsbPreparationResult FormatUsbMbrNtfs(
      UsbDeviceInfo usbDevice,
      string partitionType,
      string fileSystem,
      string volumeLabel,
      string targetSystem) {
    UsbPreparationResult result = new();

    try {
      result.Logs.Add(LogLine("=== USB FORMAT START ==="));
      result.Logs.Add(LogLine("Disk      : " + usbDevice.DiskNumber));
      result.Logs.Add(LogLine("Letter    : " + usbDevice.DriveLetters));
      result.Logs.Add(LogLine("Partition : " + partitionType));
      result.Logs.Add(LogLine("FileSystem: " + fileSystem));
      result.Logs.Add(LogLine("Target    : " + targetSystem));
      result.Logs.Add(LogLine("Label     : " + volumeLabel));

      ProgressCallback?.Invoke(10,"Preparing diskpart script...");

      string script = GenerateDiskpartScript(
          usbDevice,
          partitionType,
          fileSystem,
          volumeLabel,
          targetSystem
      );

      result.Logs.Add(LogLine(""));
      result.Logs.Add(LogLine("=== DISKPART SCRIPT ==="));
      result.Logs.Add(script);

      string scriptPath = Path.Combine(
          Path.GetTempPath(),
          "deploy_usb_diskpart.txt"
      );

      File.WriteAllText(
          scriptPath,
          script,
          EncodingHelper.Utf8NoBom
      );

      ProcessStartInfo psi = new() {
        FileName = "diskpart.exe",
        Arguments = $"/s \"{scriptPath}\"",
        UseShellExecute = false,
        CreateNoWindow = true,
        RedirectStandardOutput = true,
        RedirectStandardError = true,

        StandardOutputEncoding = Encoding.GetEncoding(850),
        StandardErrorEncoding = Encoding.GetEncoding(850)
      };
      ProgressCallback?.Invoke(40,"Formatting USB...");
      using Process process = Process.Start(psi)!;
      ProgressCallback?.Invoke(100,"USB formatted.");
      string output = process.StandardOutput.ReadToEnd();
      string error = process.StandardError.ReadToEnd();

      process.WaitForExit();

      if (!string.IsNullOrWhiteSpace(output))
        result.Logs.Add(output);

      if (!string.IsNullOrWhiteSpace(error))
        result.Errors.Add(LogLine(error));

      if (process.ExitCode != 0) {
        result.Success = false;
        result.Errors.Add(LogLine(
            "DiskPart failed with exit code : " +
            process.ExitCode
        ));

        return result;
      }

      result.Success = true;
      result.Logs.Add(LogLine("=== USB FORMAT SUCCESS ==="));

      return result;
    }
    catch (Exception ex) {
      result.Success = false;
      result.Errors.Add(LogLine(ex.ToString()));
      return result;
    }
  }

  public int ProgressOffset { get; set; } = 0;
  public int ProgressSpan { get; set; } = 100;

  public static string GenerateDiskpartScript(
      UsbDeviceInfo usbDevice,
      string partitionType,
      string fileSystem,
      string volumeLabel,
      string targetSystem) {
    StringBuilder sb = new();

    string driveLetter =
        usbDevice.DriveLetters.Replace(":","").Replace("\\","").Trim();

    sb.AppendLine($"select disk {usbDevice.DiskNumber}");
    sb.AppendLine("clean");

    if (partitionType.Equals("GPT",StringComparison.OrdinalIgnoreCase)) {
      sb.AppendLine("convert gpt");
      sb.AppendLine("create partition primary");
      sb.AppendLine($"format fs={fileSystem.ToLower()} quick label=\"{volumeLabel}\"");
      sb.AppendLine($"assign letter={driveLetter}");
    }
    else {
      sb.AppendLine("convert mbr");
      sb.AppendLine("create partition primary");
      sb.AppendLine($"format fs={fileSystem.ToLower()} quick label=\"{volumeLabel}\"");

      if (targetSystem.Contains("BIOS",StringComparison.OrdinalIgnoreCase))
        sb.AppendLine("active");

      sb.AppendLine($"assign letter={driveLetter}");
    }

    sb.AppendLine("exit");

    return sb.ToString();
  }

  public UsbPreparationResult ExtractIsoToUsbWith7Zip(
      string isoPath,
      string usbDriveLetter,
      CancellationToken cancellationToken) {
    UsbPreparationResult result = new();

    try {
      result.Logs.Add(LogLine("=== EXTRACT ISO TO USB START ==="));
      result.Logs.Add(LogLine("ISO    : " + isoPath));
      result.Logs.Add(LogLine("Target : " + usbDriveLetter));

      string targetRoot = usbDriveLetter.TrimEnd('\\');

      PipelineLogger log = new();

      BuildPipelineService.ExtractArchiveWith7ZipProgress(
          source: isoPath,
          destination: targetRoot,
          log: log,
          progressStart: 0,
          progressSpan: 100,
          progressLabel: "Extraction ISO vers USB",
          reportProgress: (percent,message) => ProgressCallback?.Invoke(percent,message),
          liveLog: line => {
            result.Logs.Add(line);
            LiveLogCallback?.Invoke(line);
          },
          cancellationToken: cancellationToken
      );
      result.Logs.AddRange(log.Lines);
      result.Success = true;
      result.Logs.Add(LogLine("=== EXTRACT ISO TO USB SUCCESS ==="));

      return result;
    }
    catch (OperationCanceledException) {
      result.Success = false;
      result.Errors.Add(LogLine("Extraction ISO cancelled."));
      throw;
    }
    catch (Exception ex) {
      result.Success = false;
      result.Errors.Add(LogLine(ex.ToString()));
      return result;
    }
  }
  private static ulong GetDirectorySize(string path) {
    ulong size = 0;

    foreach (string file in Directory.GetFiles(
                 path,
                 "*",
                 SearchOption.AllDirectories)) {
      try {
        size += (ulong)new FileInfo(file).Length;
      }
      catch {
      }
    }

    return size;
  }

  public static List<UsbDeviceInfo> DetectUsbDevices() {
    List<UsbDeviceInfo> devices = [];

    using ManagementObjectSearcher searcher = new(
        "SELECT * FROM Win32_DiskDrive WHERE InterfaceType='USB'"
    );

    foreach (ManagementObject disk in searcher.Get().Cast<ManagementObject>()) {
      string deviceId = disk["DeviceID"]?.ToString() ?? "";
      string model = disk["Model"]?.ToString() ?? "";
      string interfaceType = disk["InterfaceType"]?.ToString() ?? "";

      ulong sizeBytes = 0;

      if (disk["Size"] != null)
        _ = ulong.TryParse(disk["Size"].ToString(),out sizeBytes);

      int diskNumber = ExtractDiskNumber(deviceId);

      UsbDeviceInfo info = new() {
        DiskNumber = diskNumber,
        Model = model,
        InterfaceType = interfaceType,
        SizeBytes = sizeBytes,
        SizeText = FormatBytes(sizeBytes)
      };

      FillVolumes(info,deviceId);

      devices.Add(info);
    }

    return [.. devices.OrderBy(x => x.DiskNumber)];
  }

  private static int ExtractDiskNumber(string deviceId) {
    // Exemple : \\.\PHYSICALDRIVE2
    string digits = new([.. deviceId.Where(char.IsDigit)]);

    return int.TryParse(digits,out int number)
        ? number
        : -1;
  }

  private static string FormatBytes(ulong bytes) {
    if (bytes == 0)
      return "Unknown";

    double gb = bytes / 1024d / 1024d / 1024d;

    return $"{gb:0.##} GB";
  }

  private static void FillVolumes(
      UsbDeviceInfo info,
      string deviceId) {
    List<string> letters = [];
    List<string> labels = [];

    try {
      string escapedDeviceId = deviceId.Replace("\\","\\\\");

      using ManagementObjectSearcher partitionSearcher = new(
          $"ASSOCIATORS OF {{Win32_DiskDrive.DeviceID='{escapedDeviceId}'}} WHERE AssocClass=Win32_DiskDriveToDiskPartition"
      );

      foreach (ManagementObject partition in partitionSearcher.Get().Cast<ManagementObject>()) {
        string partitionId = partition["DeviceID"]?.ToString() ?? "";

        string escapedPartitionId = partitionId.Replace("\\","\\\\");

        using ManagementObjectSearcher logicalSearcher = new(
            $"ASSOCIATORS OF {{Win32_DiskPartition.DeviceID='{escapedPartitionId}'}} WHERE AssocClass=Win32_LogicalDiskToPartition"
        );

        foreach (ManagementObject logicalDisk in logicalSearcher.Get().Cast<ManagementObject>()) {
          string letter =
              logicalDisk["DeviceID"]?.ToString() ?? "";

          string label = logicalDisk["VolumeName"]?.ToString() ?? "";

          if (!string.IsNullOrWhiteSpace(letter))
            letters.Add(letter);

          if (!string.IsNullOrWhiteSpace(label))
            labels.Add(label);
        }
      }
    }
    catch {
      // Ne surtout pas ajouter tous les lecteurs amovibles ici.
      // Sinon chaque clé USB reçoit F:, J:, K:, etc.
    }

    info.DriveLetters =
        string.Join(", ",letters.Distinct());

    info.VolumeLabels =
        string.Join(", ",labels.Distinct());

    if (letters.Count > 0) {
      info.DriveLetters = string.Join(", ",letters.Distinct());
      info.VolumeLabels = string.Join(", ",labels.Distinct());
    }
    else {
      FillVolumesByDiskNumber(info);
    }
  }

  private static void FillVolumesByDiskNumber(UsbDeviceInfo info) {
    List<string> letters = [];
    List<string> labels = [];

    using ManagementObjectSearcher partitionSearcher = new(
        $"SELECT * FROM Win32_DiskPartition WHERE DiskIndex={info.DiskNumber}"
    );

    foreach (ManagementObject partition in partitionSearcher.Get().Cast<ManagementObject>()) {
      string partitionId = partition["DeviceID"]?.ToString() ?? "";
      string escapedPartitionId = partitionId.Replace("\\","\\\\");

      using ManagementObjectSearcher logicalSearcher = new(
          $"ASSOCIATORS OF {{Win32_DiskPartition.DeviceID='{escapedPartitionId}'}} WHERE AssocClass=Win32_LogicalDiskToPartition"
      );

      foreach (ManagementObject logicalDisk in logicalSearcher.Get().Cast<ManagementObject>()) {
        string letter = logicalDisk["DeviceID"]?.ToString() ?? "";
        string label = logicalDisk["VolumeName"]?.ToString() ?? "";

        if (!string.IsNullOrWhiteSpace(letter))
          letters.Add(letter);

        if (!string.IsNullOrWhiteSpace(label))
          labels.Add(label);
      }
    }

    info.DriveLetters = string.Join(", ",letters.Distinct());
    info.VolumeLabels = string.Join(", ",labels.Distinct());
  }

  private static void ValidateMediaStructure(
      string mediaPath,
      UsbPreparationResult result) {
    ValidatePath(
        Path.Combine(mediaPath,"boot"),
        "boot folder",
        result
    );

    ValidatePath(
        Path.Combine(mediaPath,"efi"),
        "efi folder",
        result
    );

    ValidatePath(
        Path.Combine(mediaPath,"sources"),
        "sources folder",
        result
    );

    ValidatePath(
        Path.Combine(mediaPath,"Deploy"),
        "Deploy folder",
        result
    );

    ValidatePath(
        Path.Combine(mediaPath,"autounattend.xml"),
        "autounattend.xml",
        result
    );
  }

  private static void ValidatePath(
      string path,
      string label,
      UsbPreparationResult result) {
    bool exists =
        Directory.Exists(path) ||
        File.Exists(path);

    if (exists) {
      result.Logs.Add(
          "[OK] " + label
      );
    }
    else {
      result.Errors.Add(LogLine(
          "[MISSING] " + label
      ));
    }
  }
}