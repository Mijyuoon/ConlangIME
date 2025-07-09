using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using ConlangIME.Core;

using JetBrains.Annotations;

using ConlangIME.Languages;

namespace ConlangIME.InputMethods;

[UsedImplicitly]
[InputMethod(typeof(Mwadengrukay))]
public class MwadengRomV2 : IInputMethod
{
    public string Name => "MwadengRom v2";

    [InputMethodFlag("Vowel Ligatures")]
    public bool VowelLigatures { get; set; } = false;

    [Flags]
    private enum CharT
    {
        Null = 0,

        Cons = 1 << 0,
        Vowel = 1 << 1,
        Punc = 1 << 2,

        VowelA = 1 << 3,
        Glide = 1 << 4,
    }

    #region Data Constants

    private static readonly Dictionary<char, CharT> CharTypes = new()
    {
        { 'a', CharT.Vowel | CharT.VowelA },
        { 'e', CharT.Vowel },
        { 'i', CharT.Vowel },
        { 'o', CharT.Vowel },
        { 'u', CharT.Vowel },

        { 'p', CharT.Cons },
        { 't', CharT.Cons },
        { 'č', CharT.Cons },
        { 'k', CharT.Cons },
        { 'b', CharT.Cons },
        { 'd', CharT.Cons },
        { 'ž', CharT.Cons },
        { 'g', CharT.Cons },
        { 'f', CharT.Cons },
        { 's', CharT.Cons },
        { 'š', CharT.Cons },
        { 'x', CharT.Cons },
        { 'z', CharT.Cons },
        { 'ɣ', CharT.Cons },
        { 'm', CharT.Cons },
        { 'n', CharT.Cons },
        { 'ñ', CharT.Cons },
        { 'ŋ', CharT.Cons },
        { 'w', CharT.Cons | CharT.Glide },
        { 'r', CharT.Cons },
        { 'y', CharT.Cons | CharT.Glide },
        { 'l', CharT.Cons },

        { ',', CharT.Punc },
        { '.', CharT.Punc },
        { '-', CharT.Punc },
        { '(', CharT.Punc },
        { ')', CharT.Punc },
        { '{', CharT.Punc },
        { '}', CharT.Punc },
        { '_', CharT.Punc },
    };

    private static readonly Dictionary<char, char> SubSingle = new()
    {
        { 'c', 'č' },
        { 'j', 'ž' },
    };

    private static readonly Dictionary<(char, char), char> SubDigraph = new()
    {
        { ('c', 'h'), 'č' },
        { ('j', 'h'), 'ž' },
        { ('s', 'h'), 'š' },
        { ('k', 'h'), 'x' },
        { ('g', 'h'), 'ɣ' },
        { ('n', 'y'), 'ñ' },
        { ('n', 'g'), 'ŋ' },
    };

    private static readonly HashSet<(char, char)> GemDigraph = new()
    {
        ('s', 'š'),
        ('k', 'x'),
        ('g', 'ɣ'),
        ('n', 'ñ'),
        ('n', 'ŋ'),
    };

    private static readonly Dictionary<char, string> Punctuation = new()
    {
        { ',', "brk1" },
        { '.', "brk2" },
        { '-', "list" },
        { '(', "par1lt" },
        { ')', "par1rt" },
        { '{', "par2lt" },
        { '}', "par2rt" },
        { '_', "empty" },
    };

    private const string Joiner = "join.1";
    private const string NonJoiner = "join.0";

    private const string LetrPrefix = "letr";
    private const string PuncPrefix = "punc";

    private const string MarkSupprVowel = "mark.cc";
    private const string MarkGeminated = "mark.gc";
    private const string MarkLongVowel = "mark.aa";

    private const string NumberOpen = "punc.numlt";
    private const string NumberClose = "punc.numrt";

    private static readonly HashSet<char> NumberChars = new("0123456789");

    private static readonly IEnumerable<string> NumberEmpty = new[]
    {
        NumberOpen, "punc.brk1", NumberClose,
    };

