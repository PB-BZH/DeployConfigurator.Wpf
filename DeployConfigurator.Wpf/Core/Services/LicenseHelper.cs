using System.Windows;
using DeployConfigurator.Wpf.Core.Models;
using PB.BZH.Licensing.Core.Models;
using PB.BZH.Licensing.Core.Services;
using PB.BZH.Licensing.Wpf.UI.Windows;

namespace DeployConfigurator.Wpf.Core.Services;

public static class LicenseHelper {
  public static bool TechnicalLicenseRequired {
    get {
#if TECHNICAL_LICENSE_REQUIRED
      return true;
#else
      return false;
#endif
    }
  }

  public static LicenseOptions ConstruireLicenseOptions(
    DeploymentProfile profile) {

    string productId =
      !string.IsNullOrWhiteSpace(profile.Product.ProductId)
        ? profile.Product.ProductId
        : "LicenseGenerator";

    string applicationFolder =
      !string.IsNullOrWhiteSpace(profile.Product.ProductFolder)
        ? profile.Product.ProductFolder
        : productId;

    return new LicenseOptions {
      ProductId = productId,
      ApplicationFolder = applicationFolder
    };
  }

  public static LicenseService CreerLicenseService(
    DeploymentProfile profile) {

    return new LicenseService(
      ConstruireLicenseOptions(profile));
  }

  public static bool VerifierLicenceAuDemarrage(DeploymentProfile profile,Window? owner = null) {
    if (!TechnicalLicenseRequired) {
      return true;
    }

    LicenseService licenseService =
      CreerLicenseService(profile);

    return VerifierLicenceObligatoire(
      licenseService,
      owner);
  }

  private static bool VerifierLicenceObligatoire(
    LicenseService licenseService,
    Window? owner) {

    LicenseValidationResult licenseResult =
      licenseService.ValidateInstalledLicense();

    if (licenseResult.IsValid) {
      return true;
    }

    LicenseActivationWindow activationView = new(
      licenseService,
      messageErreur: licenseResult.Message,
      activationObligatoire: true);

    if (owner is not null) {
      activationView.Owner = owner;
      activationView.WindowStartupLocation =
        WindowStartupLocation.CenterOwner;
    }
    else {
      activationView.WindowStartupLocation =
        WindowStartupLocation.CenterScreen;
    }

    bool? dialogResult =
      activationView.ShowDialog();

    if (dialogResult != true) {
      return false;
    }

    licenseResult =
      licenseService.ValidateInstalledLicense();

    return licenseResult.IsValid;
  }

  public static LicenseOptions ConstruireDisplayOptions(
    DeploymentProfile profile) {

    return new LicenseOptions {
      LogoImage = profile.Product.LogoImage
    };
  }

  public static void AfficherLicence(
    Window owner,
    LicenseService licenseService,
    DeploymentProfile profile) {

    LicenseValidationResult resultat =
      licenseService.ValidateInstalledLicense();

    if (!resultat.IsValid || resultat.License is null) {
      LicenseActivationWindow activationWindow = new(
        licenseService,
        resultat.Message,
        activationObligatoire: false);

      activationWindow.Owner = owner;
      activationWindow.WindowStartupLocation =
        WindowStartupLocation.CenterOwner;

      activationWindow.ShowDialog();
      return;
    }

    LicenseOptions displayOptions =
      ConstruireDisplayOptions(profile);

    LicenseInfoWindow infoWindow = new(
      resultat.License,
      displayOptions);

    infoWindow.Owner = owner;
    infoWindow.WindowStartupLocation =
      WindowStartupLocation.CenterOwner;

    infoWindow.ShowDialog();
  }

  public static void ImporterLicence(
    Window owner,
    LicenseService licenseService) {

    LicenseActivationWindow activationWindow = new(
      licenseService,
      messageErreur: "Importer une nouvelle licence.",
      activationObligatoire: false);

    activationWindow.Owner = owner;
    activationWindow.WindowStartupLocation =
      WindowStartupLocation.CenterOwner;

    activationWindow.ShowDialog();
  }
}