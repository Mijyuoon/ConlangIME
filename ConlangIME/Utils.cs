using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConlangIME {
    public static class ExtensionUtils {
        public static V GetOrDefault<K, V>(this Dictionary<K, V> dict, K key, V defaultVal) =>
            dict.TryGetValue(key, out var value) ? value : defaultVal;

        public static V GetOrDefault<K, V>(this Dictionary<K, V> dict, K key) =>
            GetOrDefault(dict, key, default(V));
    }

    public static class Utils {
        public static IEnumerable<string[]> ReadTabulated(TextReader reader) {
            while(true) {
                var values = reader.ReadLine()?.Split('\t');
                if(values == null) break;

                yield return values;
            }
        }

        public static IEnumerable<string[]> ReadTabulated(string input) => ReadTabulated(new StringReader(input));
    }
}
