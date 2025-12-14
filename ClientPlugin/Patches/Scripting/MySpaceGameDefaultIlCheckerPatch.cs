using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Timers;
using System.Xml.Serialization;
using HarmonyLib;
using SpaceEngineers.Game;
using VRage.Scripting;

namespace ClientPlugin.Patches.Scripting;

[HarmonyPatch(typeof(MySpaceGameDefaultIlChecker))]
public static class MySpaceGameDefaultIlCheckerPatch
{
    // ReSharper disable once UnusedMember.Local
    [HarmonyPrefix]
    [HarmonyPatch("AllowDefaultNamespaces")]
    private static bool AllowDefaultNamespacesPrefix(IMyWhitelistBatch handle)
    {
        // Replacement implementation

        handle.AllowNamespaceOfTypes(MyWhitelistTarget.Both, typeof(IEnumerator), typeof(HashSet<>), typeof(LinkedList<>), typeof(StringBuilder), typeof(Regex), typeof(Calendar));
        handle.AllowNamespaceOfTypes(MyWhitelistTarget.ModApi, typeof(Enumerable), typeof(ConcurrentBag<>));
        handle.AllowTypes(MyWhitelistTarget.Ingame, (from x in typeof(Enumerable).Assembly.GetTypes()
            where x.Namespace == "System.Linq"
            where !x.Name.StringExtensions_Contains("parallel", StringComparison.InvariantCultureIgnoreCase)
            select x).ToArray());
        handle.AllowNamespaceOfTypes(MyWhitelistTarget.ModApi, typeof(Timer));
        handle.AllowTypes(MyWhitelistTarget.ModApi, typeof(TraceEventType), typeof(AssemblyProductAttribute), typeof(AssemblyDescriptionAttribute), typeof(AssemblyConfigurationAttribute), typeof(AssemblyCompanyAttribute), typeof(AssemblyCultureAttribute), typeof(AssemblyVersionAttribute), typeof(AssemblyFileVersionAttribute), typeof(AssemblyCopyrightAttribute), typeof(AssemblyTrademarkAttribute), typeof(AssemblyTitleAttribute), typeof(ComVisibleAttribute), typeof(DefaultValueAttribute), typeof(SerializableAttribute), typeof(GuidAttribute), typeof(StructLayoutAttribute), typeof(LayoutKind),
            typeof(Guid));
        handle.AllowTypes(MyWhitelistTarget.Both, typeof(object), typeof(IDisposable), typeof(string), typeof(StringComparison), typeof(Math), typeof(Enum), typeof(int), typeof(short), typeof(long), typeof(uint), typeof(ushort), typeof(ulong), typeof(double), typeof(float), typeof(bool), typeof(char), typeof(byte), typeof(sbyte), typeof(decimal), typeof(DateTime), typeof(TimeSpan), typeof(Array), typeof(XmlElementAttribute), typeof(XmlAttributeAttribute), typeof(XmlArrayAttribute), typeof(XmlArrayItemAttribute), typeof(XmlAnyAttributeAttribute), typeof(XmlAnyElementAttribute),
            typeof(XmlAnyElementAttributes), typeof(XmlArrayItemAttributes), typeof(XmlAttributeEventArgs), typeof(XmlAttributeOverrides), typeof(XmlAttributes), typeof(XmlChoiceIdentifierAttribute), typeof(XmlElementAttributes), typeof(XmlElementEventArgs), typeof(XmlEnumAttribute), typeof(XmlIgnoreAttribute), typeof(XmlIncludeAttribute), typeof(XmlRootAttribute), typeof(XmlTextAttribute), typeof(XmlTypeAttribute), typeof(RuntimeHelpers), typeof(BinaryReader), typeof(BinaryWriter), typeof(NullReferenceException), typeof(ArgumentException), typeof(ArgumentNullException),
            typeof(InvalidOperationException), typeof(FormatException), typeof(Exception), typeof(DivideByZeroException), typeof(InvalidCastException), typeof(FileNotFoundException), typeof(NotSupportedException), typeof(Nullable<>), typeof(StringComparer), typeof(IEquatable<>), typeof(IComparable), typeof(IComparable<>), typeof(BitConverter), typeof(FlagsAttribute), typeof(Path), typeof(Random), typeof(Convert), typeof(StringSplitOptions), typeof(DateTimeKind), typeof(MidpointRounding), typeof(EventArgs), typeof(Buffer), typeof(INotifyPropertyChanging), typeof(PropertyChangingEventHandler),
            typeof(PropertyChangingEventArgs), typeof(INotifyPropertyChanged), typeof(PropertyChangedEventHandler), typeof(PropertyChangedEventArgs));
        handle.AllowTypes(MyWhitelistTarget.ModApi, typeof(Stream), typeof(TextWriter), typeof(TextReader));
        handle.AllowMembers(MyWhitelistTarget.Both, typeof(MemberInfo).GetProperty("Name"));
        handle.AllowMembers(MyWhitelistTarget.Both, typeof(Type).GetProperty("FullName"), typeof(Type).GetMethod("GetTypeFromHandle"), typeof(Type).GetMethod("GetFields", new Type[1] { typeof(BindingFlags) }), typeof(Type).GetMethod("IsEquivalentTo"), typeof(Type).GetMethod("op_Equality"), typeof(Type).GetMethod("ToString"));
        handle.AllowMembers(MyWhitelistTarget.Both, typeof(ValueType).GetMethod("Equals"), typeof(ValueType).GetMethod("GetHashCode"), typeof(ValueType).GetMethod("ToString"));
        handle.AllowMembers(MyWhitelistTarget.Both, typeof(Environment).GetProperty("CurrentManagedThreadId", BindingFlags.Static | BindingFlags.Public), typeof(Environment).GetProperty("NewLine", BindingFlags.Static | BindingFlags.Public), typeof(Environment).GetProperty("ProcessorCount", BindingFlags.Static | BindingFlags.Public));
        var type = typeof(Type).Assembly.GetType("System.RuntimeType");
        handle.AllowMembers(MyWhitelistTarget.Both, type.GetMethod("GetFields", new Type[] { typeof(BindingFlags) }));
        handle.AllowMembers(MyWhitelistTarget.Both, (from m in MySpaceGameDefaultIlChecker.AllDeclaredMembers(typeof(Delegate))
            where m.Name != "CreateDelegate"
            select m).ToArray());
        handle.AllowTypes(MyWhitelistTarget.Both, typeof(Action), typeof(Action<>), typeof(Action<,>), typeof(Action<,,>), typeof(Action<,,,>), typeof(Action<,,,,>), typeof(Action<,,,,,>), typeof(Action<,,,,,,>), typeof(Action<,,,,,,,>), typeof(Action<,,,,,,,,>), typeof(Action<,,,,,,,,,>), typeof(Action<,,,,,,,,,,>), typeof(Action<,,,,,,,,,,,>), typeof(Action<,,,,,,,,,,,,>), typeof(Action<,,,,,,,,,,,,,>), typeof(Action<,,,,,,,,,,,,,,>), typeof(Action<,,,,,,,,,,,,,,,>), typeof(Func<>), typeof(Func<,>), typeof(Func<,,>), typeof(Func<,,,>), typeof(Func<,,,,>), typeof(Func<,,,,,>),
            typeof(Func<,,,,,,>), typeof(Func<,,,,,,,>), typeof(Func<,,,,,,,,>), typeof(Func<,,,,,,,,,>), typeof(Func<,,,,,,,,,,>), typeof(Func<,,,,,,,,,,,>), typeof(Func<,,,,,,,,,,,,>), typeof(Func<,,,,,,,,,,,,,>), typeof(Func<,,,,,,,,,,,,,,>), typeof(Func<,,,,,,,,,,,,,,,>), typeof(Func<,,,,,,,,,,,,,,,,>));

        // Skip the original implementation
        return false;
    }

    public static bool StringExtensions_Contains(this string text, string testSequence, StringComparison comparison)
    {
        return text.IndexOf(testSequence, comparison) != -1;
    }
}