using System;
using System.Collections.Generic;
using System.Linq;

namespace ConlangIME {
    public static class ExtensionUtils {
        public static V GetOrDefault<K, V>(this Dictionary<K, V> dict, K key, V defaultValue) =>
            dict.TryGetValue(key, out var value) ? value : defaultValue;

        public static V GetOrDefault<K, V>(this Dictionary<K, V> dict, K key) =>
            dict.TryGetValue(key, out var value) ? value : default(V);

        public static IEnumerable<int> AsCodePoints(this string str) {
            for(int i = 0; i < str.Length; i++) {
                yield return Char.ConvertToUtf32(str, i);
                if(Char.IsSurrogatePair(str, i)) i++;
            }
        }
    }

    public static class Utils {
        public static Dictionary<T, int> IndexMap<T>(IEnumerable<T> list) =>
            list.Select((x, i) => new { k = x, v = i })
                .Where(x => x.k != null)
                .ToDictionary(x => x.k, x => x.v);

        public static HashSet<char> CharRanges(params string[] args) {
            IEnumerable<char> MapRange(string arg) {
                switch(arg.Length) {
                case 1:
                    yield return arg[0];
                    break;
                case 2:
                    for(char c = arg[0]; c <= arg[1]; c++) yield return c;
                    break;
                default:
                    throw new ArgumentException("invalid range", arg);
                }
            }

            return new HashSet<char>(args.SelectMany(x => MapRange(x)));
        }
    }
}
