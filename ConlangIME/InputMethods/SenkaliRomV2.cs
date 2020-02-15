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
            Cons = 1 << 0,
            Vowel = 1 << 1,
            Final = 1 << 2,
            Term = 1 << 3,
            Isol = 1 << 4,
            Punc = 1 << 5,

            Syll = Cons | Vowel,
        }

        static readonly Dictionary<char, CharT> CharTypes =
            new Dictionary<char, CharT> {
                { 'a', CharT.Vowel },
                { 'e', CharT.Vowel },
                { 'o', CharT.Vowel },
                { 'ö', CharT.Vowel },
                { 'i', CharT.Vowel | CharT.Final },
                { 'u', CharT.Vowel | CharT.Final },
                { 'ü', CharT.Vowel | CharT.Final },

                { 'p', CharT.Cons | CharT.Term },
                { 'b', CharT.Cons },
                { 't', CharT.Cons | CharT.Term },
                { 'd', CharT.Cons },
                { 'k', CharT.Cons | CharT.Term },
                { 'g', CharT.Cons },
                { 'f', CharT.Cons | CharT.Term },
                { 'v', CharT.Cons },
                { 's', CharT.Cons | CharT.Term },
                { 'z', CharT.Cons },
                { 'š', CharT.Cons | CharT.Term },
                { 'ž', CharT.Cons },
                { 'h', CharT.Cons | CharT.Term },
                { 'c', CharT.Cons },
                { 'č', CharT.Cons },
                { 'm', CharT.Cons | CharT.Final },
                { 'n', CharT.Cons | CharT.Final },
                { 'l', CharT.Cons | CharT.Final },
                { 'r', CharT.Cons | CharT.Final },
                { 'y', CharT.Cons },

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

                { ' ',  CharT.Punc },
                { '\t', CharT.Punc },
                { '.',  CharT.Punc },
                { ',',  CharT.Punc },
                { '-',  CharT.Punc },
                { '—',  CharT.Punc },
                { '’',  CharT.Punc },
                { '”',  CharT.Punc },
                { '„',  CharT.Punc },
                { '|',  CharT.Punc },

                { '#', CharT.Punc },
                { '@', CharT.Punc },
                { '$', CharT.Punc },
            };

        static readonly Dictionary<char, char> SubSingle =
            new Dictionary<char, char> {
                { 'x', 'š' },
                { 'X', 'Š' },
                { 'j', 'ž' },
                { 'J', 'Ž' },
                { 'q', 'č' },
                { 'Q', 'Č' },

                { '\u2000', ' '  }, // EN QUAD
                { '\u2002', ' '  }, // EN SPACE
                { '\u2001', '\t' }, // EM QUAD
                { '\u2003', '\t' }, // EM SPACE

                { '–',  '-' },
                { '\'', '’' },
                { '"',  '”' },
            };

        static readonly Dictionary<(char, char), char> SubDigraph =
            new Dictionary<(char, char), char> {
                { ('e', 'w'), 'ö' },
                { ('E', 'W'), 'Ö' },
                { ('i', 'w'), 'ü' },
                { ('I', 'W'), 'Ü' },

                { (' ',  ' '),  '\t' },
                { ('-',  '-'),  '—'  },
                { ('\'', '\''), '”'  },
                { ('`',  '`'),  '„'  },
            };

        static readonly Dictionary<char, string> Punctuation =
            new Dictionary<char, string> {
                { ' ',  "nspace" },
                { '\t', "wspace" },
                { '.',  "period" },
                { ',',  "comma"  },
                { '-',  "ndash"  },
                { '—',  "wdash"  },
                { '’',  "apost"  },
                { '”',  "rquot"  },
                { '„',  "lquot"  },

                { '|',  "parstart" },
            };

        static readonly Dictionary<string, string> Logograms =
            new Dictionary<string, string> {
                // Punctuation based
                { "#", "num"  },
                { "@", "name" },
                { "$", "time" },

                // Keyword based
                { "y",   "yes" },
                { "yes", "yes" },

                { "n",  "no" },
                { "no", "no" },
            };

        static readonly char LogogramStart = '\\';
        static readonly HashSet<char> KeyChars = Utils.CharRanges("az", "AZ", "09");

        static readonly string DigitChars = "0123456789ABCDEF";
        static readonly Dictionary<char, int> DigitVals = Utils.IndexMap(DigitChars);

        static readonly string[] ZeroNumber = new[] { "punc.ndash" };

        static readonly HashSet<char> RawTokens = new HashSet<char>(" \t\n\r");

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
                    string tok = $"final.{c0}";

                    isr.Backtrack(() => {
                        var (c1, t1) = ReadChar();
                        var (c2, t2) = PeekChar(1);

                        if((t1 & CharT.Final) != 0) {
                            if((t1 & CharT.Cons) != 0 && (t2 & CharT.Vowel) != 0) return false;
                            if((t1 & CharT.Vowel) != 0 && (t0 & CharT.Final) != 0) return false;
                            tok += c1;
                            return true;
                        }

                        if((t1 & CharT.Term) != 0 && (t2 & CharT.Syll) == 0) {
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
            while(DigitVals.TryGetValue(isr.Peek(1), out var dig)) {
                isr.Advance(1);
                NumBuf.Add(dig);
            }

            if(NumBuf.Count == 0) yield break;

            if(NumBuf.Sum() == 0) {
                foreach(var tok in ZeroNumber) {
                    yield return Token.Sub(tok);
                }
            } else {
                int nbase = DigitChars.Length;
                int first = NumBuf.FindIndex(x => x > 0);

                for(int i = NumBuf.Count - 1; i > first; i--) {
                    if(NumBuf[i] > 0) continue;
                    NumBuf[i - 1]--;
                    NumBuf[i] += nbase;
                }

                first = NumBuf.FindIndex(first, x => x > 0);

                for(int i = first; i < NumBuf.Count; i++) {
                    char ch = DigitChars[NumBuf[i] % nbase];
                    yield return Token.Sub($"num.{ch}");
                }
            }

            NumBuf.Clear();
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
                if(ReadToken(isr) is Token tok) {
                    yield return tok;
                    continue;
                }

                bool gotnum = false;
                foreach(var ntok in ReadNumber(isr)) {
                    yield return ntok;
                    gotnum = true;
                }
                if(gotnum) continue;

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
        }
    }
}
