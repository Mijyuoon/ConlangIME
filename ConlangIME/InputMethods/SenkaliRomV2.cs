using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ConlangIME.Core;

using JetBrains.Annotations;

using ConlangIME.Languages;

namespace ConlangIME.InputMethods;

[UsedImplicitly]
[InputMethod(typeof(Senkalicu))]
public class SenkaliRomV2 : IInputMethod {
    public string Name => "SenkaliRom v2";

    [Flags]
    private enum CharT {
        Null = 0,

        Cons  = 1 << 0, // Consonant (onset) token
        Vowel = 1 << 1, // Vowel (final) token

        Isol = 1 << 2, // Isolated token
        Punc = 1 << 3, // Punctuation token

        Coda = 1 << 4, // Coda (final) token
        Final = 1 << 5, // Word-final coda

        AuxTone = 1 << 6, // Marks pitch accent

        Syll = Cons | Vowel, // Syllable component
    }

    #region Data Constants

    private static readonly Dictionary<char, CharT> CharTypes =
        new() {
            // Vowel letters
            { 'a', CharT.Vowel },
            { 'e', CharT.Vowel },
            { 'o', CharT.Vowel },
            { 'ö', CharT.Vowel },
            { 'i', CharT.Vowel | CharT.Coda },
            { 'u', CharT.Vowel | CharT.Coda },
            { 'ü', CharT.Vowel | CharT.Coda },

            // Vowel letters w/ accent #1
            { 'á', CharT.Vowel | CharT.AuxTone },
            { 'é', CharT.Vowel | CharT.AuxTone },
            { 'ó', CharT.Vowel | CharT.AuxTone },
            { 'ő', CharT.Vowel | CharT.AuxTone },
            { 'í', CharT.Vowel | CharT.AuxTone },
            { 'ú', CharT.Vowel | CharT.AuxTone },
            { 'ű', CharT.Vowel | CharT.AuxTone },

            // Vowel letters w/ accent #2
            { 'à', CharT.Vowel | CharT.AuxTone },
            { 'è', CharT.Vowel | CharT.AuxTone },
            { 'ò', CharT.Vowel | CharT.AuxTone },
            { 'ȍ', CharT.Vowel | CharT.AuxTone },
            { 'ì', CharT.Vowel | CharT.AuxTone },
            { 'ù', CharT.Vowel | CharT.AuxTone },
            { 'ȕ', CharT.Vowel | CharT.AuxTone },

            // Consonant letters
            { 'p', CharT.Cons | CharT.Final },
            { 'b', CharT.Cons },
            { 't', CharT.Cons | CharT.Final },
            { 'd', CharT.Cons },
            { 'k', CharT.Cons | CharT.Final },
            { 'g', CharT.Cons },
            { 'f', CharT.Cons | CharT.Final },
            { 'v', CharT.Cons },
            { 's', CharT.Cons | CharT.Final },
            { 'z', CharT.Cons },
            { 'š', CharT.Cons | CharT.Final },
            { 'ž', CharT.Cons },
            { 'h', CharT.Cons | CharT.Final },
            { 'c', CharT.Cons },
            { 'č', CharT.Cons },
            { 'm', CharT.Cons | CharT.Coda },
            { 'n', CharT.Cons | CharT.Coda },
            { 'l', CharT.Cons | CharT.Coda },
            { 'r', CharT.Cons | CharT.Coda },
            { 'y', CharT.Cons },

            // Isolate letters
            { 'A', CharT.Isol },
            { 'E', CharT.Isol },
            { 'O', CharT.Isol },
            { 'Ö', CharT.Isol },
            { 'I', CharT.Isol },
            { 'U', CharT.Isol },
            { 'Ü', CharT.Isol },
            { 'ʔ', CharT.Isol },
            { 'P', CharT.Isol },
            { 'B', CharT.Isol },
            { 'T', CharT.Isol },
            { 'D', CharT.Isol },
            { 'K', CharT.Isol },
            { 'G', CharT.Isol },
            { 'F', CharT.Isol },
            { 'V', CharT.Isol },
            { 'S', CharT.Isol },
            { 'Z', CharT.Isol },
            { 'Š', CharT.Isol },
            { 'Ž', CharT.Isol },
            { 'H', CharT.Isol },
            { 'C', CharT.Isol },
            { 'Č', CharT.Isol },
            { 'M', CharT.Isol },
            { 'N', CharT.Isol },
            { 'L', CharT.Isol },
            { 'R', CharT.Isol },
            { 'Y', CharT.Isol },

            // Basic punctuation
            { ' ',  CharT.Punc },
            { '\t', CharT.Punc },
            { '.',  CharT.Punc },
            { ',',  CharT.Punc },
            { '-',  CharT.Punc },
            { '—',  CharT.Punc },
            { '’',  CharT.Punc },
            { '”',  CharT.Punc },
            { '„',  CharT.Punc },

            // Extended punctuation
            { '|',  CharT.Punc },

            // Diacritics
            { '¹',  CharT.Punc },
            { '²',  CharT.Punc },

            // Punctiation-like logographs
            { '#', CharT.Punc },
            { '@', CharT.Punc },
            { '$', CharT.Punc },
        };

