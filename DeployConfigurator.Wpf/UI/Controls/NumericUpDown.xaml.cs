using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DeployConfigurator.Wpf.UI.Controls;

public partial class NumericUpDown: UserControl {
  public event RoutedPropertyChangedEventHandler<int>? ValueChanged;

  // =====================================================================
  // VALUE
  // =====================================================================

  public static readonly DependencyProperty ValueProperty =
    DependencyProperty.Register(
      nameof(Value),
      typeof(int),
      typeof(NumericUpDown),
      new FrameworkPropertyMetadata(
        0,
        FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
        OnValueChanged,
        CoerceValue));

  public int Value {
    get => (int)GetValue(ValueProperty);
    set => SetValue(ValueProperty,value);
  }


  // =====================================================================
  // MINIMUM
  // =====================================================================

  public static readonly DependencyProperty MinimumProperty =
    DependencyProperty.Register(
      nameof(Minimum),
      typeof(int),
      typeof(NumericUpDown),
      new PropertyMetadata(
        0,
        OnLimitChanged));

  public int Minimum {
    get => (int)GetValue(MinimumProperty);
    set => SetValue(MinimumProperty,value);
  }


  // =====================================================================
  // MAXIMUM
  // =====================================================================

  public static readonly DependencyProperty MaximumProperty =
    DependencyProperty.Register(
      nameof(Maximum),
      typeof(int),
      typeof(NumericUpDown),
      new PropertyMetadata(
        int.MaxValue,
        OnLimitChanged));

  public int Maximum {
    get => (int)GetValue(MaximumProperty);
    set => SetValue(MaximumProperty,value);
  }


  // =====================================================================
  // INCREMENT
  // =====================================================================

  public static readonly DependencyProperty IncrementProperty =
    DependencyProperty.Register(
      nameof(Increment),
      typeof(int),
      typeof(NumericUpDown),
      new PropertyMetadata(1));

  public int Increment {
    get => (int)GetValue(IncrementProperty);
    set => SetValue(
      IncrementProperty,
      value <= 0 ? 1 : value);
  }


  // =====================================================================
  // CONSTRUCTEUR
  // =====================================================================

  public NumericUpDown() {

    InitializeComponent();

    UpdateText();
  }


  // =====================================================================
  // VALEUR MODIFIÉE
  // =====================================================================

  private static void OnValueChanged(
    DependencyObject d,
    DependencyPropertyChangedEventArgs e) {

    if (d is not NumericUpDown control)
      return;

    control.UpdateText();

    control.ValueChanged?.Invoke(
      control,
      new RoutedPropertyChangedEventArgs<int>(
        (int)e.OldValue,
        (int)e.NewValue));
  }

  // =====================================================================
  // MINIMUM / MAXIMUM MODIFIÉ
  // =====================================================================

  private static void OnLimitChanged(
    DependencyObject d,
    DependencyPropertyChangedEventArgs e) {

    d.CoerceValue(
      ValueProperty);

    if (d is NumericUpDown control) {
      control.UpdateText();
    }
  }


  // =====================================================================
  // GARANTIT QUE VALUE RESTE DANS LES LIMITES
  // =====================================================================

  private static object CoerceValue(
    DependencyObject d,
    object baseValue) {

    NumericUpDown control =
      (NumericUpDown)d;

    int value =
      (int)baseValue;

    if (value < control.Minimum) {
      return control.Minimum;
    }

    if (value > control.Maximum) {
      return control.Maximum;
    }

    return value;
  }


  // =====================================================================
  // BOUTON +
  // =====================================================================

  private void btnUp_Click(
    object sender,
    RoutedEventArgs e) {

    long newValue =
      (long)Value
      + Increment;

    Value =
      (int)Math.Min(
        newValue,
        Maximum);
  }


  // =====================================================================
  // BOUTON -
  // =====================================================================

  private void btnDown_Click(
    object sender,
    RoutedEventArgs e) {

    long newValue =
      (long)Value
      - Increment;

    Value =
      (int)Math.Max(
        newValue,
        Minimum);
  }


  // =====================================================================
  // SAISIE MANUELLE
  // =====================================================================

  private void txtValue_LostKeyboardFocus(
    object sender,
    KeyboardFocusChangedEventArgs e) {

    ApplyTextValue();
  }


  private void txtValue_KeyDown(
    object sender,
    KeyEventArgs e) {

    if (e.Key == Key.Enter) {

      ApplyTextValue();

      Keyboard.ClearFocus();
    }
  }


  private void ApplyTextValue() {

    if (
      int.TryParse(
        txtValue.Text,
        out int value)) {

      Value = value;
    }

    UpdateText();
  }


  // =====================================================================
  // AFFICHAGE
  // =====================================================================

  private void UpdateText() {

    if (txtValue is not null) {
      txtValue.Text =
        Value.ToString();
    }
  }
}