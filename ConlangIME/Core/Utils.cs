using System;
using System.Collections.Generic;
using System.Linq;

using Avalonia;
using Avalonia.Media;

namespace ConlangIME.Core;

public static class StringExtensions {
    public static IEnumerable<int> AsCodePoints(this string str) {
        for(var i = 0; i < str.Length; i++) {
            yield return Char.ConvertToUtf32(str, i);
            if(Char.IsSurrogatePair(str, i)) i++;
        }
    }
}

public static class Utils {
    public static Dictionary<T, int> IndexMap<T>(IEnumerable<T?> source) where T : notnull =>
        source
            .Select((x, i) => new { k = x, v = i })
            .Where(x => x.k is not null)
            .ToDictionary(x => x.k!, x => x.v);

    public static HashSet<char> CharRanges(params string[] args) =>
        [..args.SelectMany(arg => arg.Length switch
        {
            1 => [arg[0]],
            2 => Enumerable.Range(arg[0], arg[1] - arg[0] + 1)
                    .Select(ch => (char)ch),
            _ => throw new ArgumentException("invalid range"),
        })];
}