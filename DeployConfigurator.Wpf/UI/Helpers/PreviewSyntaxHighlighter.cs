using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace DeployConfigurator.Wpf.UI.Helpers;

public sealed class PreviewSyntaxHighlighter {

  private bool _isHighlighting;


  // ============================================================
  // CMD
  // ============================================================

  public void ApplyCmd(
    RichTextBox textBox) {

    if (_isHighlighting)
      return;

    _isHighlighting = true;

    try {

      Brush defaultForeground =
        textBox.Foreground;

      ResetForeground(
        textBox,
        defaultForeground);


      // Commentaires
      HighlightRegex(
        textBox,
        @"^[ \t]*::.*$",
        Brushes.LightGreen,
        RegexOptions.Multiline);


      // Mots-clés CMD
      HighlightWord(
        textBox,
        "echo",
        Brushes.CornflowerBlue);

      HighlightWord(
        textBox,
        "set",
        Brushes.CornflowerBlue);

      HighlightWord(
        textBox,
        "if",
        Brushes.CornflowerBlue);

      HighlightWord(
        textBox,
        "else",
        Brushes.CornflowerBlue);

      HighlightWord(
        textBox,
        "goto",
        Brushes.CornflowerBlue);

      HighlightWord(
        textBox,
        "call",
        Brushes.CornflowerBlue);

      HighlightWord(
        textBox,
        "for",
        Brushes.CornflowerBlue);

      HighlightWord(
        textBox,
        "exit",
        Brushes.CornflowerBlue);


      // Commandes système
      HighlightWord(
        textBox,
        "diskpart",
        Brushes.Orange);

      HighlightWord(
        textBox,
        "dism",
        Brushes.Orange);

      HighlightWord(
        textBox,
        "bcdboot",
        Brushes.Orange);

      HighlightWord(
        textBox,
        "xcopy",
        Brushes.Orange);

      HighlightWord(
        textBox,
        "copy",
        Brushes.Orange);

      HighlightWord(
        textBox,
        "attrib",
        Brushes.Orange);


      // Variables
      HighlightRegex(
        textBox,
        @"%[A-Z0-9_]+%",
        Brushes.Orange);


      // Chaînes
      HighlightRegex(
        textBox,
        "\".*?\"",
        Brushes.LightSalmon);


      // Labels
      HighlightRegex(
        textBox,
        @"^:[A-Z0-9_]+",
        Brushes.Gold,
        RegexOptions.Multiline);


      // Erreurs / avertissements
      HighlightWord(
        textBox,
        "ERROR",
        Brushes.Red);

      HighlightWord(
        textBox,
        "FATAL",
        Brushes.Red);

      HighlightWord(
        textBox,
        "FAILED",
        Brushes.Red);

      HighlightWord(
        textBox,
        "WARNING",
        Brushes.Orange);

      HighlightWord(
        textBox,
        "WARN",
        Brushes.Orange);


      // Codes retour
      HighlightRegex(
        textBox,
        @"RC=3010",
        Brushes.Cyan);

      HighlightRegex(
        textBox,
        @"RC=%RC%",
        Brushes.Cyan);

      HighlightRegex(
        textBox,
        @"%ERRORLEVEL%",
        Brushes.Cyan);


      ApplyValidationHighlighting(
        textBox);
    }
    finally {

      _isHighlighting = false;
    }
  }


  // ============================================================
  // XML
  // ============================================================

  public void ApplyXml(
    RichTextBox textBox) {

    if (_isHighlighting)
      return;

    _isHighlighting = true;

    try {

      ResetForeground(
        textBox,
        textBox.Foreground);


      // Balises
      HighlightRegex(
        textBox,
        @"</?[^>]+?>",
        Brushes.DeepSkyBlue);


      // Attributs
      HighlightRegex(
        textBox,
        @"\s[a-zA-Z0-9:-]+=",
        Brushes.Orange);


      // Valeurs
      HighlightRegex(
        textBox,
        "\".*?\"",
        Brushes.LightSalmon);


      // Commentaires
      HighlightRegex(
        textBox,
        @"<!--(.*?)-->",
        Brushes.DarkGray,
        RegexOptions.Singleline);
    }
    finally {

      _isHighlighting = false;
    }
  }


  // ============================================================
  // JSON
  // ============================================================

