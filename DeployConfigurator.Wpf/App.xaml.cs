using System.Text;
using System.Windows;

namespace DeployConfigurator.Wpf;

public partial class App: Application {

  protected override void OnStartup(StartupEventArgs e) {

    // ============================================================
    // ENCODAGES WINDOWS / DOS
    // ============================================================

    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    base.OnStartup(e);
  }
}