    private static readonly IEnumerable<(int, char)> NumberMajors = new[]
    {
        (30, 'u'), (24, 'o'), (18, 'i'), (12, 'e'), (6, 'a'), (0, Char.MinValue),
    };

    private static readonly IEnumerable<(int, char)> NumberMinors = new[]
    {
        (5, 'ñ'), (4, 'p'), (3, 'l'), (2, 'č'), (1, 'x'), (0, Char.MinValue),
    };

    private static readonly IReadOnlyList<char> NumberDigits = new List<char>
    {
        Char.MinValue, 'k', 'd', 'n', 'b', 't', 'ɣ', 'g', 'm', 'f', 'ž',
    };

    static readonly HashSet<char> RawTokens = new(" \t\n\r");

    #endregion

    private IEnumerable<Token>? ReadLetters(StringReaderEx isr)
    {
        (char, CharT) ReadChar()
        {
            var c1 = Char.ToLowerInvariant(isr.Read());
            var c2 = Char.ToLowerInvariant(isr.Peek(1));

            if (SubDigraph.TryGetValue((c1, c2), out var cd))
            {
                c1 = cd;
                isr.Advance(1);
            }
            else
            {
                c1 = SubSingle.GetValueOrDefault(c1, c1);
            }

            return (c1, CharTypes.GetValueOrDefault(c1, CharT.Null));
        }

        List<Token>? result = null;

        isr.Backtrack(() =>
        {
            var (c1, t1) = ReadChar();
            if (t1 == CharT.Null) return false;

            result = new List<Token>(capacity: 6);

            if ((t1 & CharT.Cons) != 0)
            {
                var skipConsonant = isr.Backtrack(() =>
                {
                    var (c2, t2) = ReadChar();
                    if ((t2 & CharT.Cons) == 0) return false;

                    if (c1 == c2 || GemDigraph.Contains((c1, c2)))
                    {
                        // For geminates the correct consonant is always the second one
                        result.Add(Token.Sub($"{LetrPrefix}.{c2}"));
                        result.Add(Token.Sub(MarkGeminated));
                        return true;
                    }

                    if ((t2 & CharT.Glide) != 0 && (t1 & CharT.Glide) == 0)
                    {
                        // Add both parts of a consonant-glide ligature
                        result.Add(Token.Sub($"{LetrPrefix}.{c1}"));
                        result.Add(Token.Sub($"{LetrPrefix}.{c2}"));
                        return true;
                    }

                    return false;
                });

                if (!skipConsonant)
                {
                    // Add the single consonant if this isn't a special case
                    result.Add(Token.Sub($"{LetrPrefix}.{c1}"));
                }

                isr.Backtrack(() =>
                {
                    var (c2, t2) = ReadChar();

                    if ((t2 & CharT.Vowel) == 0)
                    {
                        // Add the vowel suppressor diacritic if this consonant isn't followed by a vowel
                        result.Add(Token.Sub(MarkSupprVowel));
                    }
                    else if ((t2 & CharT.VowelA) != 0)
                    {
                        isr.Backtrack(() =>
                        {
                            var (_, t3) = ReadChar();
                            if ((t3 & CharT.VowelA) == 0) return false;

                            // Add the diacritic for an implied long A vowel
                            result.Add(Token.Sub(MarkLongVowel));
                            return true;
                        });

                        isr.Backtrack(() =>
                        {
                            var (_, t3) = ReadChar();
                            if ((t3 & CharT.Glide) == 0) return false;

                            // Add a non-joiner if the next letter would form an unwanted ligature
                            result.Add(Token.Sub(NonJoiner));
                            return false;
                        });

                        return true;
                    }

                    return false;
                });
            }
            else if ((t1 & CharT.Vowel) != 0)
            {
                result.Add(Token.Sub($"{LetrPrefix}.{c1}"));

                bool longVowel = false;

                isr.Backtrack(() =>
                {
                    var (c2, t2) = ReadChar();
                    if ((t2 & CharT.Vowel) == 0) return false;

                    if (c1 == c2)
                    {
                        // Cannot emit the long A vowel diacritic immediately
                        // As it would prevent vowel-glide ligatures from forming
                        longVowel = true;
                        return true;
                    }

                    return false;
                });

                if (VowelLigatures)
                {
                    isr.Backtrack(() =>
                    {
                        var (c2, t2) = ReadChar();
                        if ((t2 & CharT.Glide) == 0) return false;

                        var sameSyllable = true;

                        isr.Backtrack(() =>
                        {
                            var (_, t3) = ReadChar();
                            if ((t3 & CharT.Vowel) == 0) return false;

                            // Check if this glide belongs to the same syllable as the preceding vowel
                            sameSyllable = false;
                            return false;
                        });

                        // Do not add a joiner if this glide belongs to the next syllable
                        if (!sameSyllable) return false;

                        result.Add(Token.Sub(Joiner));
                        result.Add(Token.Sub($"{LetrPrefix}.{c2}"));
                        return true;
                    });
                }

                if (longVowel)
                {
                    // Add the long A vowel diacritic at the end
                    result.Add(Token.Sub(MarkGeminated));
                }
            }
            else if ((t1 & CharT.Punc) != 0)
            {
                result.Add(Token.Sub($"{PuncPrefix}.{Punctuation[c1]}"));
            }

            return true;
        });

        return result;
    }

