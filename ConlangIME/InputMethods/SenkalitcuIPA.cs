using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConlangIME.Languages;
using ConlangIME.Properties;

namespace ConlangIME.InputMethods {
    [InputMethod(typeof(Senkalitcu))]
    public class SenkalitcuIPA : IInputMethod {
        public string Name => "IPA (Senkaliçu)";

        static Dictionary<string, string> Substitute =
            new Dictionary<string, string> {
                { "/", "nspace" }, { "//", "wspace" },
            };

        public IEnumerable<Token> Tokenize(string input) {
            var buffer = new StringBuilder();

            Token ProcBuf() {
                var str = buffer.ToString();
                str = Substitute.TryGetValue(str, out var subst) ? subst : str;

                buffer.Clear();
                return Token.Sub(str);
            }

            for(int i = 0; i < input.Length; i++) {
                if(input[i] == ' ' || input[i] == '\n') {

                    if(buffer.Length > 0) {
                        yield return ProcBuf();
                    }

                    if(input[i] == '\n') {
                        yield return Token.Raw("\n");
                    }

                } else if(input[i] != '\r') {
                    buffer.Append(input[i]);
                }
            }

            if(buffer.Length > 0) {
                yield return ProcBuf();
            }
        }
    }
}
