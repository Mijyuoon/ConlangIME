using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using ConlangIME.Languages;

namespace ConlangIME.Languages {
    [Language]
    public partial class Senkalitcu : ILanguage {
        static FontFamily FontFamily = new FontFamily(new Uri("pack://application:,,,/"), "./Fonts/#Senkalitcu");

        public string Name { get; } = "Senkaliçu";
        public FontFamily Font { get; } = FontFamily;
        public double FontSize { get; } = 24.0;

        public string Process(IEnumerable<Token> tokens) {
            var sb = new StringBuilder();

            foreach(var token in tokens) {
                if(token.IsSub) {

                    if(GlyphMap.TryGetValue(token.Value, out var glyph)) {
                        sb.Append(glyph);
                    } else {
                        sb.Append('<');
                        sb.Append(token.Value);
                        sb.Append('>');
                    }

                } else {
                    sb.Append(token.Value);
                }
            }

            return sb.ToString();
        }
    }
}
