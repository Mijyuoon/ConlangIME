using System;
using System.Text;

using Avalonia.Controls;

using ConlangIME.Core;
using ConlangIME.ViewModels;

namespace ConlangIME.Views;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel = new();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _viewModel;

        _viewModel.LanguageSelected += OnLanguageSelected;
        _viewModel.InputMethodSelected += OnInputMethodSelected;
        _viewModel.InputChanged += OnInputChanged;
        _viewModel.InputMethodFlagsChanged += OnInputMethodFlagsChanged;
        _viewModel.OutputUnicodeCopied += OnOutputUnicodeCopied;

        _viewModel.Languages = LanguageRegistry.GetLanguages();
    }

    private void OnLanguageSelected(object? sender, ILanguage language)
    {
        _viewModel.InputMethods = LanguageRegistry.GetInputMethods(language);
    }

    private void OnInputMethodSelected(object? sender, IInputMethod inputMethod)
    {
        _viewModel.InputMethodFlags = LanguageRegistry.GetInputMethodFlags(inputMethod);

        // Refresh the output when the input method changes
        _viewModel.OutputText = ComputeOutputText();
    }

    private void OnInputChanged(object? sender, string input)
    {
        _viewModel.OutputText = ComputeOutputText(input);
    }

    private void OnInputMethodFlagsChanged(object? sender, EventArgs e)
    {
        // Refresh the output when the input method flags change
        _viewModel.OutputText = ComputeOutputText();
    }

    private async void OnOutputUnicodeCopied(object? sender, (string Text, int WrapAtColumn) e)
    {
        await Clipboard!.SetTextAsync(FormatUnicodeCodepoints(e.Text, e.WrapAtColumn));
    }

    private string ComputeOutputText(string? input = null)
    {
        input ??= _viewModel.InputText;

        var tokens = _viewModel.ActiveInputMethod?.Tokenize(input);
        if (tokens is null) return String.Empty;

        var output = _viewModel.ActiveLanguage?.Process(tokens);
        if (output is null) return String.Empty;

        return output;
    }

    private string FormatUnicodeCodepoints(string output, int wrapAt)
    {
        var index = 0;
        var sb = new StringBuilder();

        foreach (var codepoint in output.AsCodePoints())
        {
            if (index > 0)
            {
                var wrap = wrapAt > 0 && index % wrapAt == 0;
                sb.Append(wrap ? "\n" : " ");
            }

            sb.Append($"{codepoint:X4}");
            index += 1;
        }

        return sb.ToString();
    }
}