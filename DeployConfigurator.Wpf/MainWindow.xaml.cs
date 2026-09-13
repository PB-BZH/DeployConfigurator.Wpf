using System.Diagnostics;
using System.Drawing.Printing;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DeployConfigurator.Wpf.Core.Builders;
using DeployConfigurator.Wpf.Core.Enums;
using DeployConfigurator.Wpf.Core.Models;
using DeployConfigurator.Wpf.Core.Pipeline;
using DeployConfigurator.Wpf.Core.Services;
using DeployConfigurator.Wpf.UI.Controls;
using DeployConfigurator.Wpf.UI.Helpers;
using DeployConfigurator.Wpf.UI.Windows;
using Microsoft.Win32;
using PB.BZH.Help.Wpf.UI.Theming;
using PB.BZH.Licensing.Core.Services;
using Brushes = System.Windows.Media.Brushes;
using Path = System.IO.Path;

namespace DeployConfigurator.Wpf;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow: Window {
  #region Fieldspublic MainForm() {
  private DeploymentProfile _profile = new();
  private List<PhysicalDiskInfo> _detectedDisks = [];

  private bool _livePreviewReady;
  private bool _isUpdatingPreview;
  private bool _isHighlighting;
  private bool _hasValidationError;

  private string _currentProfilePath = "";
  private string _fullPreviewText = "";
  private ToolTip _toolTip = new();

  private readonly HashSet<string> _collapsedSections = [];

  private PrintDocument? _printDocument;
  private string[] _printLines = [];
  private int _currentPrintLine;
  private CancellationTokenSource? _buildCancellation;

  private PreviewDocumentType _activePreviewDocument = PreviewDocumentType.Diskprep;
  private int _refreshCount;
  private bool _isRefreshingPreview;
  private readonly ManualResetEventSlim _pauseEvent = new(true);
  private bool _isPaused;
  private bool _pipelineHasWarning;
  private readonly Dictionary<string,RichTextBox> _previewEditors = [];
  private readonly Dictionary<RichTextBox,string> _editorFileNames = [];

  private bool _orchestrationPendingChanges;
  private readonly Dictionary<string,(int Start,int Span)>
  _pipelineProgressMap =
      new() {
        ["01"] = (0,5),
        ["02"] = (5,10),
        ["03"] = (15,25),
        ["04"] = (40,10),
        ["05"] = (50,15),
        ["06"] = (65,5),
        ["07"] = (70,5),
        ["08"] = (75,20),
        ["09"] = (95,3),
        ["10"] = (98,2),
      };
  private BuildPipelineService? _pipeline;
  private readonly List<PipelineStepLog> _pipelineSteps = [];
  private string _usbLiveReportPath = "";
  private readonly Lock _usbLiveReportLock = new();
  private int _currentStepProgressStart;
  private int _currentStepProgressSpan;

  private const string WebCategoryFolderName = "msi-software-packager";
  private readonly LicenseService _licenseService;
  private readonly PreviewSyntaxHighlighter _syntaxHighlighter = new();


  #endregion