  public void ApplyJson(
    RichTextBox textBox) {

    if (_isHighlighting)
      return;

    _isHighlighting = true;

    try {

      ResetForeground(
        textBox,
        textBox.Foreground);


      // Clés
      HighlightRegex(
        textBox,
        "\"[^\"]+\"\\s*:",
        Brushes.DeepSkyBlue);


      // Chaînes
      HighlightRegex(
        textBox,
        ":\\s*\".*?\"",
        Brushes.LightSalmon);


      // Nombres
      HighlightRegex(
        textBox,
        @"\b\d+\b",
        Brushes.LightGreen);


      // Booléens / null
      HighlightRegex(
        textBox,
        @"\b(true|false|null)\b",
        Brushes.MediumPurple);
    }
    finally {

      _isHighlighting = false;
    }
  }


  // ============================================================
  // VALIDATION
  // ============================================================

  private static void ApplyValidationHighlighting(
    RichTextBox textBox) {

    HighlightLinesContaining(
      textBox,
      "ERROR",
      Brushes.OrangeRed);

    HighlightLinesContaining(
      textBox,
      "FATAL",
      Brushes.Red);

    HighlightLinesContaining(
      textBox,
      "FAILED",
      Brushes.Red);

    HighlightLinesContaining(
      textBox,
      "WARNING",
      Brushes.Orange);

    HighlightLinesContaining(
      textBox,
      "SUCCESS",
      Brushes.LightGreen);

    HighlightLinesContaining(
      textBox,
      "RC=3010",
      Brushes.Cyan);

    HighlightLinesContaining(
      textBox,
      "%ERRORLEVEL%",
      Brushes.Cyan);
  }


  // ============================================================
  // RESET COULEUR
  // ============================================================

  private static void ResetForeground(
    RichTextBox textBox,
    Brush foreground) {

    TextRange range =
      new(
        textBox.Document.ContentStart,
        textBox.Document.ContentEnd);

    range.ApplyPropertyValue(
      TextElement.ForegroundProperty,
      foreground);
  }


  // ============================================================
  // REGEX
  // ============================================================

  private static void HighlightRegex(
    RichTextBox textBox,
    string pattern,
    Brush color,
    RegexOptions options = RegexOptions.None) {

    string text =
      GetText(textBox);

    foreach (
      Match match
      in Regex.Matches(
        text,
        pattern,
        options)) {

      ApplyColor(
        textBox,
        match.Index,
        match.Length,
        color);
    }
  }


  // ============================================================
  // MOT ENTIER
  // ============================================================

  private static void HighlightWord(
    RichTextBox textBox,
    string word,
    Brush color) {

    HighlightRegex(
      textBox,
      $@"\b{Regex.Escape(word)}\b",
      color,
      RegexOptions.IgnoreCase);
  }


  // ============================================================
  // LIGNE CONTENANT UN TEXTE
  // ============================================================

  private static void HighlightLinesContaining(
    RichTextBox textBox,
    string value,
    Brush color) {

    HighlightRegex(
      textBox,
      $@"^.*{Regex.Escape(value)}.*$",
      color,
      RegexOptions.Multiline |
      RegexOptions.IgnoreCase);
  }


  // ============================================================
  // APPLICATION DE LA COULEUR
  // ============================================================

  private static void ApplyColor(
    RichTextBox textBox,
    int start,
    int length,
    Brush color) {

    TextPointer? startPointer =
      GetTextPointerAtOffset(
        textBox.Document.ContentStart,
        start);

    TextPointer? endPointer =
      GetTextPointerAtOffset(
        textBox.Document.ContentStart,
        start + length);

    if (
      startPointer is null
      ||
      endPointer is null) {

      return;
    }

    TextRange range =
      new(
        startPointer,
        endPointer);

    range.ApplyPropertyValue(
      TextElement.ForegroundProperty,
      color);
  }


  // ============================================================
  // POSITION TEXTE → TEXTPointer WPF
  // ============================================================

  private static TextPointer? GetTextPointerAtOffset(
    TextPointer start,
    int offset) {

    TextPointer? navigator =
      start;

    int currentOffset = 0;

    while (navigator is not null) {

      if (
        navigator.GetPointerContext(
          System.Windows.Documents.LogicalDirection.Forward)
        ==
        TextPointerContext.Text) {

        string textRun =
          navigator.GetTextInRun(
            System.Windows.Documents.LogicalDirection.Forward);

        if (
          currentOffset + textRun.Length
          >=
          offset) {

          return navigator.GetPositionAtOffset(
            offset - currentOffset,
            System.Windows.Documents.LogicalDirection.Forward);
        }

        currentOffset +=
          textRun.Length;
      }

      navigator =
        navigator.GetNextContextPosition(
          System.Windows.Documents.LogicalDirection.Forward);
    }

    return null;
  }


  // ============================================================
  // TEXTE DU RICHTEXTBOX
  // ============================================================

  private static string GetText(
    RichTextBox textBox) {

    return new TextRange(
      textBox.Document.ContentStart,
      textBox.Document.ContentEnd)
      .Text;
  }
}