    private IEnumerable<Token>? ReadNumbers(StringReaderEx isr)
    {
        var numText = isr.ReadWhile(ch => NumberChars.Contains(ch));
        if (numText.Length == 0) return null;

        var number = BigInteger.Parse(numText);
        if (number.IsZero)
        {
            // Special case for the zero
            return NumberEmpty.Select(Token.Sub);
        }

        var result = new List<Token>(capacity: 14);
        result.Add(Token.Sub(NumberOpen));

        // Major groups are every 10^6
        var majMod = (BigInteger)1000000;

        // Minor groups are every 10^1
        var minMod = (BigInteger)10;

        foreach (var (majPow, majCh) in NumberMajors)
        {
            var majDiv = BigInteger.Pow(10, majPow);
            var majVal = number / majDiv % majMod;

            if (majVal.IsZero) continue;

            // Special case for 10^1 having a dedicated symbol
            if (majVal == minMod)
            {
                var digCh = NumberDigits[(int)majVal];
                result.Add(Token.Sub($"{LetrPrefix}.{digCh}"));
            }
            else
            {
                foreach (var (minPow, minCh) in NumberMinors)
                {
                    var minDiv = BigInteger.Pow(10, minPow);
                    var minVal = majVal / minDiv % minMod;

                    if (minVal.IsZero) continue;

                    var digCh = NumberDigits[(int)minVal];
                    if (digCh == Char.MinValue) continue;

                    result.Add(Token.Sub($"{LetrPrefix}.{digCh}"));

                    if (minCh == Char.MinValue) continue;

                    result.Add(Token.Sub($"{LetrPrefix}.{minCh}"));
                }
            }

            if (majCh == Char.MinValue) continue;

            result.Add(Token.Sub($"{LetrPrefix}.{majCh}"));
        }

        result.Add(Token.Sub(NumberClose));
        return result;
    }

    public IEnumerable<Token> Tokenize(string input)
    {
        var isr = new StringReaderEx(input);

        while (!isr.IsEOF)
        {
            if (ReadNumbers(isr) is {} numbers)
            {
                foreach (var tok in numbers) yield return tok;
                continue;
            }

            if (ReadLetters(isr) is {} letters)
            {
                foreach (var tok in letters) yield return tok;
                continue;
            }

            var raw = isr.ReadWhile(ch => RawTokens.Contains(ch));
            if (raw.Length > 0)
            {
                yield return Token.Raw(raw);
                continue;
            }

            isr.Advance(1);
        }
    }
}