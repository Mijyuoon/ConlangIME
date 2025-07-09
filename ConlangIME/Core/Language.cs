using System;
using System.Collections.Generic;
using System.ComponentModel;

using Avalonia.Media;

namespace ConlangIME.Core;

public record struct Token(string Value, bool IsSub)
{
    public static Token Sub(string value) => new(value, true);

    public static Token Raw(string value) => new(value, false);
}

public interface ILanguage
{
    string Name { get; }

    FontFamily Font { get; }

    double FontSize { get; }

    string Process(IEnumerable<Token> tokens);
}

public interface IInputMethod
{
    string Name { get; }

    IEnumerable<Token> Tokenize(string input);
}

public interface IInputMethodFlag : INotifyPropertyChanged
{
    string Label { get; }

    bool Value { get; set; }
}

[AttributeUsage(AttributeTargets.Class)]
public class LanguageAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Class)]
public class InputMethodAttribute(Type language) : Attribute
{
    public Type Language { get; } = language;
}

[AttributeUsage(AttributeTargets.Property)]
public class InputMethodFlagAttribute(string label) : Attribute
{
    public string Label { get; } = label;
}