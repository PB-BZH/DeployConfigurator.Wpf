using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace DeployConfigurator.Wpf.UI.Controls;

public partial class PreviewEditor: UserControl {

  private ScrollViewer? _editorScrollViewer;
  public event EventHandler? OpenInNotepadPlusPlusRequested;


  public PreviewEditor() {
    InitializeComponent();
  }


  // ============================================================
  // ACCÈS AU RICHTEXTBOX
  // ============================================================

  public RichTextBox Editor => rtbEditor;


  // ============================================================
  // CHARGEMENT
  // ============================================================

  private void rtbEditor_Loaded(
    object sender,
    RoutedEventArgs e) {

    _editorScrollViewer =
      FindVisualChild<ScrollViewer>(rtbEditor);

    if (_editorScrollViewer is not null) {

      _editorScrollViewer.ScrollChanged +=
        EditorScrollViewer_ScrollChanged;
    }

    UpdateLineNumbers();
  }

  public event MouseButtonEventHandler?
  EditorMouseDoubleClick;


  private void rtbEditor_MouseDoubleClick(
    object sender,
    MouseButtonEventArgs e) {

    EditorMouseDoubleClick?.Invoke(
      rtbEditor,
      e);
  }

  // ============================================================
  // MODIFICATION DU TEXTE
  // ============================================================

  private void rtbEditor_TextChanged(
    object sender,
    TextChangedEventArgs e) {

    UpdateLineNumbers();
  }


  // ============================================================
  // NUMÉROS DE LIGNES
  // ============================================================

  private void UpdateLineNumbers() {

    string text =
      new TextRange(
        rtbEditor.Document.ContentStart,
        rtbEditor.Document.ContentEnd)
      .Text;

    text =
      text.TrimEnd(
        '\r',
        '\n');

    if (string.IsNullOrEmpty(text)) {

      txtLineNumbers.Text =
        string.Empty;

      return;
    }


    int lineCount =
      1;

    foreach (char character in text) {

      if (character == '\n') {
        lineCount++;
      }
    }


    StringBuilder builder =
      new();

    for (
      int line = 1;
      line <= lineCount;
      line++) {

      builder.Append(line);

      if (line < lineCount) {
        builder.AppendLine();
      }
    }

    txtLineNumbers.Text =
      builder.ToString();
  }


  // ============================================================
  // SYNCHRONISATION DU SCROLL
  // ============================================================

  private void EditorScrollViewer_ScrollChanged(
    object sender,
    ScrollChangedEventArgs e) {

    lineNumbersScrollViewer.ScrollToVerticalOffset(
      e.VerticalOffset);
  }


  // ============================================================
  // RECHERCHE D'UN CONTRÔLE DANS L'ARBRE VISUEL
  // ============================================================

  private static T? FindVisualChild<T>(
    DependencyObject parent)
    where T : DependencyObject {

    int childCount =
      VisualTreeHelper.GetChildrenCount(
        parent);

    for (
      int i = 0;
      i < childCount;
      i++) {

      DependencyObject child =
        VisualTreeHelper.GetChild(
          parent,
          i);

      if (child is T result) {
        return result;
      }

      T? descendant =
        FindVisualChild<T>(
          child);

      if (descendant is not null) {
        return descendant;
      }
    }

    return null;
  }

  //======================================================
  //CONTEXT mnu
  //======================================================

  private void Copy_Click(
  object sender,
  RoutedEventArgs e) {

    rtbEditor.Copy();
  }


  private void SelectAll_Click(
    object sender,
    RoutedEventArgs e) {

    rtbEditor.SelectAll();
  }


  private void OpenNotepadPlusPlus_Click(
    object sender,
    RoutedEventArgs e) {

    OpenInNotepadPlusPlusRequested?.Invoke(
      this,
      RoutedEventArgs.Empty);
  }
}