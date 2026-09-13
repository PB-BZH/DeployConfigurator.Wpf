using System.IO;
using System.Windows;
using System.Windows.Controls;
using DeployConfigurator.Wpf.Core.Models;
using Microsoft.Win32;
using PB.BZH.Help.Wpf.UI.Theming;

namespace DeployConfigurator.Wpf.UI.Windows;

public partial class OemOptionsWindow: Window {
  public OemConfiguration Oem { get; private set; }

  public OemOptionsWindow(OemConfiguration oem) {
    InitializeComponent();
    Oem = Clone(oem);
    ThemeManager.SetTheme(AppTheme.Dark);
    LoadToUi();
    UpdateUiState();
  }

  private static OemConfiguration Clone(OemConfiguration source) {
    return new OemConfiguration {
      EnableOemInformation = source.EnableOemInformation,
      Manufacturer = source.Manufacturer,
      Model = source.Model,
      SupportPhone = source.SupportPhone,
      SupportUrl = source.SupportUrl,
      SupportHours = source.SupportHours,
      EnableWallpaper = source.EnableWallpaper,
      WallpaperPath = source.WallpaperPath,
      EnableLogo = source.EnableLogo,
      LogoPath = source.LogoPath,
      HideEulaPage = source.HideEulaPage,
      ProtectYourPc = source.ProtectYourPc,
      HideOnlineAccountScreens = source.HideOnlineAccountScreens,
      HideOEMRegistrationScreen = source.HideOEMRegistrationScreen,
      HideWirelessSetupInOobe = source.HideWirelessSetupInOobe,
      SupportAppUrl = source.SupportAppUrl,
      SupportProvider = source.SupportProvider,

      UseManufacturer = source.UseManufacturer,
      UseModel = source.UseModel,
      UseSupportAppUrl = source.UseSupportAppUrl,
      UseSupportUrl = source.UseSupportUrl,
      UseSupportHours = source.UseSupportHours,
      UseSupportProvider = source.UseSupportProvider,
      UseSupportPhone = source.UseSupportPhone,
    };
  }

  private void LoadToUi() {
    chkEnableOemInformation.IsChecked = Oem.EnableOemInformation;

    txtManufacturer.Text = Oem.Manufacturer;
    txtModel.Text = Oem.Model;
    txtSupportAppUrl.Text = Oem.SupportAppUrl;
    txtSupportHours.Text = Oem.SupportHours;
    txtSupportPhone.Text = Oem.SupportPhone;
    txtSupportUrl.Text = Oem.SupportUrl;
    txtSupportProvider.Text = Oem.SupportProvider;


    chkEnableLogo.IsChecked = Oem.EnableLogo;
    txtLogoPath.Text = Oem.LogoPath;

    chkEnableWallpaper.IsChecked = Oem.EnableWallpaper;
    txtWallpaperPath.Text = Oem.WallpaperPath;

    chkHideEulaPage.IsChecked = Oem.HideEulaPage;
    numProtectYourPc.Value = Oem.ProtectYourPc;
    chkHideOnlineAccountScreens.IsChecked = Oem.HideOnlineAccountScreens;
    chkHideOEMRegistrationScreen.IsChecked = Oem.HideOEMRegistrationScreen;
    chkHideWirelessSetupInOobe.IsChecked = Oem.HideWirelessSetupInOobe;

    chkUseManufacturer.IsChecked = Oem.UseManufacturer;
    chkUseModel.IsChecked = Oem.UseModel;
    chkUseSupportAppUrl.IsChecked = Oem.UseSupportAppUrl;
    chkUseSupportUrl.IsChecked = Oem.UseSupportUrl;
    chkUseSupportHours.IsChecked = Oem.UseSupportHours;
    chkUseSupportProvider.IsChecked = Oem.UseSupportProvider;
    chkUseSupportPhone.IsChecked = Oem.UseSupportPhone;

    foreach (
      CheckBox checkBox
      in new[] {
        chkEnableOemInformation,
        chkEnableLogo,
        chkEnableWallpaper,
        chkUseManufacturer,
        chkUseModel,
        chkUseSupportAppUrl,
        chkUseSupportUrl,
        chkUseSupportHours,
        chkUseSupportProvider,
        chkUseSupportPhone
      }) {

      HookUpdateUiState(checkBox);
    }
  }

  private void HookUpdateUiState(CheckBox checkBox) {
    checkBox.Checked += (_,_) => UpdateUiState();
    checkBox.Unchecked += (_,_) => UpdateUiState();
  }

