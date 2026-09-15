using System.Windows;
using DeployConfigurator.Wpf.Core.Models;
using PB.BZH.Theme.Theming;

namespace DeployConfigurator.Wpf.UI.Windows;

public partial class UsbTargetConfirmWindow: Window {

  public UsbDeviceInfo? SelectedUsbDevice { get; private set; }
  public string SelectedVolumeLabel => txtVolumeName.Text.Trim();
  public string SelectedPartitionType => cmbPartitionType.Text;
  public string SelectedTargetSystem => cmbTargetSystem.Text;
  public string SelectedFileSystem => cmbSystemFiles.Text;
  public string SelectedMediaProfile => cmbMediaProfil.Text;
  public bool SplitWimIfNeeded => chkSplitWimIfNeeded.IsChecked == true;

  public UsbTargetConfirmWindow(List<UsbDeviceInfo> devices) {
    InitializeComponent();
    ThemeManager.ApplyTheme(this);

    // WPF : DataSource -> ItemsSource
    cmbUsbDevices.ItemsSource = devices;
    if (devices.Count > 0)
      cmbUsbDevices.SelectedIndex = 0;
    txtVolumeName.Text = "WIN10-OEM";

    // WPF : SelectedIndexChanged -> SelectionChanged
    cmbUsbDevices.SelectionChanged += (_,_) => UpdateSummary();
    cmbMediaProfil.SelectionChanged += (_,_) => UpdateSummary();
    cmbPartitionType.SelectionChanged += (_,_) => UpdateSummary();
    cmbTargetSystem.SelectionChanged += (_,_) => UpdateSummary();
    cmbSystemFiles.SelectionChanged += (_,_) => UpdateSummary();
    txtVolumeName.TextChanged += (_,_) => UpdateSummary();

    UpdateSummary();
  }


  private void btnOk_Click(object sender,RoutedEventArgs e) {
    SelectedUsbDevice = cmbUsbDevices.SelectedItem as UsbDeviceInfo;
    if (SelectedUsbDevice is null) {
      MessageBox.Show(
        "No USB device selected.",
        "USB",
        MessageBoxButton.OK,
        MessageBoxImage.Warning);
      return;
    }
    if (string.IsNullOrWhiteSpace(txtVolumeName.Text)) {
      MessageBox.Show(
        "The volume name is required.",
        "USB",
        MessageBoxButton.OK,
        MessageBoxImage.Warning);

      return;
    }

    DialogResult = true;
  }

  private void UpdateSummary() {
    txtSummary.Text =
      $"Device: {cmbUsbDevices.Text}" +
      Environment.NewLine +
      $"Profile: {cmbMediaProfil.Text} | " +
      $"Volume: {txtVolumeName.Text}" +
      Environment.NewLine +
      $"Partition: {cmbPartitionType.Text} | " +
      $"Target: {cmbTargetSystem.Text} | " +
      $"FS: {cmbSystemFiles.Text}";
  }

  private void btnCancel_Click(
    object sender,
    RoutedEventArgs e) {

    DialogResult =
      false;
  }
}