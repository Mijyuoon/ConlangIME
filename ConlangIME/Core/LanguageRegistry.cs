using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;

using Avalonia;
using Avalonia.Media;

namespace ConlangIME.Core;

public static class LanguageRegistry
{
    private static readonly List<ILanguage> Languages = new();
    private static readonly Dictionary<Type, List<IInputMethod>> InputMethods = new();
    private static readonly Dictionary<Type, List<IInputMethodFlag>> InputMethodFlags = new();

    public static void Initialize()
    {
        Languages.Clear();
        InputMethods.Clear();
        InputMethodFlags.Clear();

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
            if (attrib is null) continue;

            var instance = (IInputMethod)Activator.CreateInstance(type)!;

            if (InputMethods.TryGetValue(attrib.Language, out var value))
            {
                value.Add(instance);
            }
            else
            {
                InputMethods.Add(attrib.Language, [instance]);
            }

            var flagProxies = new List<IInputMethodFlag>();
            InputMethodFlags.Add(type, flagProxies);

            foreach (var propInfo in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!propInfo.CanRead || !propInfo.CanWrite) continue;

                var propAttrib = propInfo.GetCustomAttribute<InputMethodFlagAttribute>();
                if (propAttrib is null) continue;

                flagProxies.Add(new InputMethodFlagProxy(propAttrib.Label, instance, propInfo));
            }
        }
    }

    public static IReadOnlyList<ILanguage> GetLanguages() =>
        new ReadOnlyCollection<ILanguage>(Languages);

    public static IReadOnlyList<IInputMethod> GetInputMethods(ILanguage language) =>
        InputMethods.TryGetValue(language.GetType(), out var inputMethods)
            ? new ReadOnlyCollection<IInputMethod>(inputMethods)
            : ReadOnlyCollection<IInputMethod>.Empty;

    public static IReadOnlyList<IInputMethodFlag> GetInputMethodFlags(IInputMethod inputMethod) =>
        InputMethodFlags.TryGetValue(inputMethod.GetType(), out var inputMethodFlags)
            ? new ReadOnlyCollection<IInputMethodFlag>(inputMethodFlags)
            : ReadOnlyObservableCollection<IInputMethodFlag>.Empty;

    public static FontFamily GetFont<T>() where T : ILanguage
    {
        var fontKey = $"Font:{typeof(T).Name}";
        if (Application.Current?.TryGetResource(fontKey, null, out var resource) ?? false)
        {
            return resource as FontFamily ?? FontFamily.Default;
        }

        return FontFamily.Default;
    }

    private class InputMethodFlagProxy(string label, IInputMethod instance, PropertyInfo property) : IInputMethodFlag
    {
        private readonly Func<bool> _proxyGetter =
            (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), instance, property.GetMethod!);

        private readonly Action<bool> _proxySetter =
            (Action<bool>)Delegate.CreateDelegate(typeof(Action<bool>), instance, property.SetMethod!);

        public string Label { get; } = label;

        public bool Value
        {
            get => _proxyGetter();
            set => SetProperty(value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void SetProperty(bool value)
        {
            _proxySetter(value);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
        }
    }
}