    private static readonly Dictionary<char, char> SubSingle =
        new() {
            // Letters
            { 'x', 'š' },
            { 'X', 'Š' },
            { 'j', 'ž' },
            { 'J', 'Ž' },
            { 'q', 'č' },
            { 'Q', 'Č' },

            // Whitespace
            { '\u2000', ' '  }, // EN QUAD
            { '\u2002', ' '  }, // EN SPACE
            { '\u2001', '\t' }, // EM QUAD
            { '\u2003', '\t' }, // EM SPACE

            // Punctuation
            { '–',  '-' },
            { '\'', '’' },
            { '"',  '”' },
        };

    private static readonly Dictionary<(char, char), char> SubDigraph =
        new() {
            // Letters
            { ('e', 'o'), 'ö' },
            { ('E', 'O'), 'Ö' },
            { ('i', 'u'), 'ü' },
            { ('I', 'U'), 'Ü' },
            { ('s', 'y'), 'š' },
            { ('S', 'Y'), 'Š' },
            { ('z', 'y'), 'ž' },
            { ('Z', 'Y'), 'Ž' },
            { ('c', 'y'), 'č' },
            { ('C', 'Y'), 'Č' },

            // Punctuation
            { (' ',  ' ' ), '\t' },
            { ('-',  '-' ), '—'  },
            { ('\'', '\''), '”'  },
            { ('`',  '`' ), '„'  },

            // Pitch accent marks
            { ('^', '1'), '¹' },
            { ('^', '2'), '²' },
        };

    private static readonly Dictionary<char, string> Punctuation =
        new() {
            // Base punctuation
            { ' ',  "nspace" },
            { '\t', "wspace" },
            { '.',  "period" },
            { ',',  "comma"  },
            { '-',  "ndash"  },
            { '—',  "wdash"  },
            { '’',  "apost"  },
            { '”',  "rquot"  },
            { '„',  "lquot"  },

            // Extended punctuation
            { '|',  "parstart" },

            // Pitch accent marks
            { '¹', "tone1" },
            { '²', "tone2" },
        };

    private static readonly Dictionary<string, string> Logograms =
        new() {
            // Punctuation based
            { "#", "num"  },
            { "@", "name" },
            { "$", "time" },

            // Keyword based
            { "num",  "num"  },
            { "name", "name" },
            { "time", "time" },

            { "y", "yes" },
            { "n", "no"  },

            { "yes", "yes" },
            { "no",  "no"  },
        };


    private static readonly char LogogramStart = '\\';
    private static readonly HashSet<char> KeyChars = Utils.CharRanges("az", "AZ", "09");

    private static readonly string DigitChars = "G123456789ABCDEF";
    private static readonly Dictionary<char, int> DigitVals = Utils.IndexMap("0123456789ABCDEF");

    private static readonly IEnumerable<string> ZeroNumber = new[] { "num.0" };

    private static readonly HashSet<char> RawTokens = new(" \t\n\r");

    private static readonly Dictionary<char, string> ToneMarkers =
        new() {
            { '\u0301', "punc.tone1" }, // COMBINING ACUTE ACCENT
            { '\u030b', "punc.tone1" }, // COMBINING DOUBLE ACUTE ACCENT
            { '\u0300', "punc.tone2" }, // COMBINING GRAVE ACCENT
            { '\u030f', "punc.tone2" }, // COMBINING DOUBLE GRAVE ACCENT
        };

    #endregion

    private readonly Queue<Token> _auxTokens = new(16);

    private readonly List<int> _numBuf = new(32);

    private void AuxExtractTone(ref char input) {
        var decomp = input.ToString().Normalize(NormalizationForm.FormD);
        if(decomp.Length != 2) throw new ArgumentException();

        var tone = ToneMarkers[decomp[1]];
        _auxTokens.Enqueue(Token.Sub(tone));
        input = decomp[0];
    }

