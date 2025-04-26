using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using ConlangIME.Languages;

namespace ConlangIME.InputMethods
{
    [InputMethod(typeof(Mwadengrukay))]
    public class MwadengRomV1 : IInputMethod
    {
        public string Name => "MwadengRomV1";

        private const string LetterToken = "letr";
        private const string PunctToken = "punc";

        private const string MarkConsCluster = "mark.cc";
        private const string MarkGeminate = "mark.gc";

        private const string VowelA = "a";

        private static readonly Dictionary<char, string> PunctMap = new()
        {
            { ',', "brk1" }, { '.', "brk2" }, { '-', "list" },
            { '(', "par1lt" }, { '{', "par2lt" }, { '[', "numlt" },
            { ')', "par1rt" }, { '}', "par2rt" }, { ']', "numrt" },
        };

        private static readonly Regex ScanRegex = new(
            @"([ptčkbdžgfsšxzɣmnñŋwryl])|([aeiou])|([\,\.\-\(\)\[\]\{\}])|.",
            RegexOptions.Compiled);

        public IEnumerable<Token> Tokenize(string input)
        {
            var firstVowelA = true;
            var lastConsonant = String.Empty;

            foreach (Match rm in ScanRegex.Matches(input))
            {
                if (rm.Groups[1].Success)
                {
                    var tok = rm.Groups[1].Value;

                    if (tok == lastConsonant)
                    {
                        yield return Token.Sub(MarkGeminate);
                    }
                    else
                    {
                        if (!String.IsNullOrEmpty(lastConsonant))
                        {
                            yield return Token.Sub(MarkConsCluster);
                        }

                        yield return Token.Sub($"{LetterToken}.{tok}");
                    }

                    firstVowelA = false;
                    lastConsonant = tok;
                }
                else if (rm.Groups[2].Success)
                {
                    var tok = rm.Groups[2].Value;

                    if (tok != VowelA || firstVowelA)
                    {
                        yield return Token.Sub($"{LetterToken}.{tok}");
                    }

                    firstVowelA = true;
                    lastConsonant = String.Empty;
                }
                else if (rm.Groups[3].Success)
                {
                    var tok = PunctMap[rm.Groups[3].Value[0]];

                    if (!String.IsNullOrEmpty(lastConsonant))
                    {
                        yield return Token.Sub(MarkConsCluster);
                    }

                    yield return Token.Sub($"{PunctToken}.{tok}");

                    firstVowelA = true;
                    lastConsonant = String.Empty;
                }
                else
                {
                    if (!String.IsNullOrEmpty(lastConsonant))
                    {
                        yield return Token.Sub(MarkConsCluster);
                    }

                    yield return Token.Raw(rm.Value);

                    firstVowelA = true;
                    lastConsonant = String.Empty;
                }
            }

            if (!String.IsNullOrEmpty(lastConsonant))
            {
                yield return Token.Sub(MarkConsCluster);
            }
        }
    }
}