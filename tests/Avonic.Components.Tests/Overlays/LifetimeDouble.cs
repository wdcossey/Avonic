using System.Reflection;
using System.Reflection.Emit;
using Avalonia.Controls.ApplicationLifetimes;

namespace Avonic.Components.Tests.Overlays;

/// <summary>
/// A lifetime that implements Avalonia's lifetime interfaces, each property a plain field. Avalonia's reference assemblies give
/// each interface a member that user code can't implement, so C# can't declare one, but the runtime assemblies don't have it, so
/// a type made at run time can. Android's lifetime is internal to <c>Avalonia.Android</c>, which the tests can't load, and is
/// both an <see cref="IActivityApplicationLifetime"/> and an <see cref="ISingleViewApplicationLifetime"/>.
/// </summary>
internal static class LifetimeDouble
{
    private static readonly ModuleBuilder s_module = AssemblyBuilder
        .DefineDynamicAssembly(new AssemblyName("Avonic.LifetimeDoubles"), AssemblyBuilderAccess.Run)
        .DefineDynamicModule("Avonic.LifetimeDoubles");

    private static readonly Dictionary<string, Type> s_types = [];

    /// <summary>Android's: <c>MainViewFactory</c> and <c>MainView</c>.</summary>
    public static IApplicationLifetime Android() => Create(typeof(IActivityApplicationLifetime), typeof(ISingleViewApplicationLifetime));

    /// <summary>iOS's and the browser's: <c>MainView</c> alone.</summary>
    public static IApplicationLifetime SingleView() => Create(typeof(ISingleViewApplicationLifetime));

    private static IApplicationLifetime Create(params Type[] interfaces)
    {
        var name = string.Join("And", interfaces.Select(i => i.Name));
        lock (s_types)
        {
            if (!s_types.TryGetValue(name, out var type))
            {
                type = Define(name, interfaces);
                s_types[name] = type;
            }

            return (IApplicationLifetime)Activator.CreateInstance(type)!;
        }
    }

    private static Type Define(string name, Type[] interfaces)
    {
        var all = interfaces.SelectMany(i => i.GetInterfaces().Prepend(i)).Distinct().ToArray();
        var type = s_module.DefineType(name, TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Class, typeof(object), all);
        const MethodAttributes accessor = MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final
                                          | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.SpecialName;
        foreach (var property in all.SelectMany(i => i.GetProperties()))
        {
            var field = type.DefineField("_" + property.Name, property.PropertyType, FieldAttributes.Private);
            var built = type.DefineProperty(property.Name, PropertyAttributes.None, property.PropertyType, null);
            if (property.GetMethod is { } get)
            {
                var getter = type.DefineMethod(get.Name, accessor, property.PropertyType, Type.EmptyTypes);
                var il = getter.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, field);
                il.Emit(OpCodes.Ret);
                type.DefineMethodOverride(getter, get);
                built.SetGetMethod(getter);
            }

            if (property.SetMethod is { } set)
            {
                var setter = type.DefineMethod(set.Name, accessor, typeof(void), [property.PropertyType]);
                var il = setter.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Stfld, field);
                il.Emit(OpCodes.Ret);
                type.DefineMethodOverride(setter, set);
                built.SetSetMethod(setter);
            }
        }

        return type.CreateType();
    }
}
