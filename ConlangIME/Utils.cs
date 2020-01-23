﻿using System;
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
        public static Dictionary<T, int> IndexMap<T>(IEnumerable<T> list) =>
            list.Select((x, i) => new { k = x, v = i }).ToDictionary(x => x.k, x => x.v);
    }
}
