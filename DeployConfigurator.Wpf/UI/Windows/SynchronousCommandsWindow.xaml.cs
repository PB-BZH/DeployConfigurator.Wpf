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
║  Le 22/5/2026 - 21:34                                                          ║                                                          
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
║  Nom de fichier : SynchronousCommandsForm.cs												     
╚════════════════════════════════════════════════════════════════════════════════╝
*/
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using DeployConfigurator.Wpf.Core.Models;
using PB.BZH.Help.Wpf.UI.Theming;

namespace DeployConfigurator.Wpf.UI.Windows;

public partial class SynchronousCommandsWindow: Window {
  public List<SynchronousCommandConfiguration> WindowsPeCommands { get; private set; }
  public List<SynchronousCommandConfiguration> FirstLogonCommands { get; private set; }

  public SynchronousCommandsWindow(
      List<SynchronousCommandConfiguration> windowsPeCommands,
      List<SynchronousCommandConfiguration> firstLogonCommands) {
    InitializeComponent();

    ThemeManager.SetTheme(AppTheme.Dark);

    // ============================================================
    // INITIALISATION DES TYPES DE COMMANDES
    // ============================================================

    cmbEditType.ItemsSource = Enum.GetValues<SynchronousCommandType>();

    WindowsPeCommands = CloneCommands(windowsPeCommands);
    FirstLogonCommands = CloneCommands(firstLogonCommands);

    LoadGrids();
    LoadSelectedCommandToEditor();
  }

  public event Action? CommandsChanged;
  private void btnApplyCommandChanges_Click(object? sender,RoutedEventArgs e) {
    SynchronousCommandConfiguration? command = CurrentCommand;
    if (command == null)
      return;
    command.Order = (int)numEditOrder.Value;
    command.Enabled = chkEditEnabled.IsChecked == true;
    command.Type = (SynchronousCommandType)cmbEditType.SelectedItem!;
    command.Description = txtEditDescription.Text;
    command.CommandLine = txtEditCommandLine.Text;
    CurrentList.Sort((a,b) => a.Order.CompareTo(b.Order));
    Renumber(CurrentList);
    LoadGrids();
    SelectRow(CurrentGrid,Math.Max(0,command.Order - 1));
    CommandsChanged?.Invoke();
  }

  private void LoadSelectedCommandToEditor() {
    SynchronousCommandConfiguration? command = CurrentCommand;
    bool hasCommand = command != null;
    pnlEditor.IsEnabled = hasCommand;
    if (!hasCommand) {
      numEditOrder.Value = 1;
      chkEditEnabled.IsChecked = false;
      cmbEditType.SelectedIndex = 0;
      txtEditDescription.Text = "";
      txtEditCommandLine.Text = "";
      return;
    }
    numEditOrder.Value = Math.Max(numEditOrder.Minimum,Math.Min(numEditOrder.Maximum,command!.Order));
    chkEditEnabled.IsChecked = command.Enabled;
    cmbEditType.SelectedItem = command.Type;

    txtEditDescription.Text = command.Description;
    txtEditCommandLine.Text = command.CommandLine;
  }

  private void gridCommands_SelectionChanged(object? sender,RoutedEventArgs e) {
    LoadSelectedCommandToEditor();
  }

  private void tabCommands_SelectedIndexChanged(object? sender,RoutedEventArgs e) {
    LoadSelectedCommandToEditor();
  }

  // ============================================================
  // COMMANDE COURANTE
  // ============================================================

  private SynchronousCommandConfiguration? CurrentCommand {
    get {
      int index = CurrentGrid.SelectedIndex;

      if (index < 0 || index >= CurrentList.Count)
        return null;

      return CurrentList[index];
    }
  }

  private static List<SynchronousCommandConfiguration> CloneCommands(
      IEnumerable<SynchronousCommandConfiguration> source) {
    return source.Select(x => new SynchronousCommandConfiguration {
      Enabled = x.Enabled,
      Type = x.Type,
      Order = x.Order,
      Description = x.Description,
      CommandLine = x.CommandLine
    }).ToList();
  }

  private static void ConfigureGrid(DataGrid grid) {

    // ============================================================
    // COMPORTEMENT DU DATAGRID
    // ============================================================

    grid.AutoGenerateColumns = false;
    grid.CanUserAddRows = false;
    grid.CanUserDeleteRows = false;

    grid.SelectionMode = DataGridSelectionMode.Single;
    grid.SelectionUnit = DataGridSelectionUnit.FullRow;
    grid.HeadersVisibility = DataGridHeadersVisibility.Column;

    // ============================================================
    // COLONNES
    // ============================================================

    grid.Columns.Clear();

    // Enabled
    grid.Columns.Add(new DataGridCheckBoxColumn {
      Header = "Enabled",
      Binding =
          new Binding("Enabled") {
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
          },
      Width = 70
    });

    // Type
    grid.Columns.Add(new DataGridComboBoxColumn {
      Header = "Type",
      ItemsSource = Enum.GetValues<SynchronousCommandType>(),
      SelectedItemBinding =
          new Binding("Type") {
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
          },
      Width = 110
    });

    // Order
    grid.Columns.Add(new DataGridTextColumn {
      Header = "Order",
      Binding = new Binding("Order") {
        Mode = BindingMode.TwoWay
      },
      Width = 60
    });

    // Description
    grid.Columns.Add(new DataGridTextColumn {
      Header = "Description",
      Binding = new Binding("Description") {
        Mode = BindingMode.TwoWay
      },
      Width = new DataGridLength(1,DataGridLengthUnitType.Star)
    });

    // Command line
    grid.Columns.Add(new DataGridTextColumn {
      Header = "Command line",
      Binding = new Binding("CommandLine") {
        Mode = BindingMode.TwoWay
      },

      Width = new DataGridLength(2,DataGridLengthUnitType.Star)
    });
  }