  private void UpdateUiState() {
    bool oemEnabled = chkEnableOemInformation.IsChecked == true;

    txtManufacturer.IsEnabled = oemEnabled;
    txtModel.IsEnabled = oemEnabled;
    txtSupportPhone.IsEnabled = oemEnabled;
    txtSupportUrl.IsEnabled = oemEnabled;
    txtSupportHours.IsEnabled = oemEnabled;
    txtSupportAppUrl.IsEnabled = oemEnabled;
    txtSupportProvider.IsEnabled = oemEnabled;

    txtLogoPath.IsEnabled = chkEnableLogo.IsChecked == true;
    btnBrowseLogo.IsEnabled = chkEnableLogo.IsChecked == true;

    txtWallpaperPath.IsEnabled = chkEnableWallpaper.IsChecked == true;
    btnBrowseWallpaper.IsEnabled = chkEnableWallpaper.IsChecked == true;

    chkUseManufacturer.IsEnabled = oemEnabled;
    chkUseModel.IsEnabled = oemEnabled;
    chkUseSupportAppUrl.IsEnabled = oemEnabled;
    chkUseSupportUrl.IsEnabled = oemEnabled;
    chkUseSupportHours.IsEnabled = oemEnabled;
    chkUseSupportProvider.IsEnabled = oemEnabled;
    chkUseSupportPhone.IsEnabled = oemEnabled;

    txtManufacturer.IsEnabled = oemEnabled && chkUseManufacturer.IsChecked == true;
    txtModel.IsEnabled = oemEnabled && chkUseModel.IsChecked == true;
    txtSupportAppUrl.IsEnabled = oemEnabled && chkUseSupportAppUrl.IsChecked == true;
    txtSupportUrl.IsEnabled = oemEnabled && chkUseSupportUrl.IsChecked == true;
    txtSupportHours.IsEnabled = oemEnabled && chkUseSupportHours.IsChecked == true;
    txtSupportProvider.IsEnabled = oemEnabled && chkUseSupportProvider.IsChecked == true;
    txtSupportPhone.IsEnabled = oemEnabled && chkUseSupportPhone.IsChecked == true;
  }

  private void btnBrowseLogo_Click(object? sender,RoutedEventArgs e) {
    OpenFileDialog dialog = new() {
      Filter = "Images (*.bmp;*.png;*.jpg;*.jpeg)|*.bmp;*.png;*.jpg;*.jpeg|All files (*.*)|*.*",
      Title = "Select OEM logo"
    };

    if (!string.IsNullOrWhiteSpace(txtLogoPath.Text)) {
      string? dir = Path.GetDirectoryName(txtLogoPath.Text);

      if (!string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir))
        dialog.InitialDirectory = dir;
    }

    if (dialog.ShowDialog(this) == true)
      txtLogoPath.Text = dialog.FileName;
  }

  private void SaveFromUi() {
    Oem.EnableOemInformation = chkEnableOemInformation.IsChecked == true;

    Oem.Manufacturer = txtManufacturer.Text;
    Oem.Model = txtModel.Text;
    Oem.SupportPhone = txtSupportPhone.Text;
    Oem.SupportUrl = txtSupportUrl.Text;
    Oem.SupportHours = txtSupportHours.Text;
    Oem.SupportAppUrl = txtSupportAppUrl.Text;
    Oem.SupportProvider = txtSupportProvider.Text;

    Oem.EnableLogo = chkEnableLogo.IsChecked == true;
    Oem.LogoPath = txtLogoPath.Text;

    Oem.EnableWallpaper = chkEnableWallpaper.IsChecked == true;
    Oem.WallpaperPath = txtWallpaperPath.Text;

    Oem.HideEulaPage = chkHideEulaPage.IsChecked == true;
    Oem.ProtectYourPc = (int)numProtectYourPc.Value;
    Oem.HideOnlineAccountScreens = chkHideOnlineAccountScreens.IsChecked == true;
    Oem.HideOEMRegistrationScreen = chkHideOEMRegistrationScreen.IsChecked == true;
    Oem.HideWirelessSetupInOobe = chkHideWirelessSetupInOobe.IsChecked == true;

    Oem.UseManufacturer = chkUseManufacturer.IsChecked == true;
    Oem.UseModel = chkUseModel.IsChecked == true;
    Oem.UseSupportAppUrl = chkUseSupportAppUrl.IsChecked == true;
    Oem.UseSupportUrl = chkUseSupportUrl.IsChecked == true;
    Oem.UseSupportHours = chkUseSupportHours.IsChecked == true;
    Oem.UseSupportProvider = chkUseSupportProvider.IsChecked == true;
    Oem.UseSupportPhone = chkUseSupportPhone.IsChecked == true;
  }

  private void btnBrowseWallpaper_Click(object? sender,RoutedEventArgs e) {
    OpenFileDialog dialog = new() {
      Filter = "Images (*.bmp;*.png;*.jpg;*.jpeg)|*.bmp;*.png;*.jpg;*.jpeg|All files (*.*)|*.*",
      Title = "Select wallpaper"
    };

    if (!string.IsNullOrWhiteSpace(txtWallpaperPath.Text)) {
      string? dir = Path.GetDirectoryName(txtWallpaperPath.Text);

      if (!string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir))
        dialog.InitialDirectory = dir;
    }

    if (dialog.ShowDialog(this) == true)
      txtWallpaperPath.Text = dialog.FileName;
  }

  private void btnOk_Click(object? sender,RoutedEventArgs e) {
    SaveFromUi();

    DialogResult = true;

  }

  private void btnCancel_Click(object? sender,RoutedEventArgs e) {
    DialogResult = false;
  }
}