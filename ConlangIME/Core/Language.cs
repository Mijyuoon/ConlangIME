using System;
using System.Collections.Generic;

using Avalonia.Media;

namespace ConlangIME.Core;

public struct Token {
    public string Value;

    public bool IsSub;

    public static Token Sub(string value) =>
        new() { Value = value, IsSub = true };

    public static Token Raw(string value) =>
        new() { Value = value, IsSub = false };
}

public interface ILanguage {
    string Name { get; }

    FontFamily Font { get; }

    double FontSize { get; }

    string Process(IEnumerable<Token> tokens);
}

public interface IInputMethod {
    string Name { get; }

    IEnumerable<Token> Tokenize(string input);
}

[AttributeUsage(AttributeTargets.Class)]
public class LanguageAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Class)]
public class InputMethodAttribute(Type language) : Attribute
{
    public Type Language { get; } = language;
}