using System;
using System.Collections.Generic;
using System.Text;

using Avalonia;
using Avalonia.Media;

using ConlangIME.Core;

namespace ConlangIME.Languages;

[Language]
public class Senkalicu : ILanguage
{
    private static readonly FontFamily FontFamily =
        LanguageRegistry.GetFont<Senkalicu>();

    public string Name => "Senkalicu";
    public FontFamily Font => FontFamily;
    public double FontSize => 27.0;

    private const int FinalBase = 0xE000;
    private const int InitBase = 0xE080;
    private const int IsolBase = 0xE230;
    private const int NumsBase = 0xE250;
    private const int PuncBase = 0xE270;
    private const int LogoBase = 0xE2A0;

    private static readonly Dictionary<char, int> Final1Map = Utils.IndexMap("aeoöiuü");
    private static readonly Dictionary<char, int> Final2Map = Utils.IndexMap("-iuümnlrfsšhptk");

    private static readonly Dictionary<char, int> Init1Map = Utils.IndexMap("pbtdkgfvszšžhcčmnlry");
    private static readonly Dictionary<char, int> Init2Map = Utils.IndexMap("-pbtdkgfvszšžhcčmnlry");

    private static readonly Dictionary<char, int> IsolMap = Utils.IndexMap("aeoöiuüʔpbtdkgfvszšžhcčmnlry");
    private static readonly Dictionary<char, int> NumsMap = Utils.IndexMap("0123456789ABCDEFG");

    private static readonly Dictionary<string, int> PuncMap = Utils.IndexMap(new[] {
        "nspace", "wspace", "period", "comma", "ndash", "wdash", "apost", "lquot",
        "rquot", "parstart", null, null, null, null, "tone2", "tone1",
    });

    private static readonly Dictionary<string, int> LogoMap = Utils.IndexMap(new[] {
        "num", "name", "time", "yes", "no",
    });

    public string Process(IEnumerable<Token> tokens) {
        var sb = new StringBuilder();

        foreach(var tk in tokens) {
            if (!tk.IsSub)
            {
                sb.Append(tk.Value);
                continue;
            }

            var par = tk.Value.Split('.');
            var (type, par1) = (par[0], par[1]);

            char c, c1, c2;

            switch (type) {
            case "init":
                (c1, c2) = par1.Length < 2 ? (par1[0], '-') : (par1[0], par1[1]);
                c = (char)(InitBase + Init1Map[c1] + Init2Map[c2] * Init1Map.Count);
                break;

            case "final":
                (c1, c2) = par1.Length < 2 ? (par1[0], '-') : (par1[0], par1[1]);
                c = (char)(FinalBase + Final1Map[c1] + Final2Map[c2] * Final1Map.Count);
                break;

            case "isol":
                c = (char)(IsolBase + IsolMap[par1[0]]);
                break;

            case "num":
                c = (char)(NumsBase + NumsMap[par1[0]]);
                break;

            case "punc":
                c = (char)(PuncBase + PuncMap[par1]);
                break;

            case "logo":
                c = (char)(LogoBase + LogoMap[par1]);
                break;

            default:
                throw new FormatException("invalid token type");
            }

            sb.Append(c);
        }

        return sb.ToString();
    }
}