    private Token? ReadToken(StringReaderEx isr) {
        (char, CharT) ReadChar() {
            char c1 = isr.Read();
            char c2 = isr.Peek(1);

            if(SubDigraph.TryGetValue((c1, c2), out var cd)) {
                isr.Advance(1);
                c1 = cd;
            } else {
                c1 = SubSingle.GetValueOrDefault(c1, c1);
            }

            return (c1, CharTypes.GetValueOrDefault(c1, CharT.Null));
        }

        (char, CharT) PeekChar(int n) {
            char c1 = isr.Peek(n);
            return (c1, CharTypes.GetValueOrDefault(c1, CharT.Null));
        }

        Token? output = null;

        isr.Backtrack(() => {
            var (c0, t0) = ReadChar();

            if((t0 & CharT.Cons) != 0) {
                string tok = $"init.{c0}";

                isr.Backtrack(() => {
                    var (c1, t1) = ReadChar();

                    if((t1 & CharT.Cons) != 0) {
                        tok += c1;
                        return true;
                    }

                    return false;
                });

                output = Token.Sub(tok);
                return true;
            }

            if((t0 & CharT.Vowel) != 0) {
                if((t0 & CharT.AuxTone) != 0) {
                    AuxExtractTone(ref c0);
                }

                string tok = $"final.{c0}";

                isr.Backtrack(() => {
                    var (c1, t1) = ReadChar();
                    var (c2, t2) = PeekChar(1);

                    if((t1 & CharT.Coda) != 0) {
                        if((t1 & CharT.Cons) != 0 && (t2 & CharT.Vowel) != 0) return false;
                        if((t1 & CharT.Vowel) != 0 && (t0 & CharT.Coda) != 0) return false;
                        tok += c1;
                        return true;
                    }

                    if((t1 & CharT.Final) != 0 && (t2 & CharT.Syll) == 0) {
                        tok += c1;
                        return true;
                    }

                    return false;
                });

                output = Token.Sub(tok);
                return true;
            }

            if((t0 & CharT.Isol) != 0) {
                c0 = Char.ToLower(c0);
                output = Token.Sub($"isol.{c0}");
                return true;
            }

            if((t0 & CharT.Punc) != 0) {
                if(Punctuation.TryGetValue(c0, out var pnc)) {
                    output = Token.Sub($"punc.{pnc}");
                } else {
                    string log = Logograms[c0.ToString()];
                    output = Token.Sub($"logo.{log}");
                }

                return true;
            }

            return false;
        });

        return output;
    }

    private IEnumerable<Token> ReadNumber(StringReaderEx isr) {
        _numBuf.Clear();

        while(DigitVals.TryGetValue(isr.Peek(1), out var dig)) {
            isr.Advance(1);
            _numBuf.Add(dig);
        }

        if(_numBuf.Count == 0) return null;

        if(_numBuf.Sum() == 0) {
            return ZeroNumber.Select(Token.Sub);
        }

        IEnumerable<Token> Generator() {
            int nbase = DigitChars.Length;
            int first = _numBuf.FindIndex(x => x > 0);

            for(int i = _numBuf.Count - 1; i > first; i -= 1) {
                if(_numBuf[i] > 0) continue;
                _numBuf[i - 1] -= 1;
                _numBuf[i] += nbase;
            }

            first = _numBuf.FindIndex(first, x => x > 0);

            for(int i = first; i < _numBuf.Count; i += 1) {
                char ch = DigitChars[_numBuf[i] % nbase];
                yield return Token.Sub($"num.{ch}");
            }
        }

        return Generator();
    }

    private Token? ReadLogograph(StringReaderEx isr) {
        Token? output = null;

        isr.Backtrack(() => {
            if(isr.Read() != LogogramStart) return false;

            string key = isr.ReadWhile(ch => KeyChars.Contains(ch));
            if(key.Length == 0) return false;

            if(Logograms.TryGetValue(key.ToLower(), out var ltok)) {
                output = Token.Sub($"logo.{ltok}");
            }

            return true;
        });

        return output;
    }

    public IEnumerable<Token> Tokenize(string input) {
        var isr = new StringReaderEx(input);

        while(!isr.IsEOF) {
            while(_auxTokens.Count > 0) {
                yield return _auxTokens.Dequeue();
            }

            if(ReadToken(isr) is {} tok) {
                yield return tok;
                continue;
            }

            if(ReadNumber(isr) is {} nums) {
                foreach(var num in nums) yield return num;
                continue;
            }

            if(ReadLogograph(isr) is {} logo) {
                yield return logo;
                continue;
            }

            var raw = isr.ReadWhile(ch => RawTokens.Contains(ch));
            if(raw.Length > 0) {
                yield return Token.Raw(raw);
                continue;
            }

            isr.Advance(1);
        }

        while(_auxTokens.Count > 0) {
            yield return _auxTokens.Dequeue();
        }
    }
}