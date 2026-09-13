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
║  Nom de fichier : PreviewDocumentBuilder.cs												     
╚════════════════════════════════════════════════════════════════════════════════╝
*/
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DeployConfigurator.Wpf.Core.Enums;
using DeployConfigurator.Wpf.Core.Models;

namespace DeployConfigurator.Wpf.Core.Builders;

public class PreviewDocumentBuilder {
  public static string Generate(PreviewDocumentType documentType,DeploymentProfile profile,string templatesRoot) {
    JsonSerializerOptions profileJsonSerializerOptions = new() {
      WriteIndented = true,
      Converters = { new JsonStringEnumConverter() }
    };
    JsonSerializerOptions serializerOptions = profileJsonSerializerOptions;
    return documentType switch {
      PreviewDocumentType.Diskprep =>
          GenerateFromTemplate(
              Path.Combine(templatesRoot,"diskprep.template.cmd"),
              template => DiskprepBuilder.Generate(template,profile)
          ),

      PreviewDocumentType.AutoUnattend =>
          GenerateAutoUnattend(templatesRoot,profile),

      PreviewDocumentType.Unattend =>
          GenerateUnattend(templatesRoot,profile),

      PreviewDocumentType.SetupComplete =>
          GenerateFromTemplate(
              Path.Combine(templatesRoot,"SetupComplete.template.cmd"),
              template => template
          ),

      PreviewDocumentType.Orchestrator =>
          GenerateOrchestrator(templatesRoot,profile),

      PreviewDocumentType.ProfileJson =>
          JsonSerializer.Serialize(
              profile,
              serializerOptions
          ),

      _ => ""
    };
  }

  private static string FirstSytemLocale(string value) {
    if (string.IsNullOrWhiteSpace(value))
      return "fr-FR";

    return value
        .Split(';',StringSplitOptions.RemoveEmptyEntries)
        .Select(x => x.Trim())
        .FirstOrDefault() ?? "fr-FR";
  }

  private static string FirstInputLocale(string value) {
    if (string.IsNullOrWhiteSpace(value))
      return "040c:0000040c";

    return value
        .Split(';',StringSplitOptions.RemoveEmptyEntries)
        .Select(x => x.Trim())
        .FirstOrDefault() ?? "040c:0000040c";
  }

  private static string GenerateAutoUnattend(
      string templatesRoot,
      DeploymentProfile profile) {
    return GenerateFromTemplate(
        Path.Combine(templatesRoot,"autounattend.template.xml"),
        template => {
          string result = template;

          result = result.Replace("__PRODUCT_KEY__",profile.Unattend.ProductKey);
          result = result.Replace("__OWNER__",profile.Unattend.Owner);
          result = result.Replace("__ORGANIZATION__",profile.Unattend.Organization);

          result = result.Replace("__INPUT_LOCALE_AUTOUNATTEND__",profile.Unattend.InputLocale);
          result = result.Replace("__SYSTEM_LOCALE_AUTOUNATTEND__",profile.Unattend.SystemLocale);

          result = result.Replace("__USER_LOCALE__",profile.Unattend.UserLocale);
          result = result.Replace("__UI_LANGUAGE__",profile.Unattend.UILanguage);
          result = result.Replace("__UI_LANGUAGE_FALLBACK__",profile.Unattend.UILanguageFallback);

          result = result.Replace("__WINDOWS_PE_SYNCHRONOUS_COMMANDS__",SynchronousCommandsBuilder.GenerateRunSynchronousCommands(profile.WindowsPeCommands)
);
          return result;
        }
    );
  }

