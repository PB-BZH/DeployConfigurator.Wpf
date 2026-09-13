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
using System.Windows.Media;
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

    colWinPeType.ItemsSource = Enum.GetValues(typeof(SynchronousCommandType));
    colFirstLogonType.ItemsSource = Enum.GetValues(typeof(SynchronousCommandType));

    cmbEditType.ItemsSource = Enum.GetValues(typeof(SynchronousCommandType));

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

  private SynchronousCommandConfiguration? CurrentCommand {
    get {
      if (CurrentGrid.CurrentRow == null)
        return null;

      int index = CurrentGrid.CurrentRow.Index;

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
    grid.AutoGenerateColumns = false;
    grid.AllowUserToAddRows = false;
    grid.AllowUserToDeleteRows = false;
    grid.SelectionMode = DataGridSelectionMode.FullRowSelect;
    grid.MultiSelect = false;
    grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
    grid.RowHeadersVisible = false;

    grid.BackgroundColor = new SolidColorBrush(Color.FromRgb(45,45,45));
    grid.BorderStyle = BorderStyle.None;
    grid.GridColor = new SolidColorBrush(Color.FromRgb(80,80,80));

    grid.EnableHeadersVisualStyles = false;
    grid.ColumnHeadersDefaultCellStyle.BackColor = new SolidColorBrush(Color.FromRgb(35,35,35));
    grid.ColumnHeadersDefaultCellStyle.Foreground = Brushes.White;
    grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = new SolidColorBrush(Color.FromRgb(35,35,35));
    grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = Brushes.White;

    grid.DefaultCellStyle.BackColor = new SolidColorBrush(Color.FromRgb(45,45,45));
    grid.DefaultCellStyle.Foreground = Brushes.White;
    grid.DefaultCellStyle.SelectionBackColor = new SolidColorBrush(Color.FromRgb(0,120,215));
    grid.DefaultCellStyle.SelectionForeColor = Brushes.White;

    grid.Columns.Clear();

    grid.Columns.Add(new DataGridCheckBoxColumn {
      Binding = new Binding("Enabled"),
      Header = "Enabled",
      Width = 70,
    });

    grid.Columns.Add(new DataGridComboBoxColumn {
      Header = "Type",
      ItemsSource = Enum.GetValues<SynchronousCommandType>(),
      SelectedItemBinding = new Binding("Type") {
        Mode = BindingMode.TwoWay,
        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
      },
      Width = 110
    });

    grid.Columns.Add(new DataGridTextColumn {
      Binding = new Binding("Order"),
      Header = "Order",
      Width = 60,
    });

    grid.Columns.Add(new DataGridTextColumn {
      Binding = new Binding("Description"),
      Header = "Description",
    });

    grid.Columns.Add(new DataGridTextColumn {
      Binding = new Binding("CommandLine"),
      Header = "Command line",
    });
  }

  private void LoadGrids() {
    gridWindowsPeCommands.ItemsSource = null;
    gridWindowsPeCommands.ItemsSource = WindowsPeCommands;

    gridFirstLogonCommands.ItemsSource = null;
    gridFirstLogonCommands.ItemsSource = FirstLogonCommands;
  }

  private DataGrid CurrentGrid =>
      tabCommands.SelectedItem == tabWindowsPeCommands
          ? gridWindowsPeCommands
          : gridFirstLogonCommands;

  private List<SynchronousCommandConfiguration> CurrentList =>
      tabCommands.SelectedTab == tabWindowsPeCommands
          ? WindowsPeCommands
          : FirstLogonCommands;

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
    if (CurrentGrid.CurrentRow == null)
      return;

    int index = CurrentGrid.CurrentRow.Index;

    if (index < 0 || index >= CurrentList.Count)
      return;

    CurrentList.RemoveAt(index);
    Renumber(CurrentList);

    LoadGrids();

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