  public MainWindow() {
    InitializeComponent();
    _licenseService = LicenseHelper.CreerLicenseService(_profile);

    ThemeMode.IsChecked = true;
    DarkTheme();

    #region UI Initialization

    LoadKeyboardLocales();
    PreviewKeyDown += MainForm_KeyDown;
    txtSearch.KeyDown += txtSearch_KeyDown;
    LoadDisks();
    LoadUsbDriveLetters();
    HookLivePreviewEvents();
    UpdateUiState();
    _livePreviewReady = true;
    ValidateConfiguration();
    RefreshPreview();
    UpdateStatusBar();
    picOperation.Source = LoadImageResource("gear.png");
    imgPauseResume.Source = LoadImageResource("ready.png");
    #endregion
  }
  #region élements ajoutés
  // ---------------------
  private void MainForm_KeyDown(object? sender,KeyEventArgs e) {
    if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && e.Key == Key.F) {
      txtSearch.Focus();
      txtSearch.SelectAll();
      e.Handled = true;
      return;
    }
    if (e.Key == Key.Escape) {
      txtSearch.Clear();
      CurrentPreviewTextBox.Focus();
      e.Handled = true;
    }
  }

  // ============================================================
  // OUVERTURE D'UN APERÇU DANS NOTEPAD++
  // ============================================================

  private void Preview_OpenInNotepadPlusPlusRequested(object? sender,EventArgs e) {

    if (sender is not PreviewEditor preview)
      return;

    string fileName;

    if (preview == previewDiskPrep)
      fileName = "diskprep.cmd";
    else if (preview == previewSetupComplete)
      fileName = "SetupComplete.cmd";
    else if (preview == previewOrchestrator_resume)
      fileName = "Orchestrator_resume.cmd";
    else if (preview == previewAutoUnattend)
      fileName = "autounattend.xml";
    else if (preview == previewUnattend)
      fileName = "unattend.xml";
    else if (preview == previewProfileDeployJson)
      fileName = "profile.deploy.json";
    else if (preview == previewprofileBuildReport)
      fileName = "BuildReport.txt";
    else if (preview == previewBuildLog)
      fileName = "BuildLog.txt";
    else
      return;

    OpenTextInNotepadPlusPlus(GetRichText(preview.Editor),fileName);
  }

  private void DarkTheme() {

    ThemeManager.SetTheme(
      ThemeMode.IsChecked
        ? AppTheme.Dark
        : AppTheme.Light);

    lblValidation.Foreground = Brushes.Red;
    lblOrchestrationPending.Foreground = Brushes.Orange;
  }


  private void FindNext() {
    RichTextBox textBox = CurrentPreviewTextBox;
    string search = txtSearch.Text;
    if (string.IsNullOrWhiteSpace(search))
      return;
    string text = NormalizeRichText(GetRichText(textBox));
    int start = NormalizeRichText(new TextRange(textBox.Document.ContentStart,textBox.Selection.End).Text).Length;
    int index = text.IndexOf(search,start,StringComparison.OrdinalIgnoreCase);

    // Retour au début du document
    if (index < 0) {
      index = text.IndexOf(search,0,StringComparison.OrdinalIgnoreCase);
    }
    if (index < 0)
      return;
    TextPointer? selectionStart = GetTextPointerAtOffset(textBox,index);
    TextPointer? selectionEnd = GetTextPointerAtOffset(textBox,index + search.Length);
    if (selectionStart is null || selectionEnd is null) {
      return;
    }
    textBox.Selection.Select(selectionStart,selectionEnd);
    textBox.CaretPosition = selectionEnd;
    textBox.Focus();
    textBox.UpdateLayout();
    Rect position = selectionStart.GetCharacterRect(LogicalDirection.Forward);
    textBox.ScrollToVerticalOffset(Math.Max(0,textBox.VerticalOffset + position.Top - textBox.ViewportHeight / 2));
  }

  private static string NormalizeRichText(
  string text) {

    return text
      .Replace("\r\n","\n")
      .Replace('\r','\n');
  }

  private static TextPointer? GetTextPointerAtOffset(RichTextBox textBox,int offset) {
    TextPointer? pointer = textBox.Document.ContentStart.GetInsertionPosition(LogicalDirection.Forward);
    int currentOffset = 0;
    while (pointer is not null && currentOffset < offset) {
      TextPointer? next = pointer.GetNextInsertionPosition(LogicalDirection.Forward);
      if (next is null)
        break;
      pointer = next;
      currentOffset++;
    }
    return pointer;
  }


  private void txtSearch_KeyDown(object? sender,KeyEventArgs e) {
    if (e.Key == Key.Enter) {
      FindNext();
      e.Handled = true;
      return;
    }
    if (e.Key == Key.Escape) {
      txtSearch.Clear();
      CurrentPreviewTextBox.Focus();
      e.Handled = true;
    }
  }

  private void LoadUsbDriveLetters() {
    cmbUsbDriveLetter.Items.Clear();

    List<UsbDeviceInfo> devices =
        UsbPreparationService.DetectUsbDevices();

    foreach (UsbDeviceInfo device in devices) {
      cmbUsbDriveLetter.Items.Add(device);
    }

    if (cmbUsbDriveLetter.Items.Count > 0)
      cmbUsbDriveLetter.SelectedIndex = 0;
  }

  private readonly List<KeyboardLocaleOption> _keyboardLocales = [
  new KeyboardLocaleOption
    {
        DisplayName = "Français France - fr-FR",
        LanguageTag = "fr-FR",
        InputLocaleCode = "040c:0000040c"
    },
    new KeyboardLocaleOption
    {
        DisplayName = "Anglais US - en-US",
        LanguageTag = "en-US",
        InputLocaleCode = "0409:00000409"
    },
    new KeyboardLocaleOption
    {
        DisplayName = "Allemand Allemagne - de-DE",
        LanguageTag = "de-DE",
        InputLocaleCode = "0407:00000407"
    },
    new KeyboardLocaleOption
    {
        DisplayName = "Espagnol Espagne - es-ES",
        LanguageTag = "es-ES",
        InputLocaleCode = "0c0a:0000040a"
    }
];


  private void LoadDisks() {
    _detectedDisks = DiskDetectionService.GetDisks();

    cmbOsDisk.ItemsSource = new List<PhysicalDiskInfo>(_detectedDisks);
    cmbData1Disk.ItemsSource = new List<PhysicalDiskInfo>(_detectedDisks);
    cmbData2Disk.ItemsSource = new List<PhysicalDiskInfo>(_detectedDisks);
  }


  private void HookConfigurationChanged(
  CheckBox checkBox) {

    checkBox.Checked +=
      ConfigurationChanged;

    checkBox.Unchecked +=
      ConfigurationChanged;
  }

  private void MarkOrchestrationPending() {
    _orchestrationPendingChanges = true;

    lblOrchestrationPending.Content = "\u25CF Changes pending use [ Apply All ] to apply";
    lblOrchestrationPending.Foreground = Brushes.Orange;
    lblOrchestrationPending.Background = Brushes.Transparent;
    lblOrchestrationPending.Visibility = Visibility.Visible;
    lblOrchestrationPending.FontFamily = new System.Windows.Media.FontFamily("Segoe UI");
    lblOrchestrationPending.FontSize = 8.5;
    lblOrchestrationPending.FontWeight = FontWeights.Bold;
  }

  private void HookOrchestrationPending(CheckBox checkBox) {
    checkBox.Checked += (_,_) => MarkOrchestrationPending();
    checkBox.Unchecked += (_,_) => MarkOrchestrationPending();
  }

  private void HookLivePreviewEvents() {

    // ============================================================
    // STORAGE
    // ============================================================

    HookConfigurationChanged(chkSeparateDataDisk);
    HookConfigurationChanged(chkCreateData1);
    HookConfigurationChanged(chkCreateData2);

    cmbOsDisk.SelectionChanged += ConfigurationChanged;
    cmbData1Disk.SelectionChanged += ConfigurationChanged;
    cmbData2Disk.SelectionChanged += ConfigurationChanged;
    cmbImageType.SelectionChanged += ConfigurationChanged;

    // ============================================================
    // AUTOUNATTEND / UNATTEND
    // ============================================================

    cmbUiLanguage.SelectionChanged += ConfigurationChanged;
    cmbTimeZone.SelectionChanged += ConfigurationChanged;
    HookDeferredRefresh(txtOrganization);
    HookDeferredRefresh(txtOwner);
    numEfiSize.ValueChanged += ConfigurationChanged;
    numMsrSize.ValueChanged += ConfigurationChanged;
    numSystemBiosSize.ValueChanged += ConfigurationChanged;
    numWindowsSize.ValueChanged += ConfigurationChanged;
    numRecoverySize.ValueChanged += ConfigurationChanged;
    numImageIndex.ValueChanged += ConfigurationChanged;
    HookConfigurationChanged(chkCreateLocalAccount);
    cmbLocalUserGroup.SelectionChanged += ConfigurationChanged;
    HookConfigurationChanged(chkEnableAutoLogon);
    numAutoLogonCount.ValueChanged += ConfigurationChanged;
    HookDeferredRefresh(txtProductKey);
    HookDeferredRefresh(txtLocalUserName);
    HookDeferredRefresh(txtComputerName);


    // PasswordBox : ce n'est pas un TextBox
    txtAdminPassword.PasswordChanged += ConfigurationChanged;

    // ============================================================
    // PACKAGE
    // ============================================================

    HookConfigurationChanged(chkIncludeDrivers);
    HookConfigurationChanged(chkIncludeApplications);
    HookConfigurationChanged(chkIncludeScripts);
    HookConfigurationChanged(chkIncludeWinPE);
    HookConfigurationChanged(chkIncludeSetupScripts);
    HookConfigurationChanged(chkIncludeSetupConfig);
    HookDeferredRefresh(txtSetupScriptsSourcePath);
    HookDeferredRefresh(txtSetupConfigSourcePath);
    HookDeferredRefresh(txtPackageName);
    HookDeferredRefresh(txtPackageVersion);
    HookDeferredRefresh(txtPackageAuthor);
    HookDeferredRefresh(txtOutputDirectory);
    HookDeferredRefresh(txtDriversSourcePath);
    HookDeferredRefresh(txtApplicationsSourcePath);

    // ============================================================
    // POST-INSTALL ORCHESTRATION
    // ============================================================

    HookOrchestrationPending(chkRunDetachUsb);
    HookOrchestrationPending(chkRunReorgVolumes);
    HookOrchestrationPending(chkRunWifi);
    HookOrchestrationPending(chkRunDrivers);
    HookOrchestrationPending(chkRunWindowsUpdateDrivers);
    HookOrchestrationPending(chkRunSoftwares);
    HookOrchestrationPending(chkRunM365);
    HookOrchestrationPending(chkRunPostInstall);
    HookOrchestrationPending(chkRunCleanup);

    // ============================================================
    // PREVIEW
    // ============================================================

    TabMain.SelectionChanged += tabPreview_SelectedIndexChanged;
  }
  private static BitmapImage LoadImageResource(string fileName) {
    return new BitmapImage(new Uri($"/Ressources/{fileName}",UriKind.Relative));
  }

  private void HookDeferredRefresh(
    Control control) {

    if (control is TextBox txt) {

      txt.KeyDown += DeferredRefresh_KeyDown;

      txt.LostKeyboardFocus +=
        DeferredRefresh_LostKeyboardFocus;
    }

    else if (control is CheckBox chk) {

      chk.LostKeyboardFocus +=
        DeferredRefresh_LostKeyboardFocus;

      chk.KeyDown +=
        (s,e) => {

          if (e.Key == Key.Enter) {

            e.Handled = true;

            ConfigurationChanged(
              s,
              e);
          }
        };
    }

    else if (control is ComboBox cmb) {

      cmb.SelectionChanged +=
        ConfigurationChanged;
    }

    else if (control is NumericUpDown num) {

      num.ValueChanged +=
        ConfigurationChanged;
    }
  }

  private void DeferredRefresh_KeyDown(
  object sender,
  KeyEventArgs e) {

    if (e.Key != Key.Enter)
      return;

    e.Handled = true;

    ConfigurationChanged(
      sender,
      e);
  }

  private void DeferredRefresh_LostKeyboardFocus(
  object sender,
  KeyboardFocusChangedEventArgs e) {

    ConfigurationChanged(
      sender,
      e);
  }

  private void TestDiskPrepPreview() {

    string test =
      """
    @echo off
    :: Test du fichier diskprep.cmd

    echo Préparation du disque...
    set DISK=0

    if "%DISK%"=="0" goto PREPARE

    :PREPARE
    diskpart /s diskpart.txt

    echo Installation de l'image
    dism /Apply-Image /ImageFile:install.wim /Index:1 /ApplyDir:C:\

    bcdboot C:\Windows

    echo SUCCESS
    echo WARNING : ceci est un test
    echo ERROR : erreur simulée

    set RC=3010
    echo RC=%RC%
    echo ERRORLEVEL=%ERRORLEVEL%

    exit /b 0
    """;

    SetRichText(
      previewDiskPrep.Editor,
      test);

    _syntaxHighlighter.ApplyCmd(
      previewDiskPrep.Editor);
  }

  private void mnuThemeSombre_click(object sender,RoutedEventArgs e) {
    DarkTheme();
  }

  private void ApplyUiToProfile() {
    if (cmbOsDisk.SelectedItem is PhysicalDiskInfo osDisk)
      _profile.Disk.OsDisk = osDisk.DiskNumber;

    _profile.Disk.DataDisk =
        chkSeparateDataDisk.IsChecked == true && cmbData1Disk.SelectedItem is PhysicalDiskInfo dataDisk
            ? dataDisk.DiskNumber
            : null;

    _profile.Disk.Data2Disk =
        chkCreateData2.IsChecked == true && cmbData2Disk.SelectedItem is PhysicalDiskInfo data2Disk
            ? data2Disk.DiskNumber
            : null;

    _profile.Disk.CreateData1 = chkCreateData1.IsChecked == true;
    _profile.Disk.CreateData2 = chkCreateData2.IsChecked == true;

    if (chkCreateData2.IsChecked == true)
      _profile.Disk.LayoutMode = DiskLayoutMode.DualDataDisk;
    else if (chkSeparateDataDisk.IsChecked == true)
      _profile.Disk.LayoutMode = DiskLayoutMode.SeparateDataDisk;
    else
      _profile.Disk.LayoutMode = DiskLayoutMode.SingleDisk;

    _profile.Disk.EfiSizeMb = (int)numEfiSize.Value;
    _profile.Disk.MsrSizeMb = (int)numMsrSize.Value;
    _profile.Disk.SystemSizeMb = (int)numSystemBiosSize.Value;
    _profile.Disk.WindowsSizeMb = (int)numWindowsSize.Value;
    _profile.Disk.RecoverySizeMb = (int)numRecoverySize.Value;
    _profile.Disk.FirmwareMode = FirmwareMode.Auto;

    _profile.Unattend.CreateLocalAccount = chkCreateLocalAccount.IsChecked == true;
    _profile.Unattend.LocalUserName = txtLocalUserName.Text;
    _profile.Unattend.LocalUserPassword = txtAdminPassword.Password;

    _profile.Unattend.LocalUserGroup = cmbLocalUserGroup.SelectedItem?.ToString() ?? "Administrators";
    _profile.Unattend.ComputerName = string.IsNullOrWhiteSpace(txtComputerName.Text) ? "*" : txtComputerName.Text;
    _profile.Unattend.EnableAutoLogon = chkEnableAutoLogon.IsChecked == true;
    _profile.Unattend.AutoLogonCount = (int)numAutoLogonCount.Value;
    _profile.Unattend.UILanguageFallback = _profile.Unattend.UILanguage;

    var selectedLocales = lstInputLocales.SelectedItems
    .Cast<KeyboardLocaleOption>()
    .ToList();

    _profile.Unattend.InputLocale = string.Join(";",selectedLocales.Select(x => x.InputLocaleCode));
    _profile.Unattend.SystemLocale = string.Join(";",selectedLocales.Select(x => x.LanguageTag));
    _profile.Unattend.UILanguage = cmbUiLanguage.SelectedItem?.ToString() ?? "fr-FR";
    _profile.Unattend.UserLocale = _profile.Unattend.UILanguage;
    _profile.Unattend.UILanguageFallback = _profile.Unattend.UILanguage;

    _profile.Image.ImageType = cmbImageType.SelectedIndex switch {
      1 => WindowsImageType.Wim,
      2 => WindowsImageType.Esd,
      _ => WindowsImageType.Auto
    };

    _profile.Image.InstallImage = cmbImageType.SelectedIndex switch {
      1 => "install.wim",
      2 => "install.esd",
      _ => "auto"
    };

    _profile.Image.ImageIndex = (int)numImageIndex.Value;

    _profile.Unattend.ProductKey = txtProductKey.Text;

    _profile.Unattend.UILanguage = cmbUiLanguage.SelectedItem?.ToString() ?? "fr-FR";

    _profile.Unattend.UserLocale = cmbUiLanguage.SelectedItem?.ToString() ?? "fr-FR";

    _profile.Unattend.TimeZone = cmbTimeZone.SelectedItem?.ToString() ?? "Romance Standard Time";

    _profile.Unattend.Organization = txtOrganization.Text;
    _profile.Unattend.Owner = txtOwner.Text;

    _profile.Unattend.CreateLocalAccount = chkCreateLocalAccount.IsChecked == true;
    _profile.Unattend.LocalUserName = txtLocalUserName.Text;
    _profile.Unattend.LocalUserPassword = txtAdminPassword.Password;
    _profile.Unattend.LocalUserGroup = cmbLocalUserGroup.SelectedItem?.ToString() ?? "Administrators";

    _profile.Unattend.EnableAutoLogon = chkEnableAutoLogon.IsChecked == true;
    _profile.Unattend.AutoLogonCount = (int)numAutoLogonCount.Value;

    _profile.Unattend.InputLocaleCodes = [.. lstInputLocales.SelectedItems
      .Cast<KeyboardLocaleOption>()
      .Select(static x => x.InputLocaleCode)];

    _profile.Unattend.InputLocale = string.Join(";",_profile.Unattend.InputLocaleCodes);

    _profile.Unattend.UILanguage = cmbUiLanguage.SelectedItem?.ToString() ?? "fr-FR";

    _profile.Unattend.UserLocale = _profile.Unattend.UILanguage;

    _profile.Unattend.UILanguageFallback = _profile.Unattend.UILanguage;

    _profile.Package.PackageName = txtPackageName.Text;
    _profile.Package.Version = txtPackageVersion.Text;
    _profile.Package.Author = txtPackageAuthor.Text;
    _profile.Package.OutputDirectory = txtOutputDirectory.Text;

    _profile.Package.IncludeDrivers = chkIncludeDrivers.IsChecked == true;
    _profile.Package.IncludeApplications = chkIncludeApplications.IsChecked == true;
    _profile.Package.IncludeScripts = chkIncludeScripts.IsChecked == true;
    _profile.Package.IncludeWinPE = chkIncludeWinPE.IsChecked == true;

    _profile.Package.DriversSourcePath = txtDriversSourcePath.Text;
    _profile.Package.ApplicationsSourcePath = txtApplicationsSourcePath.Text;
    //_profile.Package.ScriptsSourcePath = txtScriptsSourcePath.Text;
    //_profile.Package.WinPESourcePath = txtWinPESourcePath.Text;
    _profile.Package.IncludeSetupScripts = chkIncludeSetupScripts.IsChecked == true;
    _profile.Package.IncludeSetupConfig = chkIncludeSetupConfig.IsChecked == true;
    _profile.Package.SetupScriptsSourcePath = txtSetupScriptsSourcePath.Text;
    _profile.Package.SetupConfigSourcePath = txtSetupConfigSourcePath.Text;

    _profile.Package.IncludeSetupScripts = chkIncludeSetupScripts.IsChecked == true;
    _profile.Package.IncludeSetupConfig = chkIncludeSetupConfig.IsChecked == true;

    _profile.Package.SetupScriptsSourcePath = txtSetupScriptsSourcePath.Text;
    _profile.Package.SetupConfigSourcePath = txtSetupConfigSourcePath.Text;

    _profile.WindowsMedia.WindowsIsoPath = txtWindowsIsoPath.Text;
    _profile.WindowsMedia.MediaFolder = txtMediaWorkingDirectory.Text;
    _profile.WindowsMedia.ExtractWindowsIso = chkExtractWindowsIso.IsChecked == true;
    _profile.WindowsMedia.InjectAutounattend = chkInjectAutounattend.IsChecked == true;
    _profile.WindowsMedia.InjectDeployFolder = chkInjectDeployFolder.IsChecked == true;
    _profile.WindowsMedia.PrepareUsbMedia = chkPrepareUsbMedia.IsChecked == true;
    if (cmbUsbDriveLetter.SelectedItem is UsbDeviceInfo usb) {
      _profile.WindowsMedia.UsbDriveLetter = usb.DriveLetters;
    }
    else {
      _profile.WindowsMedia.UsbDriveLetter = cmbUsbDriveLetter.Text;
    }
    _profile.WindowsMedia.BuildIso = chkBuildIso.IsChecked == true;
    _profile.WindowsMedia.IsoOutputPath = txtFinalIsoPath.Text;

    _profile.Orchestration.RunDetachUsb = chkRunDetachUsb.IsChecked == true;
    _profile.Orchestration.RunReorgVolumes = chkRunReorgVolumes.IsChecked == true;
    _profile.Orchestration.RunWifi = chkRunWifi.IsChecked == true;
    _profile.Orchestration.RunDrivers = chkRunDrivers.IsChecked == true;
    _profile.Orchestration.RunWindowsUpdateDrivers = chkRunWindowsUpdateDrivers.IsChecked == true;
    _profile.Orchestration.RunSoftwares = chkRunSoftwares.IsChecked == true;
    _profile.Orchestration.RunMicrosoft365 = chkRunM365.IsChecked == true;
    _profile.Orchestration.RunPostInstall = chkRunPostInstall.IsChecked == true;
    _profile.Orchestration.RunCleanup = chkRunCleanup.IsChecked == true;
  }

  private static void SelectComboValue(ComboBox comboBox,string value) {
    for (int i = 0;i < comboBox.Items.Count;i++) {
      string? itemValue = comboBox.Items[i] switch {
        ComboBoxItem item => item.Content?.ToString(),
        _ => comboBox.Items[i]?.ToString()
      };
      if (string.Equals(itemValue,value,StringComparison.OrdinalIgnoreCase)) {
        comboBox.SelectedIndex = i;
        return;
      }
    }
    if (comboBox.Items.Count > 0) {
      comboBox.SelectedIndex = 0;
    }
  }

  private void ConfigurationChanged(object? sender,RoutedEventArgs e) {
    Debug.WriteLine($"CONFIG CHANGE from: {(sender as Control)?.Name}");
    if (!_livePreviewReady)
      return;

    if (_isRefreshingPreview)
      return;

    try {
      _isRefreshingPreview = true;

      UpdateUiState();
      ValidateConfiguration();
      RefreshPreview();
      UpdateStatusBar();
    }
    finally {
      _isRefreshingPreview = false;
    }
  }

  private string GeneratePreviewDocument(PreviewDocumentType documentType) {
    string templatesRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Templates");

    if (documentType == PreviewDocumentType.BuildReport)
      return GenerateBuildReport();

    return PreviewDocumentBuilder.Generate(documentType,_profile,templatesRoot);
  }

  private static void AppendResourceStatus(StringBuilder sb,string name,bool enabled,string path) {
    if (!enabled) {
      sb.AppendLine($"[DISABLED] {name}");
      return;
    }

    if (string.IsNullOrWhiteSpace(path)) {
      sb.AppendLine($"[WARNING] {name} path empty");
      return;
    }

    if (!Directory.Exists(path)) {
      sb.AppendLine($"[ERROR] {name} path not found");
      return;
    }

    int files = Directory.GetFiles(
        path,
        "*",
        SearchOption.AllDirectories
    ).Length;

    sb.AppendLine($"[OK] {name}");
    sb.AppendLine($"     Path  : {path}");
    sb.AppendLine($"     Files : {files}");
  }

  private string GenerateBuildReport() {
    StringBuilder sb = new();

    sb.AppendLine("=== DEPLOY PACKAGE REPORT ===");
    sb.AppendLine();

    sb.AppendLine($"Package Name .......... {_profile.Package.PackageName}");
    sb.AppendLine($"Version ............... {_profile.Package.Version}");
    sb.AppendLine($"Author ................ {_profile.Package.Author}");
    sb.AppendLine();

    sb.AppendLine($"Output Directory ...... {_profile.Package.OutputDirectory}");
    sb.AppendLine();

    sb.AppendLine("=== INCLUDED RESOURCES ===");
    sb.AppendLine();

    AppendResourceStatus(
        sb,
        "Drivers",
        _profile.Package.IncludeDrivers,
        _profile.Package.DriversSourcePath
    );

    AppendResourceStatus(
        sb,
        "Applications",
        _profile.Package.IncludeApplications,
        _profile.Package.ApplicationsSourcePath
    );

    AppendResourceStatus(
        sb,
        "Setup Scripts",
        _profile.Package.IncludeSetupScripts,
        _profile.Package.SetupScriptsSourcePath
    );

    AppendResourceStatus(
        sb,
        "Setup Config",
        _profile.Package.IncludeSetupConfig,
        _profile.Package.SetupConfigSourcePath
    );

    AppendResourceStatus(
        sb,
        "WinPE",
        _profile.Package.IncludeWinPE,
        _profile.Package.WinPESourcePath
    );

    sb.AppendLine();
    sb.AppendLine("=== VALIDATION ===");
    sb.AppendLine();

    sb.AppendLine(
        string.IsNullOrWhiteSpace(_profile.Unattend.ProductKey)
            ? "[WARNING] Product Key empty"
            : "[OK] Product Key"
    );

    sb.AppendLine(
        string.IsNullOrWhiteSpace(_profile.Unattend.LocalUserPassword)
            ? "[WARNING] Local user password empty"
            : "[OK] Local user password"
    );
    sb.AppendLine();
    sb.AppendLine("=== PRE-BUILD VALIDATION ===");
    sb.AppendLine();

    BuildValidationService validation = new();

    List<ValidationIssue> issues = BuildValidationService.Validate(_profile);

    if (issues.Count == 0) {
      sb.AppendLine(
          "[OK] No validation issue detected."
      );
    }
    else {
      foreach (var group in issues.GroupBy(x => x.Category)) {
        sb.AppendLine($"--- {group.Key} ---");

        foreach (ValidationIssue issue in group) {
          sb.AppendLine(
              $"[{issue.Severity}] {issue.Message}"
          );
        }

        sb.AppendLine();
      }
    }
    sb.AppendLine();
    sb.AppendLine("=== BUILD STATUS ===");

    bool hasError =
        issues.Any(x => x.Severity == "ERROR");

    if (hasError) {
      sb.AppendLine(
          "[BLOCKED] Build cannot start."
      );
    }
    else {
      sb.AppendLine(
          "[READY] Build can start."
      );
    }

    return sb.ToString();
  }

  private RichTextBox CurrentPreviewTextBox => _activePreviewDocument switch {
    PreviewDocumentType.AutoUnattend => previewAutoUnattend.Editor,
    PreviewDocumentType.Unattend => previewUnattend.Editor,
    PreviewDocumentType.SetupComplete => previewSetupComplete.Editor,
    PreviewDocumentType.ProfileJson => previewProfileDeployJson.Editor,
    PreviewDocumentType.BuildReport => previewprofileBuildReport.Editor,
    PreviewDocumentType.Orchestrator => previewOrchestrator_resume.Editor,
    _ => previewDiskPrep.Editor
  };

  [GeneratedRegex(@"^\d{1,3}%")]
  private static partial Regex PercentRegex();
  [GeneratedRegex(@"^\[(\d{2}:\d{2}:\d{2}|\d{4}-\d{2}-\d{2}\s+\d{2}:\d{2}:\d{2})\]")]
  private static partial Regex DateTimeStampRegex();
  [GeneratedRegex(@"^::\s*\d+\s*-\s+.+")]
  private static partial Regex SectionHeaderNumberRegex();
  [GeneratedRegex(@"^::\s*[A-Z0-9_ ]+$")]
  private static partial Regex SectionHeaderUppercaseRegex();

  private static bool IsSectionTitle(string line) {
    string trimmed = line.Trim();

    if (!trimmed.StartsWith("::"))
      return false;

    if (SectionHeaderNumberRegex().IsMatch(trimmed))
      return true;

    if (SectionHeaderUppercaseRegex().IsMatch(trimmed))
      return true;

    return false;
  }


  private string BuildCollapsedPreview() {
    if (string.IsNullOrWhiteSpace(_fullPreviewText))
      return "";

    string[] lines = _fullPreviewText.Replace("\r\n","\n").Split('\n');
    List<string> output = [];

    bool skipping = false;

    foreach (string line in lines) {
      if (IsSectionTitle(line)) {
        string section = line.Trim();

        skipping = _collapsedSections.Contains(section);

        output.Add(line);

        if (skipping)
          output.Add("::     ... section repliée ...");

        continue;
      }

      if (!skipping)
        output.Add(line);
    }

    return string.Join(Environment.NewLine,output);
  }

  private void ApplySyntaxHighlighting() {

    RichTextBox textBox =
      CurrentPreviewTextBox;

    switch (_activePreviewDocument) {

      case PreviewDocumentType.AutoUnattend:

      case PreviewDocumentType.Unattend:

        _syntaxHighlighter.ApplyXml(
          textBox);

        break;


      case PreviewDocumentType.ProfileJson:

        _syntaxHighlighter.ApplyJson(
          textBox);

        break;


      default:

        _syntaxHighlighter.ApplyCmd(
          textBox);

        break;
    }
  }

  private void RefreshPreview() {
    _refreshCount++;
    Debug.WriteLine($"REFRESH #{_refreshCount} - Active={_activePreviewDocument}");
    try {
      _isUpdatingPreview = true;

      ApplyUiToProfile();

      string result = GeneratePreviewDocument(_activePreviewDocument);

      if (_activePreviewDocument == PreviewDocumentType.Diskprep) {
        _fullPreviewText = result;
        result = BuildCollapsedPreview();
      }

      RichTextBox textBox = CurrentPreviewTextBox;

      if (GetRichText(textBox) != result)
        SetRichText(textBox,result);

      ApplySyntaxHighlighting();

      //pnlLineNumbers.Invalidate();
      //pnlMiniMap.Invalidate();
    }
    catch (Exception ex) {
      string message =
        "ERREUR PREVIEW :" + Environment.NewLine +
        ex.Message + Environment.NewLine +
        Environment.NewLine +
        ex.StackTrace;

      SetRichText(CurrentPreviewTextBox,message);
    }
    finally {
      _isUpdatingPreview = false;
    }
  }



  private static void SetItemChecked(ListBox listBox,int index,bool isChecked) {
    if (index < 0 || index >= listBox.Items.Count) {
      return;
    }
    if (listBox.Items[index] is CheckBox checkBox) {
      checkBox.IsChecked = isChecked;
    }
  }
  private void LoadProfileToUi() {
    _livePreviewReady = false;

    cmbOsDisk.SelectedItem = _detectedDisks.FirstOrDefault(
        d => d.DiskNumber == _profile.Disk.OsDisk
    );

    cmbData1Disk.SelectedItem = _detectedDisks.FirstOrDefault(
        d => d.DiskNumber == (_profile.Disk.DataDisk ?? 1)
    );

    cmbData2Disk.SelectedItem = _detectedDisks.FirstOrDefault(
        d => d.DiskNumber == (_profile.Disk.Data2Disk ?? 1)
    );

    chkSeparateDataDisk.IsChecked =
        _profile.Disk.LayoutMode == DiskLayoutMode.SeparateDataDisk;

    chkCreateData1.IsChecked = _profile.Disk.CreateData1;
    chkCreateData2.IsChecked = _profile.Disk.CreateData2;

    numEfiSize.Value = _profile.Disk.EfiSizeMb;
    numMsrSize.Value = _profile.Disk.MsrSizeMb;
    numSystemBiosSize.Value = _profile.Disk.SystemSizeMb;
    numWindowsSize.Value = _profile.Disk.WindowsSizeMb;
    numRecoverySize.Value = _profile.Disk.RecoverySizeMb;

    numImageIndex.Value = _profile.Image.ImageIndex;

    cmbImageType.SelectedIndex = _profile.Image.ImageType switch {
      WindowsImageType.Wim => 1,
      WindowsImageType.Esd => 2,
      _ => 0
    };

    SelectComboValue(cmbUiLanguage,_profile.Unattend.UILanguage);
    SelectComboValue(cmbTimeZone,_profile.Unattend.TimeZone);

    txtProductKey.Text = _profile.Unattend.ProductKey;
    txtOrganization.Text = _profile.Unattend.Organization;
    txtOwner.Text = _profile.Unattend.Owner;
    txtComputerName.Text = _profile.Unattend.ComputerName;

    chkCreateLocalAccount.IsChecked = _profile.Unattend.CreateLocalAccount;
    txtLocalUserName.Text = _profile.Unattend.LocalUserName;
    txtAdminPassword.Password = _profile.Unattend.LocalUserPassword;
    SelectComboValue(cmbLocalUserGroup,_profile.Unattend.LocalUserGroup);

    chkEnableAutoLogon.IsChecked = _profile.Unattend.EnableAutoLogon;
    numAutoLogonCount.Value = _profile.Unattend.AutoLogonCount;

    chkCreateLocalAccount.IsChecked = _profile.Unattend.CreateLocalAccount;
    txtLocalUserName.Text = _profile.Unattend.LocalUserName;
    txtAdminPassword.Password = _profile.Unattend.LocalUserPassword;

    SelectComboValue(
        cmbLocalUserGroup,
        _profile.Unattend.LocalUserGroup
    );

    chkEnableAutoLogon.IsChecked = _profile.Unattend.EnableAutoLogon;
    numAutoLogonCount.Value = _profile.Unattend.AutoLogonCount;

    for (int i = 0;i < lstInputLocales.Items.Count;i++) {
      if (lstInputLocales.Items[i] is KeyboardLocaleOption option) {
        bool isChecked = _profile.Unattend.InputLocaleCodes.Contains(option.InputLocaleCode);
        SetItemChecked(lstInputLocales,i,isChecked);
      }
    }
    lstInputLocales.IsKeyboardFocusWithinChanged += (_,e) => {
      if (e.NewValue is false) {
        ConfigurationChanged(lstInputLocales,new RoutedEventArgs());
      }
    };

    txtPackageName.Text = _profile.Package.PackageName;
    txtPackageVersion.Text = _profile.Package.Version;
    txtPackageAuthor.Text = _profile.Package.Author;
    txtOutputDirectory.Text = _profile.Package.OutputDirectory;

    chkIncludeDrivers.IsChecked = _profile.Package.IncludeDrivers;
    chkIncludeApplications.IsChecked = _profile.Package.IncludeApplications;
    chkIncludeScripts.IsChecked = _profile.Package.IncludeScripts;
    chkIncludeWinPE.IsChecked = _profile.Package.IncludeWinPE;

    txtDriversSourcePath.Text = _profile.Package.DriversSourcePath;
    txtApplicationsSourcePath.Text = _profile.Package.ApplicationsSourcePath;
    //txtScriptsSourcePath.Text = _profile.Package.ScriptsSourcePath;
    //txtWinPESourcePath.Text = _profile.Package.WinPESourcePath;

    chkIncludeSetupScripts.IsChecked = _profile.Package.IncludeSetupScripts;
    chkIncludeSetupConfig.IsChecked = _profile.Package.IncludeSetupConfig;

    txtSetupScriptsSourcePath.Text = _profile.Package.SetupScriptsSourcePath;
    txtSetupConfigSourcePath.Text = _profile.Package.SetupConfigSourcePath;

    txtWindowsIsoPath.Text = _profile.WindowsMedia.WindowsIsoPath;
    txtMediaWorkingDirectory.Text = _profile.WindowsMedia.MediaFolder;
    chkExtractWindowsIso.IsChecked = _profile.WindowsMedia.ExtractWindowsIso;
    chkInjectAutounattend.IsChecked = _profile.WindowsMedia.InjectAutounattend;
    chkInjectDeployFolder.IsChecked = _profile.WindowsMedia.InjectDeployFolder;
    chkPrepareUsbMedia.IsChecked = _profile.WindowsMedia.PrepareUsbMedia;
    cmbUsbDriveLetter.Text = _profile.WindowsMedia.UsbDriveLetter;

    chkBuildIso.IsChecked = _profile.WindowsMedia.BuildIso;
    txtFinalIsoPath.Text = _profile.WindowsMedia.IsoOutputPath;

    chkRunDetachUsb.IsChecked = _profile.Orchestration.RunDetachUsb;
    chkRunReorgVolumes.IsChecked = _profile.Orchestration.RunReorgVolumes;
    chkRunWifi.IsChecked = _profile.Orchestration.RunWifi;
    chkRunDrivers.IsChecked = _profile.Orchestration.RunDrivers;
    chkRunWindowsUpdateDrivers.IsChecked = _profile.Orchestration.RunWindowsUpdateDrivers;
    chkRunSoftwares.IsChecked = _profile.Orchestration.RunSoftwares;
    chkRunM365.IsChecked = _profile.Orchestration.RunMicrosoft365;
    chkRunPostInstall.IsChecked = _profile.Orchestration.RunPostInstall;
    chkRunCleanup.IsChecked = _profile.Orchestration.RunCleanup;

    UpdateUiState();

    _livePreviewReady = true;

    ValidateConfiguration();
    RefreshPreview();
    UpdateStatusBar();
  }

  private void ResetValidationColors() {
    cmbData1Disk.Foreground = Brushes.White;
    cmbData2Disk.Foreground = Brushes.White;

    lblData1Disk.Foreground = Brushes.White;
    lblData2Disk.Foreground = Brushes.White;
    lblWindowsSize.Foreground = Brushes.White;
    lblStatusProfile.Foreground = Brushes.White;
  }

  private static int GetSelectedDiskNumber(ComboBox comboBox) {
    return comboBox.SelectedItem is PhysicalDiskInfo disk
        ? disk.DiskNumber
        : -1;
  }

  private void MarkInvalid(Control control,Label label,string message) {
    _hasValidationError = true;

    control.Foreground = Brushes.OrangeRed;
    label.Foreground = Brushes.OrangeRed;
    lblValidation.Foreground = Brushes.OrangeRed;
    lblValidation.Background = Brushes.Transparent;
    lblValidation.Content = message;
    control.ToolTip = message;
  }

  private void ValidateConfiguration() {
    _hasValidationError = false;
    ResetValidationColors();

    lblValidation.Content = "";

    int osDisk = GetSelectedDiskNumber(cmbOsDisk);
    int dataDisk = GetSelectedDiskNumber(cmbData1Disk);
    int data2Disk = GetSelectedDiskNumber(cmbData2Disk);

    if (chkSeparateDataDisk.IsChecked == true && dataDisk == osDisk) {
      MarkInvalid(
          cmbData1Disk,
          lblData1Disk,
          "ERROR : Data-1 utilise le même disque que l'OS."
      );
    }

    if (chkCreateData2.IsChecked == true && data2Disk == osDisk) {
      MarkInvalid(
          cmbData2Disk,
          lblData2Disk,
          "ERROR : Data-2 utilise le même disque que l'OS."
      );
    }

    if (chkSeparateDataDisk.IsChecked == true && chkCreateData2.IsChecked == true && dataDisk == data2Disk) {
      MarkInvalid(
          cmbData2Disk,
          lblData2Disk,
          "ERROR : Data-1 et Data-2 utilisent le même disque."
      );
    }

    if (numWindowsSize.Value < 40000) {
      MarkInvalid(
          numWindowsSize,
          lblWindowsSize,
          "WARNING : la partition Windows est inférieure à 40 Go."
      );
    }

    if (_hasValidationError) {
      lblStatusProfile.Text = "⚠ Configuration invalide";
      lblStatusProfile.Foreground = Brushes.OrangeRed;
    }
    else {
      lblStatusProfile.Text = string.IsNullOrWhiteSpace(_currentProfilePath)
          ? "Profil : non sauvegardé"
          : "Profil : " + Path.GetFileName(_currentProfilePath);

      lblStatusProfile.Foreground = Brushes.White;
    }
  }

  private void UpdateUiState() {
    bool showData1 = chkSeparateDataDisk.IsChecked == true;
    lblData1Disk.Visibility = showData1
        ? Visibility.Visible
        : Visibility.Hidden;

    cmbData1Disk.Visibility = showData1
        ? Visibility.Visible
        : Visibility.Hidden;

    bool showData2 = chkCreateData2.IsChecked == true;
    lblData2Disk.Visibility = showData2
        ? Visibility.Visible
        : Visibility.Hidden;

    cmbData2Disk.Visibility = showData2
        ? Visibility.Visible
        : Visibility.Hidden;

    cmbData1Disk.IsEnabled = chkSeparateDataDisk.IsChecked == true;
    cmbData2Disk.IsEnabled = chkCreateData2.IsChecked == true;

    txtLocalUserName.IsEnabled = chkCreateLocalAccount.IsChecked == true;
    txtAdminPassword.IsEnabled = chkCreateLocalAccount.IsChecked == true;
    cmbLocalUserGroup.IsEnabled = chkCreateLocalAccount.IsChecked == true;

    chkEnableAutoLogon.IsEnabled = chkCreateLocalAccount.IsChecked == true;
    numAutoLogonCount.IsEnabled = chkCreateLocalAccount.IsChecked == true && chkEnableAutoLogon.IsChecked == true;

    txtDriversSourcePath.IsEnabled = chkIncludeDrivers.IsChecked == true;
    btnBrowseDriversSourcePath.IsEnabled = chkIncludeDrivers.IsChecked == true;

    txtApplicationsSourcePath.IsEnabled = chkIncludeApplications.IsChecked == true;
    btnBrowseApplicationsSourcePath.IsEnabled = chkIncludeApplications.IsChecked == true;

    txtSetupScriptsSourcePath.IsEnabled = chkIncludeScripts.IsChecked == true;
    btnBrowseSetupScriptsSourcePath.IsEnabled = chkIncludeScripts.IsChecked == true;

    //txtWinPESourcePath.IsEnabled = chkIncludeWinPE.IsChecked == true;
    //btnBrowseWinPE.IsEnabled = chkIncludeWinPE.IsChecked == true;

    txtSetupScriptsSourcePath.IsEnabled = chkIncludeSetupScripts.IsChecked == true;
    btnBrowseSetupScriptsSourcePath.IsEnabled = chkIncludeSetupScripts.IsChecked == true;

    txtSetupConfigSourcePath.IsEnabled = chkIncludeSetupConfig.IsChecked == true;
    btnBrowseSetupConfigSourcePath.IsEnabled = chkIncludeSetupConfig.IsChecked == true;
  }

  private void UpdateStatusBar() {
    if (!_hasValidationError) {
      lblStatusProfile.Text = string.IsNullOrWhiteSpace(_currentProfilePath)
          ? "Profil : non sauvegardé"
          : "Profil : " + Path.GetFileName(_currentProfilePath);

      lblStatusProfile.Foreground = System.Windows.Media.Brushes.White;
    }

    lblStatusDisks.Text = $"Disques : {_detectedDisks.Count} détecté(s)";
    lblStatusImage.Text = $"Image : {_profile.Image.ImageType} / Index {_profile.Image.ImageIndex}";
  }

  private static void BrowseFolderInto(TextBox target) {
    OpenFolderDialog dialog = new();

    if (!string.IsNullOrWhiteSpace(target.Text) && Directory.Exists(target.Text))
      dialog.InitialDirectory = target.Text;

    if (dialog.ShowDialog() == true)
      target.Text = dialog.FolderName;
  }

  private BuildPipelineService CreatePipelineService() {
    BuildPipelineService service = new();

    service.ProgressChanged += (value,message) => {
      if (!Dispatcher.CheckAccess()) {
        Dispatcher.BeginInvoke(
          new Action(
            () => UpdateBuildProgress(value,message)));
      }
      else {
        UpdateBuildProgress(value,message);
      }
    };

    return service;
  }

  private async Task BuildPackage() {
    ApplyUiToProfile();

    progressBuild.Value = 0;
    lblBuildProgress.Content = "Démarrage...";

    BuildPipelineService service = CreatePipelineService();

    PipelineResult result = await Task.Run(() =>
        BuildPipelineService.BuildPackage(_profile)
    );

    AppendPipelineLogs(result);

    lblBuildProgress.Content = result.Success ? "Terminé" : "Erreur";

    MessageBox.Show(
        result.Message,
        "Build Package",
        MessageBoxButton.OK,
        result.Success ? MessageBoxImage.Information : MessageBoxImage.Error
    );
  }

  private bool TryGetSelectedPreviewEditor(out RichTextBox editor) {
    editor = null!;

    if (TabMain.SelectedItem is not TabItem selectedTab)
      return false;

    string? key = selectedTab.Header?.ToString();

    if (string.IsNullOrWhiteSpace(key))
      return false;

    return _previewEditors.TryGetValue(key,out editor!);
  }

  private static string GetEditorText(RichTextBox editor) {
    TextRange range =
        new(editor.Document.ContentStart,
            editor.Document.ContentEnd);

    string text = range.Text;

    // Le FlowDocument ajoute généralement un retour de fin de paragraphe
    if (text.EndsWith("\r\n"))
      text = text[..^2];

    return text;
  }

  private void btnBrowseOutputDirectory_Click(object sender,RoutedEventArgs e) {
    BrowseFolderInto(txtOutputDirectory);
  }

  private void btnBrowseApplicationsSourcePath_Click(object sender,RoutedEventArgs e) {
    BrowseFolderInto(txtApplicationsSourcePath);
  }

  private void btnBrowseSetupScriptsSourcePath_Click(object sender,RoutedEventArgs e) {
    BrowseFolderInto(txtSetupScriptsSourcePath);
  }

  private void btnBrowseDriversSourcePath_Click(object sender,RoutedEventArgs e) {
    BrowseFolderInto(txtDriversSourcePath);
  }

  private void btnBrowseWindowsIsoPath_Click(object sender,RoutedEventArgs e) {
    BrowseFolderInto(txtWindowsIsoPath);
  }

  private void btnBrowseMediaWorkingDirectory_Click(object sender,RoutedEventArgs e) {
    BrowseFolderInto(txtMediaWorkingDirectory);
  }

  private void btnBuildWindowsMedia_Click(object sender,RoutedEventArgs e) {

  }

  private void btnOpenMediaFolder_Click(object sender,RoutedEventArgs e) {

  }

  private void btnApply_Click(object sender,RoutedEventArgs e) {

  }

  private void btnBuildIso_Click(object sender,RoutedEventArgs e) {

  }

  private void menuSaveProfile_Click(object? sender,RoutedEventArgs e) {
    ApplyUiToProfile();

    SaveFileDialog dialog = new() {
      Filter = "Deploy Profile (*.deploy.json)|*.deploy.json",
      FileName = "default.deploy.json"
    };

    if (dialog.ShowDialog() != true)
      return;

    ProfileSerializer.Save(dialog.FileName,_profile);

    _currentProfilePath = dialog.FileName;

    UpdateStatusBar();
  }

  private void menuOpenProfile_Click(object? sender,RoutedEventArgs e) {
    OpenFileDialog dialog = new() {
      Filter = "Deploy Profile (*.deploy.json)|*.deploy.json"
    };

    if (dialog.ShowDialog(this) != true)
      return;

    _profile = ProfileSerializer.Load(dialog.FileName);
    _currentProfilePath = dialog.FileName;

    LoadProfileToUi();
  }

  private void menuNewProfile_Click(object? sender,RoutedEventArgs e) {
    _profile = new DeploymentProfile();
    _currentProfilePath = "";
    LoadProfileToUi();
  }

  private void menuExit_Click(object? sender,RoutedEventArgs e) {
    if (_orchestrationPendingChanges)
      Close();
  }

  private void menuEditSelectedView_Click(object sender,RoutedEventArgs e) {
    bool flowControl = PrintSelectedView();
    if (!flowControl) {
      return;
    }
  }

  private async void mnuBuildPackage_Click(object sender,RoutedEventArgs e) {
    await BuildPackage();
  }

  private void mnuBuildWindowsMedia_Click(object sender,RoutedEventArgs e) {

  }

  private void mnuBuildAll_Click(object sender,RoutedEventArgs e) {

  }

  private void mnuPrepareUsb_Click(object sender,RoutedEventArgs e) {

  }

  private void mnuOpenPackageFolder_Click(object sender,RoutedEventArgs e) {

  }

  private void mnuOpenBuildFolder_Click(object sender,RoutedEventArgs e) {

  }

  private void mnuEditWithNotepadPlusPlus_Click(object sender,RoutedEventArgs e) {

  }

  private void menuSaveProfileAs_Click(object sender,RoutedEventArgs e) {

  }

  private static void OpenFileInNotepadPlusPlus(string path) {
    Process.Start(new ProcessStartInfo {
      FileName = @"C:\Program Files\Notepad++\notepad++.exe",
      Arguments = $"\"{path}\"",
      UseShellExecute = true
    });
  }

  private static void OpenTextInNotepadPlusPlus(string content,string fileName) {
    if (string.IsNullOrWhiteSpace(content)) {
      MessageBox.Show("Contenu vide.","Notepad++",
          MessageBoxButton.OK,MessageBoxImage.Warning);
      return;
    }

    string tempPath = Path.Combine(Path.GetTempPath(),fileName);

    File.WriteAllText(tempPath,content,EncodingHelper.Utf8NoBom);

    Process.Start(new ProcessStartInfo {
      FileName = @"C:\Program Files\Notepad++\notepad++.exe",
      Arguments = $"\"{tempPath}\"",
      UseShellExecute = true
    });
  }

  private void mnuEditFileWithNotepadPlusPlus_Click(object sender,RoutedEventArgs e) {
    if (sender is not MenuItem menuItem || menuItem.Tag is not string document) {
      return;
    }
    switch (document) {
      case "diskprep.cmd":
        OpenTextInNotepadPlusPlus(GetRichText(previewDiskPrep.Editor),"diskprep.cmd");
        break;

      case "SetupComplete.cmd":
        OpenTextInNotepadPlusPlus(GetRichText(previewSetupComplete.Editor),"SetupComplete.cmd");
        break;

      case "orchestrator_resume.cmd":
        OpenTextInNotepadPlusPlus(GetRichText(previewOrchestrator_resume.Editor),"orchestrator_resume.cmd");
        break;

      case "autounattend.xml":
        OpenTextInNotepadPlusPlus(GetRichText(previewAutoUnattend.Editor),"autounattend.xml");
        break;

      case "unattend.xml":
        OpenTextInNotepadPlusPlus(GetRichText(previewUnattend.Editor),"unattend.xml");
        break;


      case "BuildReport":
        OpenTextInNotepadPlusPlus(GetRichText(previewprofileBuildReport.Editor),"BuildReport.txt");
        break;

      case "BuildLog":
        OpenTextInNotepadPlusPlus(GetRichText(previewBuildLog.Editor),"BuildLog.txt");
        break;

      case "CurrentProfile":
        if (string.IsNullOrWhiteSpace(_currentProfilePath) || !File.Exists(_currentProfilePath)) {
          MessageBox.Show("Aucun fichier profil JSON chargé.");
          return;
        }
        OpenTextInNotepadPlusPlus(File.ReadAllText(_currentProfilePath),"profile.deploy.json");
        break;
    }
  }
  private void mnuOemCommands_Click(object sender,RoutedEventArgs e) {

  }

  private void mnuSynchronousCommands_Click(object sender,RoutedEventArgs e) {
    SynchronousCommandsWindow window = new(_profile.WindowsPeCommands,_profile.FirstLogonCommands);

    window.CommandsChanged += () => {
      _profile.WindowsPeCommands = window.WindowsPeCommands;

      _profile.FirstLogonCommands = window.FirstLogonCommands;

      RefreshPreview();
    };

    if (!ShowModalWithFade(window))
      return;

    _profile.WindowsPeCommands = window.WindowsPeCommands;
    _profile.FirstLogonCommands = window.FirstLogonCommands;

    RefreshPreview();
    UpdateStatusBar();
  }

  private void mnuAbout_Click(object? sender,RoutedEventArgs e) {
    HelpHelper.mnuAbout(this,_profile);
  }

  private void MnuLicense_Click(object sender,RoutedEventArgs e) {
    LicenseHelper.AfficherLicence(this,_licenseService,_profile);
  }

  private void MnuImportNewLicense_Click(object sender,RoutedEventArgs e) {
    LicenseHelper.ImporterLicence(this,_licenseService);
  }

  private async void MnuCheckForUpdate_Click(object sender,RoutedEventArgs e) {
    await HelpHelper.mnuCheckForUpdates(this,_profile);
  }

  private void btnBuildAll_Click(object sender,RoutedEventArgs e) {

  }

  private void btnOpenBuildAllPackages_Click(object sender,RoutedEventArgs e) {

  }

  private void btnBuildPackage_Click(object sender,RoutedEventArgs e) {

  }

  private void btnOpenPackageFolder_Click(object sender,RoutedEventArgs e) {

  }

  private void btnPrepareUSB_Click(object sender,RoutedEventArgs e) {

  }

  private void btnPauseResume_Click(object sender,RoutedEventArgs e) {

  }

  private void btnCancelBuild_Click(object sender,RoutedEventArgs e) {

  }

  private void btnPrintSelectedView_Click(object sender,RoutedEventArgs e) {
    bool flowControl = PrintSelectedView();
    if (!flowControl) {
      return;
    }
  }

  private void btnCloseSearch_Click(object sender,RoutedEventArgs e) {

  }

  private void btnFindNext_Click(object sender,RoutedEventArgs e) {

  }

  private void btnBrowseSetupConfigSourcePath_Click(object sender,RoutedEventArgs e) {

  }
  #endregion

  // ===================================================
  //  Progression globale pour le processus de préparation USB
  //  -------------------------------------------------
  //  0-10    Initialisation
  //  10-40   Extraction ISO source
  //  40-55   Injection autounattend
  //  55 - 75   Injection Deploy
  //  75 - 100  Finalisation média
  // ===================================================

  private async void btnPrepareUsb_Click(object sender,EventArgs e) {
    await RunPrepareUsbPipelineAsync(DateTime.Now);
  }

  private async Task<bool> RunPrepareUsbPipelineAsync(DateTime usbStartTime) {
    imgPauseResume.Source = LoadImageResource("pause.png");

    _pipeline = new BuildPipelineService();

    _pipeline.ProgressChanged += (value,message) => {
      UpdateBuildProgress(value,message);
    };

    try {
      ApplyUiToProfile();
      // ==================================================
      // 0 - INITIALISATION
      // ==================================================
      if (!ValidatePrepareUsbPrerequisites())
        return false;
      PrepareUsbUiStart();

      _buildCancellation?.Dispose();
      _buildCancellation = new CancellationTokenSource();

      InitializeUsbLiveReport(usbStartTime);
      _pipelineSteps.Clear();
      _pipelineHasWarning = false;
      UsbPreparationService service = CreateUsbPreparationService();
      service.LiveLogCallback = line => AppendBuildLog(line);
      service.ProgressCallback = (percent,message) => ReportStepProgress(percent,message);

      // ==================================================
      // 1 - USB TARGET SELECTION
      // ==================================================
      UsbPreparationContext? context = null;

      PipelineStepResult targetResult =
          await RunPipelineStepAsync(
              "01 - Sélection média USB...",
              LoadImageResource("usb.png"),
              () => {
                UsbPreparationContext selectedContext =
              SelectUsbTarget();

                if (selectedContext.Cancelled) {
                  return Task.FromResult(
                PipelineStepResult.Fail(
                    "USB target selection cancelled."
                )
            );
                }

                WriteDiskpartPreview(selectedContext);

                context = selectedContext;

                return Task.FromResult(
              PipelineStepResult.Ok(
                  "USB target selected successfully."
              )
          );
              });

      if (!targetResult.Success || context == null)
        return false;

      // ==================================================
      // 2 - BUILD PACKAGE
      // ==================================================
      PipelineStepResult packageResult =
          await RunPipelineStepAsync(
              "02 - Build package deploy...",
              LoadImageResource("deploy.png"),
              BuildDeploymentPackageStepAsync);

      if (!packageResult.Success)
        return false;

      // ==================================================
      // 3 - BUILD WINDOWS MEDIA
      // ==================================================
      PipelineStepResult windowsMediaResult =
          await RunPipelineStepAsync(
              "03 - Build Windows media...",
              LoadImageResource("CopyFile.png"),
              BuildWindowsMediaStepAsync);

      if (!windowsMediaResult.Success)
        return false;

      // ==================================================
      // 4 - FAT32 / SPLIT WIM
      // ==================================================
      PipelineStepResult splitResult =
          await RunPipelineStepAsync(
              "04 - Préparation USB...",
              LoadImageResource("gear.png"),
              () => Task.FromResult(
                  HandleFat32SplitStep(service,context)
              ));

      if (!splitResult.Success)
        return false;

      // ==================================================
      // 5 - BUILD FINAL ISO
      // ==================================================
      PipelineStepResult isoResult =
          await RunPipelineStepAsync(
              "05 - Build final ISO...",
              LoadImageResource("archive.png"),
              BuildFinalIsoStepAsync);

      if (!isoResult.Success)
        return false;

      // ==================================================
      // 6 - CLEANUP WINDOWS MEDIA FOLDER
      // ==================================================
      PipelineStepResult cleanupResult =
          await RunPipelineStepAsync(
              "06 - Nettoyage dossier WindowsMedia...",
              LoadImageResource("check.png"),
              () => Task.FromResult(
                  CleanupWindowsMediaFolderStep()
              ));

      if (!cleanupResult.Success)
        return false;

      // ==================================================
      // 7 - FORMAT USB
      // ==================================================
      UsbPreparationContext currentContext = context;

      PipelineStepResult formatResult =
          await RunPipelineStepAsync(
              "07 - Format USB...",
              LoadImageResource("usb.png"),
              () => Task.FromResult(
                  FormatUsbWithRetryStep(service,ref currentContext)
              ));

      context = currentContext;

      if (!formatResult.Success)
        return false;

      // ==================================================
      // 8 - EXTRACT FINAL ISO TO USB
      // ==================================================
      PipelineStepResult extractResult =
          await RunPipelineStepAsync(
              "08 - Extraction ISO vers USB...",
               LoadImageResource("archive.png"),

              () => ExtractFinalIsoToUsbStepAsync(service));

      if (!extractResult.Success)
        return false;

      // ==================================================
      // 9 - INSTALL BOOT SECTOR
      // ==================================================
      PipelineStepResult bootResult =
          await RunPipelineStepAsync(
              "09 - Installation du secteur de boot...",
              LoadImageResource("hard_disk.png"),
              () => Task.FromResult(
                  InstallBootSectorStep(context)
              ));

      if (!bootResult.Success)
        return false;

      // ==================================================
      // 10 - VALIDATE USB
      // ==================================================
      PipelineStepResult validationResult =
          await RunPipelineStepAsync(
              "10 - Validation du média USB...",
              LoadImageResource("check.png"),
              () => Task.FromResult(
                  ValidateUsbMediaStep(context)
              ));

      if (!validationResult.Success)
        return false;

      // ==================================================
      // 10 - FINALISATION
      // ==================================================
      SetOperationStatus("10 - Finalisation...");
      CompleteUsbPreparation(usbStartTime);

      imgPauseResume.Source = _pipelineHasWarning
          ? LoadImageResource("warning.png")
          : LoadImageResource("success.png");
    }
    catch (OperationCanceledException) {
      imgPauseResume.Source = LoadImageResource("stop.png");
      HandleUsbPreparationCancelled();
    }
    catch (Exception ex) {
      imgPauseResume.Source = LoadImageResource("erreur.png");
      HandleUsbPreparationException(ex);
    }
    finally {
      try {
        SaveUsbPreparationLog();
        SaveUsbBuildReport(usbStartTime);
      }
      catch (Exception logEx) {
        MessageBox.Show(
            "Impossible d'écrire le log ou le rapport USB." +
            Environment.NewLine +
            logEx.Message,
            "USB log/report",
            MessageBoxButton.OK,
            MessageBoxImage.Warning
        );
      }

      ClearOperationStatus();
      AppendUsbLiveReport("");
      AppendUsbLiveReport("==================================================");
      AppendUsbLiveReport("END OF USB BUILD REPORT");
      AppendUsbLiveReport("==================================================");
      ToggleBuildButtons(true);
    }
    return true;
  }

  private static readonly JsonSerializerOptions JsonCaseInsensitiveOptions = new() {
    PropertyNameCaseInsensitive = true
  };

  private void ExportUsbBuildArchive(string reportPath) {
    try {
      string reportsDir = Path.GetDirectoryName(reportPath)!;

      string logsDir = Path.Combine(Path.GetDirectoryName(reportsDir)!,"Logs");

      string timestamp =
          DateTime.Now.ToString("yyyyMMdd_HHmmss");

      string zipPath =
          Path.Combine(
              reportsDir,
              $"USB_BUILD_{timestamp}.zip"
          );

      using ZipArchive archive =
          ZipFile.Open(
              zipPath,
              ZipArchiveMode.Create
          );

      // Rapport final
      archive.CreateEntryFromFile(
          reportPath,
          Path.GetFileName(reportPath)
      );

      // Logs
      if (Directory.Exists(logsDir)) {
        foreach (string file in Directory.GetFiles(logsDir)) {
          archive.CreateEntryFromFile(
              file,
              Path.Combine(
                  "Logs",
                  Path.GetFileName(file)
              )
          );
        }
      }
      AppendBuildLog(
          "[INFO] ZIP archive generated : " +
          zipPath
      );
    }
    catch (Exception ex) {
      AppendBuildLog(
          "[WARNING] ZIP export failed : " +
          ex.Message
      );
    }
  }

  private static bool ShouldLog7ZipLine(string line) {
    if (!line.Contains("[7Z]"))
      return true;

    string clean = line.Replace("[INFO]","")
                       .Replace("[7Z]","")
                       .Trim();

    if (string.IsNullOrWhiteSpace(clean))
      return false;

    // Progression : 0%, 12% 321, 97% 885...
    if (PercentRegex().IsMatch(clean))
      return true;

    // Lignes utiles
    if (clean.StartsWith("Extracting archive:",StringComparison.OrdinalIgnoreCase))
      return true;

    if (clean.StartsWith("Everything is Ok",StringComparison.OrdinalIgnoreCase))
      return true;

    if (clean.StartsWith("Folders:",StringComparison.OrdinalIgnoreCase))
      return true;

    if (clean.StartsWith("Files:",StringComparison.OrdinalIgnoreCase))
      return true;

    if (clean.StartsWith("Size:",StringComparison.OrdinalIgnoreCase))
      return true;

    if (clean.StartsWith("Compressed:",StringComparison.OrdinalIgnoreCase))
      return true;

    return false;
  }

  private async Task<PipelineStepResult> BuildWindowsMediaStepAsync() {
    try {
      bool success = await BuildWindowsMediaAsync();

      if (!success) {
        return PipelineStepResult.Fail(
            "Windows media build failed."
        );
      }

      return PipelineStepResult.Ok(
          "Windows media generated successfully."
      );
    }
    catch (Exception ex) {
      return PipelineStepResult.Fail(
          "Windows media build failed : " + ex.Message,
          ex
      );
    }
  }

  private async Task<PipelineStepResult> BuildDeploymentPackageStepAsync() {
    try {
      bool success =
          await BuildDeploymentPackageAsync();

      if (!success) {
        return PipelineStepResult.Fail(
            "Deployment package build failed."
        );
      }

      return PipelineStepResult.Ok(
          "Deployment package generated successfully."
      );
    }
    catch (Exception ex) {
      return PipelineStepResult.Fail(
          "Deployment package build failed : " + ex.Message,
          ex
      );
    }
  }

  private PipelineStepResult HandleFat32SplitStep(UsbPreparationService service,UsbPreparationContext context) {
    try {
      bool success =
          HandleFat32SplitIfNeeded(service,context);

      if (!success) {
        return PipelineStepResult.Fail(
            "FAT32 / Split WIM preparation failed."
        );
      }

      return PipelineStepResult.Ok(
          "FAT32 / Split WIM preparation completed."
      );
    }
    catch (Exception ex) {
      return PipelineStepResult.Fail(
          "FAT32 / Split WIM preparation failed : " + ex.Message,
          ex
      );
    }
  }

  private async Task<PipelineStepResult> BuildFinalIsoStepAsync() {
    try {
      bool success =
          await BuildFinalIsoAsync();

      if (!success) {
        return PipelineStepResult.Fail(
            "Final ISO build failed."
        );
      }

      return PipelineStepResult.Ok(
          "Final ISO generated successfully."
      );
    }
    catch (Exception ex) {
      return PipelineStepResult.Fail(
          "Final ISO build failed : " + ex.Message,
          ex
      );
    }
  }

  private async Task<PipelineStepResult> ExtractFinalIsoToUsbStepAsync(UsbPreparationService service) {
    CancellationToken token = _buildCancellation?.Token ?? CancellationToken.None;
    try {
      UsbPreparationResult result =
          await Task.Run(() =>
              service.ExtractIsoToUsbWith7Zip(
                  _profile.WindowsMedia.IsoOutputPath,
                  _profile.WindowsMedia.UsbDriveLetter,
                  token
              ),
              token
          );

      AppendUsbResult(result);

      if (!result.Success) {
        string errorMessage =
            result.Errors.Count > 0
                ? string.Join(Environment.NewLine,result.Errors)
                : "ISO extraction failed.";

        return PipelineStepResult.Fail(
            errorMessage
        );
      }

      return PipelineStepResult.Ok(
          "ISO extracted to USB successfully."
      );
    }
    catch (OperationCanceledException) {
      throw;
    }
    catch (Exception ex) {
      return PipelineStepResult.Fail(
          "ISO extraction failed : " + ex.Message,
          ex
      );
    }
  }

  private PipelineStepResult FormatUsbWithRetryStep(UsbPreparationService service,ref UsbPreparationContext context) {
    while (true) {
      try {
        bool success = FormatUsb(service,context);

        if (success) {
          return PipelineStepResult.Ok(
              "USB formatted successfully."
          );
        }

        MessageBoxResult choice = MessageBox.Show(
            "Le formatage du média USB a échoué." +
            Environment.NewLine +
            Environment.NewLine +
            "Le support est peut-être défectueux ou protégé en écriture." +
            Environment.NewLine +
            "Voulez-vous sélectionner un autre média USB ?",
            "Format USB failed",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning
        );

        if (choice == MessageBoxResult.Yes) {
          return PipelineStepResult.Fail(
              "USB format failed. User cancelled media replacement."
          );
        }

        context = SelectUsbTarget();

        if (context.Cancelled) {
          return PipelineStepResult.Fail(
              "USB format failed. USB target selection cancelled."
          );
        }

        WriteDiskpartPreview(context);

        AppendBuildLog(
            "[INFO] New USB target selected. Retrying format..."
        );
      }
      catch (Exception ex) {
        return PipelineStepResult.Fail(
            "USB format failed : " + ex.Message,
            ex
        );
      }
    }
  }

  private PipelineStepResult ValidateUsbMediaStep(UsbPreparationContext context) {
    try {
      bool success =
          ValidateUsbMedia(context);

      if (!success) {
        return PipelineStepResult.Fail(
            "USB media validation failed."
        );
      }

      return PipelineStepResult.Ok(
          "USB media validation successful."
      );
    }
    catch (Exception ex) {
      return PipelineStepResult.Fail(
          "USB media validation failed : " + ex.Message,
          ex
      );
    }
  }

  private PipelineStepResult CleanupWindowsMediaFolderStep() {
    try {
      CleanupWindowsMediaFolder();

      return PipelineStepResult.Ok(
          "WindowsMedia content cleaned."
      );
    }
    catch (Exception ex) {
      AppendBuildLog(
          "[WARNING] Unable to clean WindowsMedia : " + ex.Message
      );

      return PipelineStepResult.Warn(
          "Unable to clean WindowsMedia : " + ex.Message
      );
    }
  }

  private void InitializeUsbLiveReport(DateTime usbStartTime) {
    string deployPackage = Path.Combine(
        _profile.Package.OutputDirectory,
        _profile.Package.PackageName
    );

    string logDir = Path.Combine(
        deployPackage,
        "Logs"
    );

    Directory.CreateDirectory(logDir);

    _usbLiveReportPath = Path.Combine(
        logDir,
        $"USB_BUILD_REPORT_{usbStartTime:yyyyMMdd_HHmmss}.txt"
    );

    StringBuilder header = new();

    header.AppendLine("==================================================");
    header.AppendLine("DeployConfigurator USB Build Live Report");
    header.AppendLine("==================================================");
    header.AppendLine();
    header.AppendLine($"Start time : {usbStartTime:yyyy-MM-dd HH:mm:ss}");
    header.AppendLine($"Profile    : {_profile.Name}");
    header.AppendLine($"Package    : {_profile.Package.PackageName}");
    header.AppendLine();

    File.WriteAllText(
        _usbLiveReportPath,
        header.ToString(),
        Encoding.UTF8
    );

    AppendBuildLog(
        "[INFO] Live report initialized : " +
        _usbLiveReportPath
    );
  }

  private void AppendBuildLog(string line,bool allowEmptyLine = false) {
    if (!Dispatcher.CheckAccess()) {
      Dispatcher.BeginInvoke(
        new Action(
          () => AppendBuildLog(
            line,
            allowEmptyLine)));

      return;
    }
    if (string.IsNullOrWhiteSpace(line)) {
      if (!allowEmptyLine)
        return;

      previewBuildLog.Editor.AppendText(Environment.NewLine);
      AppendUsbLiveReport("",allowEmptyLine: true);
      return;
    }

    line = line.Trim();

    if (!ShouldLog7ZipLine(line))
      return;


    string stampedLine;

    if (DateTimeStampRegex().IsMatch(line)) {
      stampedLine = line;
    }
    else {
      stampedLine = $"[{DateTime.Now:HH:mm:ss}] {line}";
    }

    previewBuildLog.Editor.AppendText(
        stampedLine + Environment.NewLine
    );

    previewBuildLog.Editor.CaretPosition = previewBuildLog.Editor.Document.ContentEnd;
    previewBuildLog.Editor.ScrollToEnd();

    AppendUsbLiveReport(stampedLine);
  }

  private void AppendUsbLiveReport(string line,bool allowEmptyLine = false) {
    if (string.IsNullOrWhiteSpace(_usbLiveReportPath))
      return;

    if (string.IsNullOrWhiteSpace(line)) {
      if (!allowEmptyLine)
        return;

      lock (_usbLiveReportLock) {
        File.AppendAllText(
            _usbLiveReportPath,
            Environment.NewLine,
            Encoding.UTF8
        );
      }

      return;
    }

    lock (_usbLiveReportLock) {
      File.AppendAllText(
          _usbLiveReportPath,
          line + Environment.NewLine,
          Encoding.UTF8
      );
    }
  }

  private void SaveUsbBuildReport(DateTime usbStartTime) {
    string reportDir = Path.Combine(
        _profile.Package.OutputDirectory,
        _profile.Package.PackageName,
        "Reports"
    );

    Directory.CreateDirectory(reportDir);

    string reportPath = Path.Combine(
        reportDir,
        $"USB_BUILD_REPORT_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
    );

    StringBuilder report = new();

    report.AppendLine("==================================================");
    report.AppendLine("DeployConfigurator USB Build Report");
    report.AppendLine("==================================================");
    report.AppendLine();

    report.AppendLine($"Date       : {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
    report.AppendLine($"Profile    : {_profile.Name}");
    report.AppendLine($"Duration   : {(DateTime.Now - usbStartTime):hh\\:mm\\:ss}");
    report.AppendLine();

    report.AppendLine("==================================================");
    report.AppendLine("PIPELINE STEPS");
    report.AppendLine("==================================================");

    foreach (PipelineStepLog step in _pipelineSteps) {
      report.AppendLine(
          $"[{step.State}] {step.Name} - {step.Duration:hh\\:mm\\:ss} - {step.Message}"
      );
    }

    report.AppendLine();

    report.AppendLine("==================================================");
    report.AppendLine("FINAL STATUS");
    report.AppendLine("==================================================");

    bool hasError =
        _pipelineSteps.Any(
            s => s.State == PipelineState.Error
        );

    bool hasCancelled =
        _pipelineSteps.Any(
            s => s.State == PipelineState.Cancelled
        );

    bool hasWarning =
        _pipelineSteps.Any(
            s => s.State == PipelineState.Warning
        );

    string finalStatus =
        hasCancelled
            ? "CANCELLED"
            : hasError
                ? "FAILED"
                : hasWarning
                    ? "SUCCESS WITH WARNINGS"
                    : "SUCCESS";

    report.AppendLine(finalStatus);
    File.WriteAllText(
        reportPath,
        report.ToString(),
        Encoding.UTF8
    );

    AppendBuildLog(
        "[INFO] Build report saved : " +
        reportPath +
        Environment.NewLine
    );

    ExportUsbBuildArchive(reportPath);
  }

  private bool ShowModalWithFade(Window dialog) {
    double previousOpacity = Opacity;
    try {
      dialog.Owner = this;
      Opacity = 0.15;
      return
        dialog.ShowDialog() == true;
    }
    finally {
      Opacity = previousOpacity;

      Activate();
      Focus();
    }
  }

  private int GetStepProgressStart(string status) {
    string stepId = status.Length >= 2
        ? status.Substring(0,2)
        : "";

    return _pipelineProgressMap.TryGetValue(stepId,out var range)
        ? range.Start
        : 0;
  }

  private (int Start,int Span) GetStepProgressRange(string status) {
    string stepId =
        status.Length >= 2
            ? status.Substring(0,2)
            : "";

    return _pipelineProgressMap.TryGetValue(stepId,out var range)
        ? range
        : (0,100);
  }

  private void ReportStepProgress(int innerPercent,string message) {
    int mappedPercent =
        _currentStepProgressStart +
        (int)(innerPercent * _currentStepProgressSpan / 100.0);

    mappedPercent = Math.Clamp(mappedPercent,0,100);

    UpdateBuildProgress(
        mappedPercent,
        message
    );
  }

  private async Task<PipelineStepResult> RunPipelineStepAsync(string status,ImageSource image,Func<Task<PipelineStepResult>> action) {
    int stepStart = GetStepProgressStart(status);
    UpdateBuildProgress(stepStart,status);

    await WaitIfPipelinePausedAsync();

    AppendBuildLog("",allowEmptyLine: true);
    AppendBuildLog("==================================================");
    AppendBuildLog($"START {status}");
    AppendBuildLog("==================================================");

    DateTime startTime = DateTime.Now;

    (_currentStepProgressStart,_currentStepProgressSpan) = GetStepProgressRange(status);

    UpdateBuildProgress(
        _currentStepProgressStart,
        status
    );

    SetOperationIcon(image);
    SetOperationStatus(status);

    try {
      PipelineStepResult result =
          await action();

      PipelineState state =
          !result.Success
              ? PipelineState.Error
              : result.Warning
                  ? PipelineState.Warning
                  : PipelineState.Success;

      if (result.Warning)
        _pipelineHasWarning = true;

      _pipelineSteps.Add(new PipelineStepLog {
        Name = status,
        State = state,
        StartTime = startTime,
        EndTime = DateTime.Now,
        Message = result.Message
      });

      await WaitIfPipelinePausedAsync();

      if (!result.Success) {
        AppendBuildLog("[ERROR] " + status + " - " + result.Message);
      }
      else if (result.Warning) {
        AppendBuildLog("[WARNING] " + status + " - " + result.Message);
      }
      else {
        AppendBuildLog("[OK] " + status);
      }

      if (result.Success) {
        UpdateBuildProgress(
            _currentStepProgressStart + _currentStepProgressSpan,
            result.Warning ? "Terminé avec avertissement" : "Terminé"
        );
      }
      return result;
    }
    catch (OperationCanceledException) {
      _pipelineSteps.Add(new PipelineStepLog {
        Name = status,
        State = PipelineState.Cancelled,
        StartTime = startTime,
        EndTime = DateTime.Now,
        Message = "Cancelled"
      });

      throw;
    }
    catch (Exception ex) {
      AppendBuildLog("[ERROR] " + status + " - " + ex.Message);
      _pipelineSteps.Add(new PipelineStepLog {
        Name = status,
        State = PipelineState.Error,
        StartTime = startTime,
        EndTime = DateTime.Now,
        Message = ex.Message
      });

      throw;
    }
  }

  private PipelineStepResult InstallBootSectorStep(UsbPreparationContext context) {
    bool installBootSector =
        context.IsMbr && context.IsBios;

    if (!installBootSector) {
      AppendBuildLog(
          "Boot sector skipped: not MBR/BIOS." +
          Environment.NewLine
      );

      return PipelineStepResult.Ok(
          "Boot sector skipped: not MBR/BIOS."
      );
    }

    UsbPreparationResult result =
        UsbPreparationService.InstallBootSector(
            _profile.WindowsMedia.UsbDriveLetter,
            _profile.WindowsMedia.UsbDriveLetter
        );

    AppendUsbResult(result);

    if (!result.Success) {
      AppendBuildLog(
          "[WARNING] Boot sector installation failed." +
          Environment.NewLine +
          "[WARNING] USB files were copied successfully, but BIOS boot may erreur." +
          Environment.NewLine
      );

      return PipelineStepResult.Warn(
          "Boot sector installation failed. USB copied, BIOS boot may erreur."
      );
    }

    return PipelineStepResult.Ok(
        "Boot sector installed successfully."
    );
  }

  private async Task<bool> BuildFinalIsoAsync() {
    SetOperationIcon(LoadImageResource("archive.png"));
    SetOperationStatus("05 - Build final ISO...");

    PipelineResult result =
        await Task.Run(() =>
            _pipeline!.BuildIso(_profile)
        );

    AppendPipelineResult(result);

    if (!result.Success) {
      MessageBox.Show(
          result.Message,
          "Build Final ISO",
          MessageBoxButton.OK,
          MessageBoxImage.Error
      );

      return false;
    }

    return true;
  }

  private static string GetRichText(RichTextBox richTextBox) {
    return new TextRange(richTextBox.Document.ContentStart,richTextBox.Document.ContentEnd).Text;
  }

  private static void SetRichText(RichTextBox richTextBox,string text) {
    new TextRange(
      richTextBox.Document.ContentStart,
      richTextBox.Document.ContentEnd)
      .Text = text;
  }

  private void PreviewDiskPrep_OpenInNotepadPlusPlusRequested(object? sender,EventArgs e) {
    OpenTextInNotepadPlusPlus(GetRichText(previewDiskPrep.Editor),"diskprep.cmd");
  }

  private void SetOperationStatus(string text) {
    txtOperationStatus.Text = text;
  }

  private void ClearOperationStatus() {
    txtOperationStatus.Clear();
  }

  private void SetOperationIcon(ImageSource image) {
    picOperation.Source = image;
  }

  private bool ValidatePrepareUsbPrerequisites() {
    if (_profile == null) {
      MessageBox.Show(
          "Aucun profil chargé.",
          "Prepare USB",
          MessageBoxButton.OK,
          MessageBoxImage.Warning
      );

      return false;
    }

    if (string.IsNullOrWhiteSpace(
            _profile.WindowsMedia.MediaFolder)) {
      MessageBox.Show(
          "Le dossier WindowsMedia n'est pas défini.",
          "Prepare USB",
          MessageBoxButton.OK,
          MessageBoxImage.Warning
      );

      return false;
    }

    return true;
  }

  private void PrepareUsbUiStart() {
    previewBuildLog.Editor.Document.Blocks.Clear();
    UpdateBuildProgress(0,"Preparing USB pipeline...");
  }

  private UsbPreparationContext SelectUsbTarget() {
    SetOperationIcon(LoadImageResource("usb.png"));
    SetOperationStatus("01 - Select USB media...");
    List<UsbDeviceInfo> devices = UsbPreparationService.DetectUsbDevices();
    UsbTargetConfirmWindow confirmWindow = new(devices) {
      Owner = this
    };
    if (confirmWindow.ShowDialog() != true) {
      return new UsbPreparationContext {
        Cancelled = true
      };
    }
    UsbDeviceInfo selectedUsb = confirmWindow.SelectedUsbDevice!;
    _profile.WindowsMedia.UsbDriveLetter = selectedUsb.DriveLetters;

    return new UsbPreparationContext {
      SelectedUsb = selectedUsb,
      PartitionType = confirmWindow.SelectedPartitionType,
      FileSystem = confirmWindow.SelectedFileSystem,
      VolumeLabel = confirmWindow.SelectedVolumeLabel,
      TargetSystem = confirmWindow.SelectedTargetSystem,
      SplitWimIfNeeded = confirmWindow.SplitWimIfNeeded
    };
  }

  private void WriteDiskpartPreview(UsbPreparationContext context) {
    string script =
        UsbPreparationService.GenerateDiskpartScript(
            context.SelectedUsb,
            context.PartitionType,
            context.FileSystem,
            context.VolumeLabel,
            context.TargetSystem
        );

    AppendBuildLog(
        "=== DISKPART PREVIEW ===" +
        Environment.NewLine +
        script +
        Environment.NewLine +
        Environment.NewLine
    );
  }

  private async Task<bool> BuildDeploymentPackageAsync() {
    SetOperationIcon(LoadImageResource("deploy.png"));
    SetOperationStatus("02 - Build package deploy...");

    PipelineResult result =
        await Task.Run(() =>
            BuildPipelineService.BuildPackage(_profile)
        );

    AppendPipelineResult(result);

    if (!result.Success) {
      MessageBox.Show(
          result.Message,
          "Build Package",
          MessageBoxButton.OK,
          MessageBoxImage.Error
      );

      return false;
    }

    return true;
  }

  private async Task<bool> BuildWindowsMediaAsync() {
    SetOperationIcon(LoadImageResource("CopyFile.png"));
    SetOperationStatus("03 - Build Windows media...");

    _pipeline!.ProgressChanged += (value,message) => {
      UpdateBuildProgress(value,message);
    };

    PipelineResult result =
        await Task.Run(() =>
            _pipeline!.BuildWindowsMedia(_profile)
        );

    AppendPipelineResult(result);

    if (!result.Success) {
      MessageBox.Show(
          result.Message,
          "Build Windows Media",
          MessageBoxButton.OK,
          MessageBoxImage.Error
      );

      return false;
    }

    return true;
  }

  private bool FormatUsb(UsbPreparationService service,UsbPreparationContext context) {
    SetOperationIcon(LoadImageResource("usb.png"));
    SetOperationStatus("07 - Format USB...");
    UsbPreparationResult result =
        service.FormatUsbMbrNtfs(
            context.SelectedUsb,
            context.PartitionType,
            context.FileSystem,
            context.VolumeLabel,
            context.TargetSystem
        );

    AppendUsbResult(result);

    if (!result.Success) {
      MessageBox.Show(
          "USB format failed.",
          "USB",
          MessageBoxButton.OK,
          MessageBoxImage.Error
      );

      return false;
    }

    return true;
  }

  private void CleanupWindowsMediaFolder() {
    SetOperationIcon(LoadImageResource("check.png"));
    SetOperationStatus("06 - Nettoyage dossier WindowsMedia...");

    try {
      string mediaFolder =
          _profile.WindowsMedia.MediaFolder;

      if (!Directory.Exists(mediaFolder))
        return;

      CleanDirectory(mediaFolder);

      AppendBuildLog(
          "[INFO] WindowsMedia content cleaned." +
          Environment.NewLine
      );
    }
    catch (Exception ex) {
      AppendBuildLog(
          "[WARNING] Unable to clean WindowsMedia : " +
          ex.Message +
          Environment.NewLine
      );
    }
  }

  private static void CleanDirectory(string path) {
    DirectoryInfo dir = new(path);

    foreach (FileInfo file in dir.GetFiles()) {
      file.IsReadOnly = false;
      file.Delete();
    }

    foreach (DirectoryInfo subDir in dir.GetDirectories()) {
      subDir.Delete(true);
    }
  }

  private bool ValidateUsbMedia(UsbPreparationContext context) {

    UpdateBuildProgress(99,"VALIDATE USB");
    SetOperationIcon(LoadImageResource("gear.png"));
    SetOperationStatus("10 - Validation du média USB...");

    UsbPreparationResult validationResult =
        UsbPreparationService.ValidatePreparedUsb(
            _profile.WindowsMedia.UsbDriveLetter
        );

    AppendUsbResult(validationResult);

    if (!validationResult.Success) {
      MessageBox.Show(
          "USB validation failed.",
          "USB",
          MessageBoxButton.OK,
          MessageBoxImage.Error
      );

      return false;
    }

    if (context.IsUefi) {
      UsbPreparationResult uefiResult =
          UsbPreparationService.ValidatePreparedUsbUefi(
              _profile.WindowsMedia.UsbDriveLetter
          );

      AppendUsbResult(uefiResult);

      if (!uefiResult.Success) {
        MessageBox.Show(
            "UEFI validation failed.",
            "USB",
            MessageBoxButton.OK,
            MessageBoxImage.Warning
        );

        return false;
      }
    }

    return true;
  }

  private void CompleteUsbPreparation(DateTime usbStartTime) {
    TimeSpan duration = DateTime.Now - usbStartTime;

    AppendBuildLog(
        Environment.NewLine +
        "=== USB READY ===" +
        Environment.NewLine +
        "Total duration : " +
        duration.ToString(@"hh\:mm\:ss") +
        Environment.NewLine
    );

    UpdateBuildProgress(100,"USB READY");

    MessageBox.Show(
        "USB media successfully prepared.",
        "USB",
        MessageBoxButton.OK,
        MessageBoxImage.Information
    );
  }

  private void HandleUsbPreparationCancelled() {
    AppendBuildLog(
        Environment.NewLine +
        "[CANCELLED] USB preparation cancelled." +
        Environment.NewLine
    );

    UpdateBuildProgress(
        0,
        "USB preparation cancelled"
    );
  }

  private bool HandleFat32SplitIfNeeded(UsbPreparationService service,UsbPreparationContext context) {

    SetOperationIcon(LoadImageResource("gear.png"));
    SetOperationStatus("04 - Fat32 / Split WIM...");

    string installWimPath = Path.Combine(
        _profile.WindowsMedia.MediaFolder,
        "sources",
        "install.wim"
    );

    bool isFat32 = context.FileSystem.Equals("FAT32",StringComparison.OrdinalIgnoreCase);

    bool installWimExists = File.Exists(installWimPath);

    bool installWimTooLarge = installWimExists && new FileInfo(installWimPath).Length >= 4L * 1024 * 1024 * 1024;

    AppendBuildLog(
        $"[INFO] Split check: FS={context.FileSystem}, " +
        $"WIM exists={installWimExists}, " +
        $"WIM >4GB={installWimTooLarge}, " +
        $"Split enabled={context.SplitWimIfNeeded}" +
        Environment.NewLine
    );

    bool needSplit = isFat32 && installWimTooLarge;

    if (needSplit && !context.SplitWimIfNeeded) {
      MessageBox.Show(
          "Le fichier install.wim dépasse 4 Go. FAT32 ne peut pas le contenir." +
          Environment.NewLine +
          "Active le découpage WIM ou choisis NTFS.",
          "FAT32 incompatible",
          MessageBoxButton.OK,
          MessageBoxImage.Warning
      );

      return false;
    }

    UsbPreparationResult splitResult =
        service.SplitInstallWimIfNeeded(
            _profile.WindowsMedia.MediaFolder,
            context.FileSystem,
            context.SplitWimIfNeeded
        );

    AppendUsbResult(splitResult);

    if (!splitResult.Success) {
      MessageBox.Show(
          "Split install.wim failed.",
          "USB",
          MessageBoxButton.OK,
          MessageBoxImage.Error
      );

      return false;
    }

    if (!needSplit) {
      AppendBuildLog(
          "[INFO] install.wim deletion skipped: split was not required." +
          Environment.NewLine
      );

      return true;
    }

    if (!File.Exists(installWimPath)) {
      AppendBuildLog(
          "[INFO] install.wim already absent after split." +
          Environment.NewLine
      );

      return true;
    }

    try {
      File.Delete(installWimPath);

      AppendBuildLog(
          "[INFO] install.wim deleted after successful split." +
          Environment.NewLine
      );
    }
    catch (Exception ex) {
      MessageBox.Show(
          "Impossible de supprimer install.wim après split." +
          Environment.NewLine +
          ex.Message,
          "Split WIM",
          MessageBoxButton.OK,
          MessageBoxImage.Warning
      );

      return false;
    }

    return true;
  }

  private void HandleUsbPreparationException(Exception ex) {
    AppendBuildLog(
        Environment.NewLine +
        "[EXCEPTION] " +
        ex +
        Environment.NewLine
    );

    MessageBox.Show(
        ex.Message,
        "Prepare USB",
        MessageBoxButton.OK,
        MessageBoxImage.Error
    );
  }

  private void AppendUsbResult(UsbPreparationResult result) {
    foreach (string line in result.Logs)
      AppendBuildLog(
          line + Environment.NewLine
      );

    foreach (string error in result.Errors)
      AppendBuildLog(
          "[ERROR] " +
          error +
          Environment.NewLine
      );
  }

  private void AppendPipelineResult(PipelineResult result) {
    foreach (string line in result.Logs)
      AppendBuildLog(
          line + Environment.NewLine
      );
  }

  private sealed class UsbPreparationContext {
    public bool Cancelled { get; set; }
    public UsbDeviceInfo SelectedUsb { get; set; } = null!;
    public string PartitionType { get; set; } = "";
    public string FileSystem { get; set; } = "";
    public string VolumeLabel { get; set; } = "";
    public string TargetSystem { get; set; } = "";
    public bool SplitWimIfNeeded { get; set; }
    public bool IsUefi => TargetSystem.Contains("UEFI",StringComparison.OrdinalIgnoreCase);
    public bool IsBios => TargetSystem.Contains("BIOS",StringComparison.OrdinalIgnoreCase);
    public bool IsMbr => PartitionType.Contains("MBR",StringComparison.OrdinalIgnoreCase);
  }

  private void SaveUsbPreparationLog() {
    string logsRoot = Path.Combine(
        _profile.Package.OutputDirectory,
        _profile.Package.PackageName,
        "Logs"
    );

    Directory.CreateDirectory(logsRoot);

    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

    string logPath = Path.Combine(
        logsRoot,
        $"usb_preparation_{timestamp}.log"
    );
  }

  private void ToggleBuildButtons(bool enabled) {
    btnBuildPackage.IsEnabled = true;
    btnBuildWindowsMedia.IsEnabled = true;
    btnPrepareUSB.IsEnabled = true;

    btnCancelBuild.IsEnabled = true;
  }

  private UsbPreparationService CreateUsbPreparationService() {
    UsbPreparationService service = new();

    service.ProgressCallback += (value,message) => {
      if (!Dispatcher.CheckAccess()) {
        Dispatcher.BeginInvoke(
          new Action(
            () => UpdateBuildProgress(value,message)));
        return;
      }

      UpdateBuildProgress(value,message);
    };
    return service;
  }

  private void menuOemOptions_Click(
      object? sender,
      EventArgs e) {
    OemOptionsWindow window = new(_profile.Oem);

    if (!ShowModalWithFade(window))
      return;

    _profile.Oem = window.Oem;

    RefreshPreview();
    UpdateStatusBar();
  }

  private bool ValidateBuildReadiness() {
    BuildValidationService validation = new();

    List<ValidationIssue> issues =
        BuildValidationService.Validate(_profile);

    previewprofileBuildReport.Editor.Document.Blocks.Clear();

    foreach (ValidationIssue issue in issues) {
      previewprofileBuildReport.Editor.AppendText(
          $"[{issue.Severity}] {issue.Message}" +
          Environment.NewLine
      );
    }

    bool hasError =
        issues.Any(x => x.Severity == "ERROR");

    if (issues.Count == 0) {
      previewprofileBuildReport.Content = "[OK] Build validation success.";
    }

    return !hasError;
  }

  private void btnCancelBuild_Click(object sender,EventArgs e) {
    _buildCancellation?.Cancel();

    lblBuildProgress.Content = "Annulation demandée...";
    btnCancelBuild.IsEnabled = false;
  }

  private void btnBrowseFinalIsoPath_Click(object sender,EventArgs e) {
    SaveFileDialog dialog = new() {
      Filter = "ISO files (*.iso)|*.iso",
      FileName = "WindowsMedia.iso"
    };

    if (!string.IsNullOrWhiteSpace(txtFinalIsoPath.Text)) {
      string? dir = Path.GetDirectoryName(txtFinalIsoPath.Text);

      if (!string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir))
        dialog.InitialDirectory = dir;

      dialog.FileName = Path.GetFileName(txtFinalIsoPath.Text);
    }

    if (dialog.ShowDialog() == true)
      txtFinalIsoPath.Text = dialog.FileName;
  }

  private void UpdateIsoOutputFromMediaFolder() {
    if (string.IsNullOrWhiteSpace(txtMediaWorkingDirectory.Text))
      return;

    string mediaFolder = txtMediaWorkingDirectory.Text.Trim();

    string parent =
        Directory.GetParent(mediaFolder)?.FullName
        ?? mediaFolder;

    txtFinalIsoPath.Text =
        Path.Combine(parent,"WindowsMedia.iso");
  }

  private void btnBrowseMediaWorkingDirectory_Click(object sender,EventArgs e) {
    BrowseFolderInto(txtMediaWorkingDirectory);
    UpdateIsoOutputFromMediaFolder();
  }

  private async void btnBuildAll_Click(object sender,EventArgs e) {
    ApplyUiToProfile();

    if (!ValidateBuildReadiness()) {
      MessageBox.Show(
          "La validation du build a échoué. Voir l'onglet Build Report.",
          "Validation",
          MessageBoxButton.OK,
          MessageBoxImage.Warning
      );

      return;
    }

    progressBuild.Value = 0;
    lblBuildProgress.Content = "Build All...";
    previewBuildLog.Editor.Document.Blocks.Clear();

    btnBuildAll.IsEnabled = false;
    btnBuildPackage.IsEnabled = false;
    btnBuildWindowsMedia.IsEnabled = false;
    btnBuildIso.IsEnabled = false;
    btnCancelBuild.IsEnabled = true;

    _buildCancellation = new CancellationTokenSource();

    try {
      BuildPipelineService service = CreatePipelineService();

      PipelineResult result =
          await service.BuildAllAsync(
              _profile,
              _buildCancellation.Token
          );

      AppendPipelineLogs(result);

      lblBuildProgress.Content = result.Success ? "Build terminé" : "Erreur";

      MessageBox.Show(
          result.Message,
          "Build All",
          MessageBoxButton.OK,
          result.Success
              ? MessageBoxImage.Information
              : MessageBoxImage.Warning
      );
    }
    finally {
      _buildCancellation?.Dispose();
      _buildCancellation = null;

      btnBuildAll.IsEnabled = true;
      btnBuildPackage.IsEnabled = true;
      btnBuildWindowsMedia.IsEnabled = true;
      btnBuildIso.IsEnabled = true;
      btnCancelBuild.IsEnabled = false;
    }
  }

  private async void btnBuildIso_Click(
      object sender,
      EventArgs e) {
    ApplyUiToProfile();

    progressBuild.Value = 0;
    lblBuildProgress.Content = "Démarrage...";

    BuildPipelineService service =
        CreatePipelineService();

    PipelineResult result =
        await Task.Run(() =>
            service.BuildIso(_profile)
        );

    AppendPipelineLogs(result);

    lblBuildProgress.Content =
        result.Success
            ? "Terminé"
            : "Erreur";

    MessageBox.Show(
        result.Message,
        "Build ISO",
        MessageBoxButton.OK,
        result.Success
            ? MessageBoxImage.Information
            : MessageBoxImage.Error
    );
  }

  private void UpdateBuildProgress(int value,string message) {
    if (!Dispatcher.CheckAccess()) {
      Dispatcher.Invoke(() => UpdateBuildProgress(value,message));
      return;
    }

    value = Math.Clamp(value,0,100);
    progressBuild.Value = value;

    if (message.StartsWith("Fichier ",StringComparison.OrdinalIgnoreCase)) {
      lblBuildProgress.Content = $"EXTRACTION ISO - {value}%";
      lblCurrentFile.Content = message;
      return;
    }

    if (message.StartsWith("USB copy",StringComparison.OrdinalIgnoreCase)) {
      lblBuildProgress.Content = $"COPY USB - {value}%";
      lblCurrentFile.Content = message;
      return;
    }

    lblBuildProgress.Content = $"{message} - {value}%";
    lblCurrentFile.Content = "";
  }

  private void btnOpenMediaFolder_Click(object sender,EventArgs e) {
    string path = txtMediaWorkingDirectory.Text;

    if (!Directory.Exists(path)) {
      MessageBox.Show(
          "Media folder not found.",
          "Windows Media",
          MessageBoxButton.OK,
          MessageBoxImage.Warning
      );

      return;
    }

    Process.Start(new ProcessStartInfo {
      FileName = path,
      UseShellExecute = true
    });
  }

  private async void btnBuildWindowsMedia_Click(object sender,EventArgs e) {
    ApplyUiToProfile();

    progressBuild.Value = 0;
    lblBuildProgress.Content = "Démarrage...";

    BuildPipelineService service = CreatePipelineService();

    PipelineResult result =
        await Task.Run(() => service.BuildWindowsMedia(_profile));

    AppendPipelineLogs(result);

    lblBuildProgress.Content = result.Success ? "Terminé" : "Erreur";

    MessageBox.Show(
        result.Message,
        "Build Windows Media",
        MessageBoxButton.OK,
        result.Success
            ? MessageBoxImage.Information
            : MessageBoxImage.Error
    );
  }

  private void btnBrowseWindowsIso_Click(object sender,EventArgs e) {
    OpenFileDialog dialog = new() {
      Filter = "ISO files (*.iso)|*.iso",
      Title = "Select Windows ISO"
    };

    if (dialog.ShowDialog() == true) {
      txtWindowsIsoPath.Text = dialog.FileName;
    }
    Owner = this;
  }

  private void LoadKeyboardLocales() {
    lstInputLocales.Items.Clear();

    foreach (var locale in _keyboardLocales)
      lstInputLocales.Items.Add(locale);
  }

  private void AppendPipelineLogs(PipelineResult result) {
    if (result.Logs.Count == 0)
      return;

    AppendBuildLog(
        Environment.NewLine +
        "==================================================" +
        Environment.NewLine
    );

    foreach (string line in result.Logs)
      AppendBuildLog(
          line + Environment.NewLine
      );

    previewBuildLog.Editor.CaretPosition = previewBuildLog.Editor.Document.ContentEnd;
    previewBuildLog.Editor.ScrollToEnd();
  }

  private void DeferredRefresh_Leave(object? sender,EventArgs e) {
    ValidateConfiguration();
    UpdateStatusBar();
  }

  private void btnApplyOrchestration_Click(object sender,EventArgs e) {
    ValidateConfiguration();
    RefreshPreview();
    UpdateStatusBar();

    _orchestrationPendingChanges = false;
    lblOrchestrationPending.Visibility = Visibility.Collapsed;
  }

  private void btnBrowseSetupScripts_Click(object sender,EventArgs e) {
    BrowseFolderInto(txtSetupScriptsSourcePath);
  }

  private void btnBrowseSetupConfig_Click(object sender,EventArgs e) {
    BrowseFolderInto(txtSetupConfigSourcePath);
  }

  private void btnBrowseOutputDirectory_Click(object sender,EventArgs e) {
    BrowseFolderInto(txtOutputDirectory);
  }

  private void btnBrowseDrivers_Click(object sender,EventArgs e) {
    BrowseFolderInto(txtDriversSourcePath);
  }

  private void btnBrowseApplications_Click(object sender,EventArgs e) {
    BrowseFolderInto(txtApplicationsSourcePath);
  }

  //private void btnBrowseScripts_Click(object sender,EventArgs e) {
  //  BrowseFolderInto(txtSetupScriptsSourcePath);
  //}

  //private void btnBrowseWinPE_Click(object sender,EventArgs e) {
  //  BrowseFolderInto(txtWinPESourcePath);
  //}

  private void ConfigurationChanged(object? sender,EventArgs e) {
    Debug.WriteLine($"CONFIG CHANGE from: {(sender as Control)?.Name}");
    if (!_livePreviewReady)
      return;

    if (_isRefreshingPreview)
      return;

    try {
      _isRefreshingPreview = true;

      UpdateUiState();
      ValidateConfiguration();
      RefreshPreview();
      UpdateStatusBar();
    }
    finally {
      _isRefreshingPreview = false;
    }
  }

  private void btnOpenPackageFolder_Click(object sender,EventArgs e) {
    ApplyUiToProfile();

    if (string.IsNullOrWhiteSpace(_profile.Package.OutputDirectory)) {
      MessageBox.Show(
          "Le dossier de sortie n'est pas défini.",
          "Package",
          MessageBoxButton.OK,
          MessageBoxImage.Warning
      );
      return;
    }

    string packagePath = Path.Combine(
        _profile.Package.OutputDirectory,
        _profile.Package.PackageName
    );

    if (!Directory.Exists(packagePath)) {
      MessageBox.Show(
          "Le dossier package n'existe pas encore :" +
          Environment.NewLine +
          packagePath,
          "Package",
          MessageBoxButton.OK,
          MessageBoxImage.Information
      );
      return;
    }

    Process.Start(new ProcessStartInfo {
      FileName = packagePath,
      UseShellExecute = true
    });
  }

  private void txtPreview_TextChanged(object? sender,EventArgs e) {
    if (_isUpdatingPreview)
      return;

    ApplySyntaxHighlighting();
  }

  private void txtPreview_DoubleClick(object sender,MouseButtonEventArgs e) {
    RichTextBox textBox = CurrentPreviewTextBox;
    string text = GetRichText(textBox)
        .Replace("\r\n","\n")
        .Replace('\r','\n');
    string[] lines = text.Split('\n');
    // Position actuelle du curseur dans le document
    string textBeforeCaret = new TextRange(textBox.Document.ContentStart,textBox.CaretPosition)
      .Text
      .Replace("\r\n","\n")
      .Replace('\r','\n');
    int lineIndex = textBeforeCaret.Count(character => character == '\n');

    if (lineIndex < 0 || lineIndex >= lines.Length) {
      return;
    }
    string sectionLine = string.Empty;
    for (int i = lineIndex;i >= 0;i--) {
      string candidate = lines[i].Trim();
      if (IsSectionTitle(candidate)) {
        sectionLine = candidate;
        break;
      }
    }
    if (string.IsNullOrWhiteSpace(sectionLine))
      return;
    if (!_collapsedSections.Remove(sectionLine)) {
      _collapsedSections.Add(sectionLine);
    }
    ApplySyntaxHighlighting();
  }

  private void btnFindNext_Click(object sender,EventArgs e) {
    FindNext();
  }

  private void btnCloseSearch_Click(object sender,EventArgs e) {
    txtSearch.Clear();
    CurrentPreviewTextBox.Focus();
  }

  private void btnPreview_Click(object sender,EventArgs e) {
    RefreshPreview();
  }

  private async void btnBuildPackage_Click(object sender,EventArgs e) {
    await BuildPackage();
  }

  private void menuPrintSelectedView_Click(object sender,RoutedEventArgs e) {
    bool flowControl = PrintSelectedView();
    if (!flowControl) {
      return;
    }
  }

  private bool PrintSelectedView() {
    if (!TryGetSelectedPreviewEditor(out RichTextBox textBox)) {
      MessageBox.Show(
          "No view selected..",
          "Print",
          MessageBoxButton.OK,
          MessageBoxImage.Warning);
      return false;
    }
    string text = GetEditorText(textBox);
    if (string.IsNullOrWhiteSpace(text)) {
      MessageBox.Show(
        "There is no content to print.",
        "Print",
          MessageBoxButton.OK,
          MessageBoxImage.Warning);
      return false;
    }
    PrintDialog dialog = new();
    if (dialog.ShowDialog() != true)
      return false;
    string documentName = (TabMain.SelectedItem as TabItem)?.Header?.ToString() ?? "preview";

    DocumentPaginator paginator = ((IDocumentPaginatorSource)textBox.Document).DocumentPaginator;
    paginator.PageSize = new Size(dialog.PrintableAreaWidth,dialog.PrintableAreaHeight);
    dialog.PrintDocument(paginator,documentName);
    return true;
  }

  private void menuSaveProfile_Click(object? sender,EventArgs e) {
    ApplyUiToProfile();

    SaveFileDialog dialog = new() {
      Filter = "Deploy Profile (*.deploy.json)|*.deploy.json",
      FileName = "default.deploy.json"
    };

    Owner = this;

    if (dialog.ShowDialog() != true)
      return;

    ProfileSerializer.Save(dialog.FileName,_profile);

    _currentProfilePath = dialog.FileName;

    UpdateStatusBar();
  }

  private void menuNewProfile_Click(object? sender,EventArgs e) {
    _profile = new DeploymentProfile();
    _currentProfilePath = "";
    LoadProfileToUi();
  }

  private void menuExit_Click(object? sender,EventArgs e) {
    if (_orchestrationPendingChanges)
      Close();
  }

  private void tabPreview_SelectedIndexChanged(object? sender,SelectionChangedEventArgs e) {

    if (e.OriginalSource != TabMain)
      return;

    // Build Log est alimenté directement par le pipeline.
    if (TabMain.SelectedIndex == 7)
      return;

    _activePreviewDocument = TabMain.SelectedIndex switch {
      1 => PreviewDocumentType.SetupComplete,
      2 => PreviewDocumentType.Orchestrator,
      3 => PreviewDocumentType.AutoUnattend,
      4 => PreviewDocumentType.Unattend,
      5 => PreviewDocumentType.ProfileJson,
      6 => PreviewDocumentType.BuildReport,
      _ => PreviewDocumentType.Diskprep
    };

    RefreshPreview();
  }
  private async Task WaitIfPipelinePausedAsync() {
    if (!_isPaused)
      return;

    SetOperationStatus("PAUSE - pipeline suspendu");

    while (_isPaused) {
      await Task.Delay(200);
    }

    SetOperationStatus("REPRISE...");
  }

  private void btnBuildPackageFolder_Click(object sender,EventArgs e) {
    ApplyUiToProfile();

    string buildAllPath = _profile.WindowsMedia.MediaFolder;

    if (string.IsNullOrWhiteSpace(buildAllPath)) {
      MessageBox.Show(
          "Le dossier Windows Media n'est pas défini.",
          "Dossier Build All",
          MessageBoxButton.OK,
          MessageBoxImage.Warning
      );

      return;
    }

    if (!Directory.Exists(buildAllPath)) {
      MessageBox.Show(
          "Le dossier Build All n'existe pas encore :" +
          Environment.NewLine +
          buildAllPath,
          "Dossier Build All",
          MessageBoxButton.OK,
          MessageBoxImage.Information
      );

      return;
    }

    Process.Start(new ProcessStartInfo {
      FileName = buildAllPath,
      UseShellExecute = true
    });
  }

  private void TogglePauseResume() {
    if (!_isPaused) {
      _isPaused = true;
      _pauseEvent.Reset();

      imgPauseResume.Source = LoadImageResource("resume.png");

      SetOperationStatus("PAUSE - attente fin opération en cours...");
    }
    else {
      _isPaused = false;
      _pauseEvent.Set();

      imgPauseResume.Source = LoadImageResource("pause.png");

      SetOperationStatus("REPRISE...");
    }
  }

  private void btnPauseResume_Click(object sender,EventArgs e) {
    TogglePauseResume();
  }
  private void mnuAbout_Click(object? sender,EventArgs e) {
    HelpHelper.mnuAbout(this,_profile);
  }

  private async void mnuCheckForUpdates_Click(object? sender,EventArgs e) {
    await HelpHelper.mnuCheckForUpdates(this,_profile);
  }

  private void mnuLicense_Click(object? sender,EventArgs e) {
    LicenseHelper.AfficherLicence(this,_licenseService,_profile);
  }
  private void mnuImportLicense_Click(object? sender,EventArgs e) {
    LicenseHelper.ImporterLicence(this,_licenseService);
  }
}
