using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;

namespace ConlangIME.Views.Components;

public partial class NumSpinner : UserControl
{
    public static readonly StyledProperty<decimal?> ValueProperty =
        NumericUpDown.ValueProperty.AddOwner<NumSpinner>(new StyledPropertyMetadata<decimal?>(
            defaultValue: 0m,
            defaultBindingMode: BindingMode.TwoWay,
            coerce: CoerceValue));

    public static readonly StyledProperty<decimal> MinimumProperty =
        NumericUpDown.MinimumProperty.AddOwner<NumSpinner>(new StyledPropertyMetadata<decimal>(
            defaultValue: Decimal.MinValue,
            coerce: CoerceMinimum));

    public static readonly StyledProperty<decimal> MaximumProperty =
        NumericUpDown.MaximumProperty.AddOwner<NumSpinner>(new StyledPropertyMetadata<decimal>(
            defaultValue: Decimal.MaxValue,
            coerce: CoerceMaximum));

    public static readonly StyledProperty<decimal> IncrementProperty =
        NumericUpDown.IncrementProperty.AddOwner<NumSpinner>(new StyledPropertyMetadata<decimal>(
            defaultValue: 1m,
            coerce: CoerceIncrement));

    public static readonly StyledProperty<string> SuffixProperty =
        AvaloniaProperty.Register<NumSpinner, string>(nameof(Suffix), defaultValue: String.Empty);

    public NumSpinner()
    {
        InitializeComponent();
    }

    public decimal Value
    {
        get => GetValue(ValueProperty) ?? default;
        set => SetValue(ValueProperty, value);
    }

    public decimal Minimum
    {
        get => GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public decimal Maximum
    {
        get => GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public decimal Increment
    {
        get => GetValue(IncrementProperty);
        set => SetValue(IncrementProperty, value);
    }

    public string Suffix
    {
        get => GetValue(SuffixProperty);
        set => SetValue(SuffixProperty, value);
    }

    private void UpdateValueDisplay()
    {
        TextValue.Text = $"{Value}{Suffix}";
    }

    private void ButtonMinus_OnClick(object? sender, RoutedEventArgs e)
    {
        Value -= Increment;
    }

    private void ButtonPlus_OnClick(object? sender, RoutedEventArgs e)
    {
        Value += Increment;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ValueProperty
            || change.Property == SuffixProperty)
        {
            UpdateValueDisplay();
        }
    }

    private static decimal? CoerceValue(AvaloniaObject target, decimal? value) =>
        value.HasValue && target is NumSpinner self
            ? Math.Clamp(value.Value, self.Minimum, self.Maximum)
            : default;

    private static decimal CoerceMinimum(AvaloniaObject target, decimal minimum) =>
        target is NumSpinner self ? Math.Min(minimum, self.Maximum) : minimum;

    private static decimal CoerceMaximum(AvaloniaObject target, decimal maximum) =>
        target is NumSpinner self ? Math.Max(maximum, self.Minimum) : maximum;

    private static decimal CoerceIncrement(AvaloniaObject target, decimal increment) =>
        target is NumSpinner ? Math.Abs(increment) : increment;
}