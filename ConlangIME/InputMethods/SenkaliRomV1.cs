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
                { "/", null }, { " ", "nspace" }, { "  ", "wspace" }, { "\n", "\n" },

                { "eo", "ø" }, { "ö", "ø" }, { "iu", "y" }, { "ü", "y" },
                { "x", "ʃ" }, { "š", "ʃ" }, { "j", "ʒ" }, { "ž", "ʒ" }, { "h", "x" },
                { "tc", "ts" }, { "ç", "ts" }, { "c", "tʃ" },
                { "y", "j" }, { "q", "ʔ" },
            };
        
        static Regex ScanRegex = new Regex(
            @"((?>tc|[pbtdkgqfvszšxžjhçcmnlry])?(?>(?>eo|[aeoö])[iu]|(?>eo|iu|[aeoöiuü])(?>(?>[pkfsšxhmnlr]|t(?!c))(?![aeoöiuü]))?)|(?>tc|[pbtdkgqfvszšxžjhçcmnlry]))|( {1,2}|[\/\n])|.+?",
            RegexOptions.Compiled);

        static Regex SubstRegex = new Regex(
            @"eo|iu|tc|[öüxšjžhçcyq]",
            RegexOptions.Compiled);

        public IEnumerable<Token> Tokenize(string input) {
            foreach(Match rm in ScanRegex.Matches(input)) {
                if(rm.Groups[1].Success) {

                    string tok = rm.Groups[1].Value;
                    tok = SubstRegex.Replace(tok, x => Substitute[x.Value]);

                    yield return Token.Sub(tok);

                } else if(rm.Groups[2].Success) {

                    string tok = rm.Groups[2].Value;
                    tok = Substitute.GetOrDefault(tok, tok);

                    if(tok != null) {
                        yield return Token.Sub(tok);
                    }

                } else {
                    yield return Token.Raw(rm.Value);
                }
            }
        }
    }
}
