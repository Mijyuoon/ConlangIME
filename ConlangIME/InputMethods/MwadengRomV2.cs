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

    private readonly Queue<Token> _auxTokens = new(16);

    private Token? ReadToken(StringReaderEx isr)
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

        Token? output = null;

        isr.Backtrack(() =>
        {
            var (c1, t1) = ReadChar();

            if ((t1 & CharT.Cons) != 0)
            {
                isr.Backtrack(() =>
                {
                    var (c2, t2) = ReadChar();
                    if ((t2 & CharT.Cons) == 0) return false;

                    if (c1 == c2 || GemDigraph.Contains((c1, c2)))
                    {
                        // Add a consonant gemination mark
                        _auxTokens.Enqueue(Token.Sub(MarkGeminated));

                        // The correct consonant is always the second one
                        c1 = c2;

                        return true;
                    }

                    return false;
                });

                isr.Backtrack(() =>
                {
                    var (_, t2) = ReadChar();

                    if ((t2 & CharT.VowelA) != 0)
                    {
                        isr.Backtrack(() =>
                        {
                            var (_, t3) = ReadChar();
                            if ((t3 & CharT.VowelA) == 0) return false;

                            // Add a long vowel mark for the implied A vowel
                            _auxTokens.Enqueue(Token.Sub(MarkLongVowel));

                            return true;
                        });

                        // Skip the letter for the implied A vowel
                        return true;
                    }

                    if ((t2 & CharT.Vowel) == 0)
                    {
                        // Add a vowel suppressor mark if what follows is not a vowel
                        _auxTokens.Enqueue(Token.Sub(MarkSupprVowel));
                    }

                    return false;
                });

                output = Token.Sub($"{LetrPrefix}.{c1}");
                return true;
            }

            if ((t1 & CharT.Vowel) != 0)
            {
                isr.Backtrack(() =>
                {
                    var (c2, t2) = ReadChar();
                    if ((t2 & CharT.Vowel) == 0) return false;

                    if (c1 == c2)
                    {
                        // Add a gemination mark for other long vowels
                        _auxTokens.Enqueue(Token.Sub(MarkGeminated));

                        return true;
                    }

                    return false;
                });

                output = Token.Sub($"{LetrPrefix}.{c1}");
                return true;
            }

            if ((t1 & CharT.Punc) != 0)
            {
                output = Token.Sub($"{PuncPrefix}.{Punctuation[c1]}");
                return true;
            }

            return false;
        });

        return output;
    }

    private IEnumerable<Token>? ReadNumber(StringReaderEx isr)
    {
        var numText = isr.ReadWhile(ch => NumberChars.Contains(ch));
        if (numText.Length == 0) return null;

        var number = BigInteger.Parse(numText);
        if (number.IsZero)
        {
            // Special case for the zero
            return NumberEmpty.Select(Token.Sub);
        }

        IEnumerable<Token> Generator()
        {
            yield return Token.Sub(NumberOpen);

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
                    yield return Token.Sub($"{LetrPrefix}.{digCh}");
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

                        yield return Token.Sub($"{LetrPrefix}.{digCh}");

                        if (minCh == Char.MinValue) continue;

                        yield return Token.Sub($"{LetrPrefix}.{minCh}");
                    }
                }

                if (majCh == Char.MinValue) continue;

                yield return Token.Sub($"{LetrPrefix}.{majCh}");
            }

            yield return Token.Sub(NumberClose);
        }

        return Generator();
    }

    public IEnumerable<Token> Tokenize(string input)
    {
        var isr = new StringReaderEx(input);

        while (!isr.IsEOF)
        {
            while(_auxTokens.Count > 0) {
                yield return _auxTokens.Dequeue();
            }

            if (ReadNumber(isr) is {} nums)
            {
                foreach (var num in nums) yield return num;
                continue;
            }

            if (ReadToken(isr) is {} tok)
            {
                yield return tok;
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

        while (_auxTokens.Count > 0)
        {
            yield return _auxTokens.Dequeue();
        }
    }
}