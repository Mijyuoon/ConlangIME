using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;

using Avalonia;
using Avalonia.Media;

namespace ConlangIME.Core;

public static class LanguageRegistry
{
    private static readonly List<ILanguage> Languages = new();
    private static readonly Dictionary<Type, List<IInputMethod>> InputMethods = new();

    public static void Initialize()
    {
        Languages.Clear();
        InputMethods.Clear();

        var allTypes = Assembly.GetExecutingAssembly().GetExportedTypes();

        foreach(var type in allTypes) {
            if (!typeof(ILanguage).IsAssignableFrom(type)) continue;

            var attrib = type.GetCustomAttribute<LanguageAttribute>();
            if (attrib is null) continue;

            var instance = (ILanguage)Activator.CreateInstance(type)!;
            Languages.Add(instance);
        }

        foreach(var type in allTypes) {
            if (!typeof(IInputMethod).IsAssignableFrom(type)) continue;

            var attrib = type.GetCustomAttribute<InputMethodAttribute>();
            if(attrib is null) continue;

            var instance = (IInputMethod)Activator.CreateInstance(type)!;

            if (InputMethods.TryGetValue(attrib.Language, out var value))
            {
                value.Add(instance);
            }
            else
            {
                InputMethods.Add(attrib.Language, [instance]);
            }
        }
    }

    public static IReadOnlyList<ILanguage> GetLanguages() =>
        new ReadOnlyCollection<ILanguage>(Languages);

    public static IReadOnlyList<IInputMethod> GetInputMethods(ILanguage language) =>
        InputMethods.TryGetValue(language.GetType(), out var inputMethods)
            ? new ReadOnlyCollection<IInputMethod>(inputMethods)
            : ReadOnlyCollection<IInputMethod>.Empty;

    public static FontFamily GetFont<T>() where T : ILanguage
    {
        var fontKey = $"Font:{typeof(T).Name}";
        if (Application.Current?.TryGetResource(fontKey, null, out var resource) ?? false)
        {
            return resource as FontFamily ?? FontFamily.Default;
        }

        return FontFamily.Default;
    }
}