using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
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

namespace ConlangIME {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {

        #region ViewModel

        public class ViewModel : BaseModel {
            private List<ILanguage> _Languages;
            public List<ILanguage> Languages {
                get => _Languages;
                set => SetProperty(ref _Languages, value);
            }

            private ILanguage _CurrentLanguage;
            public ILanguage CurrentLanguage {
                get => _CurrentLanguage;
                set {
                    SetProperty(ref _CurrentLanguage, value);
                    OnPropertyChanged(nameof(ScaledFontSize));
                }
            }

            private List<IInputMethod> _InputMethods;
            public List<IInputMethod> InputMethods {
                get => _InputMethods;
                set => SetProperty(ref _InputMethods, value);
            }

            private IInputMethod _CurrentInputMethod;
            public IInputMethod CurrentInputMethod {
                get => _CurrentInputMethod;
                set => SetProperty(ref _CurrentInputMethod, value);
            }

            private int _FontScale;
            public int FontScale {
                get => _FontScale;
                set {
                    SetProperty(ref _FontScale, value);
                    OnPropertyChanged(nameof(ScaledFontSize));
                }
            }

            public double ScaledFontSize =>
                (CurrentLanguage?.FontSize ?? 0) * FontScale * 0.01;

            private string _InputText;
            public string InputText {
                get => _InputText;
                set => SetProperty(ref _InputText, value);
            }

            private string _OutputText;
            public string OutputText {
                get => _OutputText;
                set => SetProperty(ref _OutputText, value);
            }
        }

        #endregion

        private List<ILanguage> Languages;
        private Dictionary<Type, List<IInputMethod>> InputMethods;

        public ViewModel VM { get; }

        public MainWindow() {
            SetupSupportedLanguages();

            VM = new ViewModel();
            VM.PropertyChanged += VM_PropertyChanged;
            VM.Languages = Languages;
            VM.FontScale = 100;

            InitializeComponent();
            DataContext = VM;
        }

        private void VM_PropertyChanged(object sender, PropertyChangedEventArgs e) {
            switch(e.PropertyName) {
            case nameof(ViewModel.Languages):
                VM.CurrentLanguage = VM.Languages?.FirstOrDefault();
                break;

            case nameof(ViewModel.InputMethods):
                VM.CurrentInputMethod = VM.InputMethods?.FirstOrDefault();
                break;

            case nameof(ViewModel.CurrentLanguage):
                VM.InputMethods = InputMethods.GetOrDefault(VM.CurrentLanguage?.GetType());
                break;

            case nameof(ViewModel.InputText):
                if(VM.CurrentLanguage != null && VM.CurrentInputMethod != null) {
                    var tok = VM.CurrentInputMethod.Tokenize(VM.InputText);
                    VM.OutputText = VM.CurrentLanguage.Process(tok);
                } else {
                    VM.OutputText = "";
                }
                break;
            }
        }

        private void SetupSupportedLanguages() {
            Languages = new List<ILanguage>();
            InputMethods = new Dictionary<Type, List<IInputMethod>>();

            var alltypes = Assembly.GetExecutingAssembly().GetExportedTypes();

            foreach(var type in alltypes) {
                if(!typeof(ILanguage).IsAssignableFrom(type)) continue;

                var attrib = type.GetCustomAttribute<LanguageAttribute>();
                if(attrib == null) continue;

                var instance = Activator.CreateInstance(type) as ILanguage;
                InputMethods.Add(type, new List<IInputMethod>());
                Languages.Add(instance);
            }

            foreach(var type in alltypes) {
                if(!typeof(IInputMethod).IsAssignableFrom(type)) continue;

                var attrib = type.GetCustomAttribute<InputMethodAttribute>();
                if(attrib == null) continue;

                if(InputMethods.TryGetValue(attrib.Language, out var value)) {
                    var instance = Activator.CreateInstance(type) as IInputMethod;
                    value.Add(instance);
                }
            }
        }
    }
}
