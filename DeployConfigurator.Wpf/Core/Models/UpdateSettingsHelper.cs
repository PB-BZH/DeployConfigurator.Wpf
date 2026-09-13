using System.IO;
using System.Text.Json;

namespace DeployConfigurator.Wpf.Core.Models;

internal class UpdateSettingsHelper {
  internal static readonly JsonSerializerOptions JsonIndentedOptions = new() {
    WriteIndented = true
  };

  internal static string GetSafeFileName(string value) {
    string result = value.Trim();

    foreach (char invalidChar in Path.GetInvalidFileNameChars()) {
      result = result.Replace(invalidChar.ToString(),"");
    }

    return result.Replace(" ","");
  }
}
