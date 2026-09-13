using System.Windows;
using DeployConfigurator.Wpf.Core.Models;
using PB.BZH.Help.Core.Profiles;
using PB.BZH.Help.Core.Services;
using PB.BZH.Help.Wpf.UI.Windows;

namespace DeployConfigurator.Wpf.Core.Services;

public class HelpHelper {

  public static ApplicationInfoProfile ConstruireApplicationInfoProfile(DeploymentProfile profile,string downloadPageUrl) {
    return new ApplicationInfoProfile {
      Product = new ProductInfo {
        ProductName = profile.Product.ProductName,
        Manufacturer = profile.Product.Manufacturer,
        Version = profile.Product.Version,
        Description = profile.Product.Description,
        UpgradeCode = profile.Product.UpgradeCode,
        IconPath = profile.Product.IconPath,
        LogoPath = profile.Product.LogoImage,
        DownloadPageUrl = downloadPageUrl,
        PrivacyPageUrl = profile.Product.PrivacyPageUrl,
        Copyright = profile.Product.Copyright,
        EmailContact = profile.Product.EmailContact,
        ProductId = profile.Product.ProductId
      },
      Update = new UpdateInfo {
        ProductName = profile.UpdateManifest.ProductName,
        Version = profile.UpdateManifest.Version,
        Publisher = profile.UpdateManifest.Publisher,
        DownloadPage = ConstruireDownloadPageUrl(profile),
        PrivacyPage = profile.UpdateManifest.PrivacyPage,
        MsiUrl = profile.UpdateManifest.MsiUrl,
        WebSetupUrl = profile.UpdateManifest.WebSetupUrl,
        UpdateManifestUrl = profile.UpdateManifest.UpdateManifestUrl,
        ReleaseDate = profile.UpdateManifest.ReleaseDate,
        ApplicationId = profile.UpdateManifest.ApplicationId
      }
    };
  }

  private static string ConstruireDownloadPageUrl(
    DeploymentProfile profile) {

    return
      $"{profile.Product.DownloadPageUrl}" +
      $"?category={Uri.EscapeDataString(profile.Product.DownloadCategory)}" +
      $"&product={Uri.EscapeDataString(profile.Product.ProductId)}";
  }

  public static void mnuAbout(Window owner,DeploymentProfile profile) {
    string downloadPageUrl = ConstruireDownloadPageUrl(profile);

    ApplicationInfoProfile aboutProfile = ConstruireApplicationInfoProfile(profile,downloadPageUrl);

    AboutWindow aboutWindow =
      new(aboutProfile) {
        Owner = owner
      };

    aboutWindow.ShowDialog();
  }

  public static async Task mnuCheckForUpdates(
    Window owner,
    DeploymentProfile profile) {

    UpdateChecker.UpdateCheckResult result =
      await UpdateChecker.GetUpdateStatusAsync();

    string downloadPageUrl =
      ConstruireDownloadPageUrl(profile);

    ApplicationInfoProfile checkUpdate =
      ConstruireApplicationInfoProfile(
        profile,
        downloadPageUrl);

    UpdateWindow updateWindow =
      new(result,checkUpdate) {
        Owner = owner
      };

    updateWindow.ShowDialog();
  }
}