  private static string GenerateUnattend(
      string templatesRoot,
      DeploymentProfile profile) {
    return GenerateFromTemplate(
        Path.Combine(templatesRoot,"unattend.template.xml"),
        template => {
          string result = template;

          string encodedPassword = EncodeUnattendPassword(profile.Unattend.LocalUserPassword,"Password");

          result = result.Replace("__CREATE_LOCAL_ACCOUNT__",profile.Unattend.CreateLocalAccount ? "true" : "false");
          result = result.Replace("__LOCAL_USER_NAME__",profile.Unattend.LocalUserName);
          result = result.Replace("__LOCAL_USER_GROUP__",profile.Unattend.LocalUserGroup);
          result = result.Replace("__LOCAL_USER_PASSWORD__",encodedPassword);

          result = result.Replace("__ENABLE_AUTO_LOGON__",profile.Unattend.EnableAutoLogon ? "true" : "false");
          result = result.Replace("__AUTO_LOGON_COUNT__",profile.Unattend.AutoLogonCount.ToString());

          result = result.Replace("__INPUT_LOCALE_UNATTEND__",FirstInputLocale(profile.Unattend.InputLocale));
          result = result.Replace("__SYSTEM_LOCALE_UNATTEND__",FirstSytemLocale(profile.Unattend.SystemLocale));

          result = result.Replace("__USER_LOCALE__",profile.Unattend.UserLocale);
          result = result.Replace("__UI_LANGUAGE__",profile.Unattend.UILanguage);
          result = result.Replace("__UI_LANGUAGE_FALLBACK__",profile.Unattend.UILanguageFallback);

          result = result.Replace("__TIME_ZONE__",profile.Unattend.TimeZone);
          result = result.Replace("__ORGANIZATION__",profile.Unattend.Organization);
          result = result.Replace("__OWNER__",profile.Unattend.Owner);
          result = result.Replace("__COMPUTER_NAME__",profile.Unattend.ComputerName);
          result = result.Replace("__FIRST_LOGON_COMMANDS__",SynchronousCommandsBuilder.GenerateFirstLogonCommands(profile.FirstLogonCommands));

          result = result.Replace("__HIDE_EULA_PAGE__",BoolToXml(profile.Oem.HideEulaPage));
          result = result.Replace("__PROTECT_YOUR_PC__",profile.Oem.ProtectYourPc.ToString());
          result = result.Replace("__HIDE_ONLINE_ACCOUNT_SCREENS__",BoolToXml(profile.Oem.HideOnlineAccountScreens));
          result = result.Replace("__HIDE_OEM_REGISTRATION_SCREEN__",BoolToXml(profile.Oem.HideOEMRegistrationScreen));
          result = result.Replace("__HIDE_WIRELESS_SETUP_IN_OOBE__",BoolToXml(profile.Oem.HideWirelessSetupInOobe));

          result = result.Replace("__OEM_MANUFACTURER__",profile.Oem.UseManufacturer ? profile.Oem.Manufacturer : "");
          result = result.Replace("__OEM_MODEL__",profile.Oem.UseModel ? profile.Oem.Model : "");
          result = result.Replace("__OEM_SUPPORT_APP_URL__",profile.Oem.UseSupportAppUrl ? profile.Oem.SupportAppUrl : "");
          result = result.Replace("__OEM_SUPPORT_URL__",profile.Oem.UseSupportUrl ? profile.Oem.SupportUrl : "");
          result = result.Replace("__OEM_SUPPORT_HOURS__",profile.Oem.UseSupportHours ? profile.Oem.SupportHours : "");
          result = result.Replace("__OEM_SUPPORT_PROVIDER__",profile.Oem.UseSupportProvider ? profile.Oem.SupportProvider : "");
          result = result.Replace("__OEM_SUPPORT_PHONE__",profile.Oem.UseSupportPhone ? profile.Oem.SupportPhone : "");
          result = result.Replace("__OEM_LOGO__",profile.Oem.UseSupportPhone ? profile.Oem.SupportPhone : "");
          return result;
        }
    );
  }

  private static string BoolToXml(bool value) {
    return value ? "true" : "false";
  }

  private static string GenerateOrchestrator(
      string templatesRoot,
      DeploymentProfile profile) {
    return GenerateFromTemplate(
        Path.Combine(templatesRoot,"orchestrator_resume.template.cmd"),
        template => {
          string result = template;

          result = result.Replace("__RUN_DETACH_USB__",BoolToBatch(profile.Orchestration.RunDetachUsb));
          result = result.Replace("__RUN_REORG_VOLUMES__",BoolToBatch(profile.Orchestration.RunReorgVolumes));
          result = result.Replace("__RUN_WIFI__",BoolToBatch(profile.Orchestration.RunWifi));
          result = result.Replace("__RUN_DRIVERS__",BoolToBatch(profile.Orchestration.RunDrivers));
          result = result.Replace("__RUN_WINDOWS_UPDATE_DRIVERS__",BoolToBatch(profile.Orchestration.RunWindowsUpdateDrivers));
          result = result.Replace("__RUN_SOFTWARES__",BoolToBatch(profile.Orchestration.RunSoftwares));
          result = result.Replace("__RUN_M365__",BoolToBatch(profile.Orchestration.RunMicrosoft365));
          result = result.Replace("__RUN_POSTINSTALL__",BoolToBatch(profile.Orchestration.RunPostInstall));
          result = result.Replace("__RUN_CLEANUP__",BoolToBatch(profile.Orchestration.RunCleanup));

          return result;
        }
    );
  }

  private static string GenerateFromTemplate(
      string templatePath,
      Func<string,string> generator) {
    if (!File.Exists(templatePath))
      throw new FileNotFoundException("Template introuvable",templatePath);

    string template = File.ReadAllText(templatePath);
    return generator(template);
  }

  //private static string EncodeUnattendPassword(string password) {
  //  byte[] bytes = Encoding.Unicode.GetBytes(password);
  //  return Convert.ToBase64String(bytes);
  //}

  private static string EncodeUnattendPassword(string password,string suffix = "Password") {
    byte[] bytes = Encoding.Unicode.GetBytes(
        password + suffix
    );

    return Convert.ToBase64String(bytes);
  }

  private static string BoolToBatch(bool value) {
    return value ? "1" : "0";
  }
}