  private void LoadGrids() {
    gridWindowsPeCommands.ItemsSource = null;
    gridWindowsPeCommands.ItemsSource = WindowsPeCommands;

    gridFirstLogonCommands.ItemsSource = null;
    gridFirstLogonCommands.ItemsSource = FirstLogonCommands;
  }

  // ============================================================
  // ONGLET COURANT
  // ============================================================

  private DataGrid CurrentGrid =>
      tabCommands.SelectedIndex == 0
          ? gridWindowsPeCommands
          : gridFirstLogonCommands;

  private List<SynchronousCommandConfiguration> CurrentList =>
      tabCommands.SelectedIndex == 1
          ? WindowsPeCommands
          : FirstLogonCommands;

  // ============================================================
  // AJOUT D'UNE COMMANDE
  // ============================================================

  private void btnAddCommand_Click(object? sender,RoutedEventArgs e) {

    int nextOrder = CurrentList.Count == 0
        ? 1
        : CurrentList.Max(x => x.Order) + 1;

    CurrentList.Add(new SynchronousCommandConfiguration {
      Enabled = true,
      Type = SynchronousCommandType.Custom,
      Order = nextOrder,
      Description = "New command",
      CommandLine = "cmd /c "
    });

    LoadGrids();
    SelectRow(CurrentGrid,CurrentList.Count - 1);

    CommandsChanged?.Invoke();
  }

  private void btnRemoveCommand_Click(object? sender,RoutedEventArgs e) {

    // ============================================================
    // LIGNE SÉLECTIONNÉE
    // ============================================================

    if (CurrentGrid.SelectedItem == null)
      return;
    int index = CurrentGrid.SelectedIndex;
    if (index < 0 || index >= CurrentList.Count)
      return;


    // ============================================================
    // SUPPRESSION
    // ============================================================

    CurrentList.RemoveAt(index);
    Renumber(CurrentList);
    LoadGrids();


    // ============================================================
    // RESTAURATION DE LA SÉLECTION
    // ============================================================

    if (CurrentList.Count > 0)
      SelectRow(CurrentGrid,Math.Min(index,CurrentList.Count - 1));

    CommandsChanged?.Invoke();
  }

  private void btnMoveUp_Click(object? sender,RoutedEventArgs e) {
    MoveSelected(-1);
    CommandsChanged?.Invoke();
  }

  private void btnMoveDown_Click(object? sender,RoutedEventArgs e) {
    MoveSelected(1);
    CommandsChanged?.Invoke();
  }

  private void MoveSelected(int direction) {

    int index = CurrentGrid.SelectedIndex;
    if (index < 0)
      return;
    int newIndex = index + direction;

    if (newIndex < 0 || newIndex >= CurrentList.Count) {
      return;
    }

    (CurrentList[index],CurrentList[newIndex]) = (CurrentList[newIndex],CurrentList[index]);

    Renumber(CurrentList);

    LoadGrids();

    SelectRow(CurrentGrid,newIndex);
  }

  private void btnOk_Click(object? sender,RoutedEventArgs e) {
    EndGridEdit();

    Renumber(WindowsPeCommands);
    Renumber(FirstLogonCommands);

    WindowsPeCommands = WindowsPeCommands
        .OrderBy(x => x.Order)
        .ToList();

    FirstLogonCommands = FirstLogonCommands
        .OrderBy(x => x.Order)
        .ToList();

    DialogResult = true;
  }

  private void btnCancel_Click(object? sender,RoutedEventArgs e) {
    DialogResult = false;
  }

  private void EndGridEdit() {
    CommitGridEdit(
      gridWindowsPeCommands);

    CommitGridEdit(
      gridFirstLogonCommands);
  }


  private static void CommitGridEdit(
    DataGrid grid) {

    grid.CommitEdit(
      DataGridEditingUnit.Cell,
      true);

    grid.CommitEdit(
      DataGridEditingUnit.Row,
      true);
  }

  private static void Renumber(List<SynchronousCommandConfiguration> commands) {
    int order = 1;

    foreach (SynchronousCommandConfiguration command in commands)
      command.Order = order++;
  }

  private static void SelectRow(DataGrid grid,int index) {
    if (index < 0 || index >= grid.Items.Count) {
      return;
    }
    grid.SelectedIndex = index;
    grid.ScrollIntoView(grid.SelectedItem);
    grid.Focus();
  }
}