using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ConlangIME.Languages;

namespace ConlangIME.InputMethods {
    [InputMethod(typeof(SenkalicuV2))]
    public class SenkaliRomV2 : IInputMethod {
        public string Name => "SenkaliRom v2";

        [Flags]
        enum CharT {
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

        static readonly Dictionary<char, CharT> CharTypes =
            new Dictionary<char, CharT> {
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

        static readonly Dictionary<char, char> SubSingle =
            new Dictionary<char, char> {
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

        static readonly Dictionary<(char, char), char> SubDigraph =
            new Dictionary<(char, char), char> {
                // Letters
                { ('e', 'o'), 'ö' },
                { ('E', 'O'), 'Ö' },
                { ('i', 'u'), 'ü' },
                { ('I', 'U'), 'Ü' },

                // Punctuation
                { (' ',  ' ' ), '\t' },
                { ('-',  '-' ), '—'  },
                { ('\'', '\''), '”'  },
                { ('`',  '`' ), '„'  },

                // Pitch accent marks
                { ('^', '1'), '¹' },
                { ('^', '2'), '²' },
            };

        static readonly Dictionary<char, string> Punctuation =
            new Dictionary<char, string> {
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

        static readonly Dictionary<string, string> Logograms =
            new Dictionary<string, string> {
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


        static readonly char LogogramStart = '\\';
        static readonly HashSet<char> KeyChars = Utils.CharRanges("az", "AZ", "09");

        static readonly string DigitChars = "G123456789ABCDEF";
        static readonly Dictionary<char, int> DigitVals = Utils.IndexMap("0123456789ABCDEF");

        static readonly HashSet<char> RawTokens = new HashSet<char>(" \t\n\r");


        static readonly IEnumerable<string> ZeroNumber = new[] { "num.0" };

        static readonly Dictionary<char, string> ToneMarkers =
            new Dictionary<char, string> {
                { '\u0301', "punc.tone1" }, // COMBINING ACUTE ACCENT
                { '\u030b', "punc.tone1" }, // COMBINING DOUBLE ACUTE ACCENT
                { '\u0300', "punc.tone2" }, // COMBINING GRAVE ACCENT
                { '\u030f', "punc.tone2" }, // COMBINING DOUBLE GRAVE ACCENT
            };

        #endregion

        private Queue<Token> AuxTokens = new Queue<Token>(16);

        private void AuxExtractTone(ref char input) {
            var decomp = input.ToString().Normalize(NormalizationForm.FormD);
            if(decomp.Length != 2) throw new ArgumentException();

            var tone = ToneMarkers[decomp[1]];
            AuxTokens.Enqueue(Token.Sub(tone));
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
                    c1 = SubSingle.GetOrDefault(c1, c1);
                }

                return (c1, CharTypes.GetOrDefault(c1, CharT.Null));
            }

            (char, CharT) PeekChar(int n) {
                char c1 = isr.Peek(n);
                return (c1, CharTypes.GetOrDefault(c1, CharT.Null));
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

        private List<int> NumBuf = new List<int>(32);

        private IEnumerable<Token> ReadNumber(StringReaderEx isr) {
            NumBuf.Clear();

            while(DigitVals.TryGetValue(isr.Peek(1), out var dig)) {
                isr.Advance(1);
                NumBuf.Add(dig);
            }

            if(NumBuf.Count == 0) return null;

            if(NumBuf.Sum() == 0) {
                return ZeroNumber.Select(Token.Sub);
            }

            IEnumerable<Token> Generator() {
                int nbase = DigitChars.Length;
                int first = NumBuf.FindIndex(x => x > 0);

                for(int i = NumBuf.Count - 1; i > first; i -= 1) {
                    if(NumBuf[i] > 0) continue;
                    NumBuf[i - 1] -= 1;
                    NumBuf[i] += nbase;
                }

                first = NumBuf.FindIndex(first, x => x > 0);

                for(int i = first; i < NumBuf.Count; i += 1) {
                    char ch = DigitChars[NumBuf[i] % nbase];
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
                while(AuxTokens.Count > 0) {
                    yield return AuxTokens.Dequeue();
                }

                if(ReadToken(isr) is Token tok) {
                    yield return tok;
                    continue;
                }

                if(ReadNumber(isr) is IEnumerable<Token> ntoks) {
                    foreach(var ntok in ntoks)
                        yield return ntok;
                    continue;
                }

                if(ReadLogograph(isr) is Token ltok) {
                    yield return ltok;
                    continue;
                }

                string raw = isr.ReadWhile(ch => RawTokens.Contains(ch));
                if(raw.Length > 0) {
                    yield return Token.Raw(raw);
                    continue;
                }

                isr.Advance(1);
            }

            while(AuxTokens.Count > 0) {
                yield return AuxTokens.Dequeue();
            }
        }
    }
}
