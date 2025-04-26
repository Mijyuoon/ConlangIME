using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace ConlangIME {
    public struct Token {
        public string Value;

        public bool IsSub;

        public static Token Sub(string value) =>
            new Token { Value = value, IsSub = true };

        public static Token Raw(string value) =>
            new Token { Value = value, IsSub = false };
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

    public class LanguageAttribute : Attribute { }

    public class InputMethodAttribute : Attribute {
        public Type Language { get; }

        public InputMethodAttribute(Type language) {
            this.Language = language;
        }
    }
}
