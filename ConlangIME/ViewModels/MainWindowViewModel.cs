using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;

using Avalonia.Media;

using CommunityToolkit.Mvvm.Input;

using ConlangIME.Core;

namespace ConlangIME.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private IReadOnlyList<ILanguage> _languages = new List<ILanguage>();
    private IReadOnlyList<IInputMethod> _inputMethods = new List<IInputMethod>();

    private ILanguage? _activeLanguage = default;
    private IInputMethod? _activeInputMethod = default;

    private int _fontScale = 100;

    private string _inputText = String.Empty;
    private string _outputText = String.Empty;

    public event EventHandler<ILanguage>? LanguageSelected;
    public event EventHandler<IInputMethod>? InputMethodSelected;

    public event EventHandler<string>? InputChanged;
    public event EventHandler<(string Text, int WrapAtColumn)>? OutputUnicodeCopied;

    public IReadOnlyList<ILanguage> Languages
    {
        get => _languages;
        set => SetProperty(ref _languages, value);
    }

    public IReadOnlyList<IInputMethod> InputMethods
    {
        get => _inputMethods;
        set => SetProperty(ref _inputMethods, value);
    }

    public ILanguage? ActiveLanguage
    {
        get => _activeLanguage;
        set => SetProperty(ref _activeLanguage, value);
    }

    public IInputMethod? ActiveInputMethod
    {
        get => _activeInputMethod;
        set => SetProperty(ref _activeInputMethod, value);
    }

    public int FontScale
    {
        get => _fontScale;
        set
        {
            SetProperty(ref _fontScale, value);
            OnPropertyChanged(nameof(OutputFontSize));
        }
    }

    public string InputText
    {
        get => _inputText;
        set => SetProperty(ref _inputText, value);
    }

    public string OutputText
    {
        get => _outputText;
        set => SetProperty(ref _outputText, value);
    }

    public FontFamily OutputFontFamily =>
        ActiveLanguage?.Font ?? FontFamily.Default;

    public double OutputFontSize =>
        0.01 * FontScale * (ActiveLanguage?.FontSize ?? 0);

    public ICommand CopyOutputUnicodeCommand { get; }

    public MainWindowViewModel()
    {
        CopyOutputUnicodeCommand = new RelayCommand<string>(OnCopyOutputUnicode);
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        switch (e.PropertyName)
        {
            case nameof(Languages):
                OnLanguagesChanged();
                break;

            case nameof(InputMethods):
                OnInputMethodsChanged();
                break;

            case nameof(ActiveLanguage):
                OnPropertyChanged(nameof(OutputFontFamily));
                OnPropertyChanged(nameof(OutputFontSize));
                OnLanguageSelected();
                break;

            case nameof(ActiveInputMethod):
                OnInputMethodSelected();
                break;

            case nameof(FontScale):
                OnPropertyChanged(nameof(OutputFontSize));
                break;

            case nameof(InputText):
                OnInputTextChanged();
                break;
        }
    }

    private void OnLanguagesChanged()
    {
        ActiveLanguage = Languages.FirstOrDefault();
    }

    private void OnInputMethodsChanged()
    {
        ActiveInputMethod = InputMethods.FirstOrDefault();
    }

    private void OnLanguageSelected()
    {
        if (ActiveLanguage is not null)
        {
            LanguageSelected?.Invoke(this, ActiveLanguage);
        }
    }

    private void OnInputMethodSelected()
    {
        if (ActiveInputMethod is not null)
        {
            InputMethodSelected?.Invoke(this, ActiveInputMethod);
        }
    }

    private void OnInputTextChanged()
    {
        InputChanged?.Invoke(this, InputText);
    }

    private void OnCopyOutputUnicode(string? param)
    {
        var wrap = Int32.TryParse(param, out var result) ? result : default;
        OutputUnicodeCopied?.Invoke(this, (Text: OutputText, WrapAtColumn: wrap));
    }
}