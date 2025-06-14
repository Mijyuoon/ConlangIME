using System;
using System.Collections.Generic;
using System.Text;

using Avalonia;
using Avalonia.Media;

using ConlangIME.Core;

namespace ConlangIME.Languages;

[Language]
public class Mwadengrukay : ILanguage
{
    private static readonly FontFamily FontFamily =
        LanguageRegistry.GetFont<Mwadengrukay>();

    public string Name => "Mwadengrukay";
    public FontFamily Font => FontFamily;
    public double FontSize => 27.0;

    private const int LetterBase = 0xE000;
    private const int MarksBase = 0xE01B;
    private const int PunctBase = 0xE020;

    private static readonly Dictionary<char, int> LetterMap = Utils.IndexMap("ptčkbdžgfsšxzɣmnñŋwrylaeiou");

    private static readonly Dictionary<string, int> MarksMap = Utils.IndexMap(new[]
    {
        null, null, "aa", "cc", "gc",
    });

    private static readonly Dictionary<string, int> PunctMap = Utils.IndexMap(new[]
    {
        "brk1", "brk2", "list", "numlt", "numrt", "par1lt", "par1rt", "par2lt", "par2rt", "empty",
    });

    public string Process(IEnumerable<Token> tokens)
    {
        var sb = new StringBuilder();

        foreach (var tk in tokens)
        {
            if (!tk.IsSub)
            {
                sb.Append(tk.Value);
                continue;
            }

            var par = tk.Value.Split('.');
            var (type, par1) = (par[0], par[1]);

            sb.Append(type switch
            {
                "letr" => (char)(LetterBase + LetterMap[par1[0]]),
                "mark" => (char)(MarksBase + MarksMap[par1]),
                "punc" => (char)(PunctBase + PunctMap[par1]),
                _ => throw new FormatException("invalid token type"),
            });
        }

        return sb.ToString();
    }
}
