using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ConlangIME.Languages;

namespace ConlangIME.InputMethods {
    [InputMethod(typeof(Senkalitcu))]
    public class SenkaliRomV1 : IInputMethod {
        public string Name => "SenkaliRom v1";

        static Dictionary<string, string> Substitute =
            new Dictionary<string, string> {
                { "/", null }, { "\n", "\n" },
                { " ", "nspace" }, { "  ", "wspace" }, { ".", "period" }, { ",", "comma" },
                { "-",  "ndash" }, { "--",  "wdash" }, { "'",  "apost" }, { "’", "apost" },
                { "<",  "lquot" }, { "\"",  "rquot" }, { ">",  "rquot" },

                { "^", "stressmark" },

                { "eo",  "ø" }, { "ö",  "ø" }, { "iu", "y" }, { "ü", "y" },
                { "x",   "ʃ" }, { "š",  "ʃ" }, { "j",  "ʒ" }, { "ž", "ʒ" }, { "h", "x" },
                { "tc", "ts" }, { "ç", "ts" }, { "c", "tʃ" },
                { "y",   "j" }, { "q",  "ʔ" },

                { "$time",  "timestamp" }, { "$name", "propername" },
                { "$yes", "affirmative" }, { "$no",     "negative" },
                { "$num",  "numbersign" },
            };
        
        static Regex ScanRegex = new Regex(
            @"((?>tc|[pbtdkgqfvszšxžjhçcmnlry])?" +
            @"(?>(?>eo|[aeoö])(?>iu|[iuü])|(?>eo|iu|[aeoöiuü])" +
            @"(?>(?>[pkfsšxhmnlr]|t(?!c))(?![aeoöiuü]))?)" +
            @"|(?>tc|[pbtdkgqfvszšxžjhçcmnlry]))" +
            @"|(  ?|--?|[/\n.,'’^<>""]|\$\w+)" +
            @"|([0-9XY])|.",
            RegexOptions.Compiled);

        static Regex RomIpaRegex = new Regex(
            @"eo|iu|tc|[öüxšjžhçcyq]",
            RegexOptions.Compiled);

        public IEnumerable<Token> Tokenize(string input) {
            foreach(Match rm in ScanRegex.Matches(input)) {
                if(rm.Groups[1].Success) {


                    string tok = rm.Groups[1].Value;
                    tok = RomIpaRegex.Replace(tok, x => Substitute[x.Value]);

                    yield return Token.Sub(tok);

                } else if(rm.Groups[2].Success) {

                    string tok = rm.Groups[2].Value;
                    tok = Substitute.GetOrDefault(tok, tok);

                    if(tok != null && tok[0] != '$') {
                        yield return Token.Sub(tok);
                    }

                } else if(rm.Groups[3].Success) {

                    string tok = rm.Groups[3].Value;
                    yield return Token.Sub("num" + tok);
                
                } else {
                    yield return Token.Raw(rm.Value);
                }
            }
        }
    }
}
