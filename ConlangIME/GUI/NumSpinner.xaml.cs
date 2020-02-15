using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Mijyuoon.MVVM;

namespace ConlangIME.GUI {
    public partial class NumSpinner : UserControl {
        #region Configuration Properties

        public int Value {
            get { return (int)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public int MinValue {
            get { return (int)GetValue(MinValueProperty); }
            set { SetValue(MinValueProperty, value); }
        }

        public int MaxValue {
            get { return (int)GetValue(MaxValueProperty); }
            set { SetValue(MaxValueProperty, value); }
        }

        public int Step {
            get { return (int)GetValue(StepProperty); }
            set { SetValue(StepProperty, value); }
        }

        public string Suffix {
            get { return (string)GetValue(SuffixProperty); }
            set { SetValue(SuffixProperty, value); }
        }

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(int), typeof(NumSpinner),
                new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, Value_Changed, Value_Coerce));

        public static readonly DependencyProperty MinValueProperty =
            DependencyProperty.Register("MinValue", typeof(int), typeof(NumSpinner),
                new PropertyMetadata(0, MinValue_Changed));

        public static readonly DependencyProperty MaxValueProperty =
            DependencyProperty.Register("MaxValue", typeof(int), typeof(NumSpinner),
                new PropertyMetadata(10, MaxValue_Changed));

        public static readonly DependencyProperty StepProperty =
            DependencyProperty.Register("Step", typeof(int), typeof(NumSpinner),
                new PropertyMetadata(1));

        public static readonly DependencyProperty SuffixProperty =
            DependencyProperty.Register("Suffix", typeof(string), typeof(NumSpinner),
                new PropertyMetadata("", Suffix_Changed));

        #endregion

        public NumSpinner() {
            InitializeComponent();
            Value = MinValue;
        }

        #region Property Events

        private static object Value_Coerce(DependencyObject d, object value) {
            var sender = d as NumSpinner;
            return Math.Min(Math.Max((int)value, sender.MinValue), sender.MaxValue);
        }

        private static void Value_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var sender = d as NumSpinner;
            sender.txField.Text = $"{e.NewValue}{sender.Suffix}";
        }

        private static void MinValue_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var sender = d as NumSpinner;
            sender.Value = Math.Max(sender.Value, (int)e.NewValue);
        }

        private static void MaxValue_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var sender = d as NumSpinner;
            sender.Value = Math.Min(sender.Value, (int)e.NewValue);
        }

        private static void Suffix_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var sender = d as NumSpinner;
            sender.txField.Text = $"{sender.Value}{e.NewValue}";
        }

        #endregion

        #region UI Events

        private void Field_TextChanged(object sender, TextChangedEventArgs e) {
            if(!txField.IsKeyboardFocusWithin) return;

            // TODO text entry & parsing
        }

        private void Minus_Click(object sender, RoutedEventArgs e) {
            Value -= Step;
        }

        private void Plus_Click(object sender, RoutedEventArgs e) {
            Value += Step;
        }

        #endregion
    }
}
