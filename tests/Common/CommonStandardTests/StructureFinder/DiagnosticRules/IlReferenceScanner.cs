#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace CompMs.Common.StructureFinder.DiagnosticRules.Tests;

/// <summary>
/// Lists every type and member a set of types references (design 6.1 item 2): each method body's IL is decoded and
/// its operands resolved with the generic context, and field, parameter, return, local, base and interface types are
/// added. Each reference is judged by an allowlist; what it does not allow is reported with a readable reason.
/// </summary>
internal sealed class IlReferenceScanner
{
    public const string EngineNamespace = "CompMs.Common.StructureFinder.DiagnosticRules";

    private static readonly OpCode[] OneByte = new OpCode[256];
    private static readonly OpCode[] TwoByte = new OpCode[256];

    static IlReferenceScanner() {
        foreach (var f in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static)) {
            if (f.GetValue(null) is OpCode op) {
                var v = unchecked((ushort)op.Value);
                if (v < 0x100) {
                    OneByte[v] = op;
                }
                else if ((v & 0xFF00) == 0xFE00) {
                    TwoByte[v & 0xFF] = op;
                }
            }
        }
    }

    public sealed class Finding
    {
        public Finding(Type scanned, string site, string reference, string reason) {
            Scanned = scanned;
            Site = site;
            Reference = reference;
            Reason = reason;
        }

        public Type Scanned { get; }
        public string Site { get; }
        public string Reference { get; }
        public string Reason { get; }

        public override string ToString() => Scanned.FullName + "::" + Site + " -> " + Reference + ": " + Reason;
    }

    /// <summary>The context of one reference.</summary>
    public sealed class Site
    {
        public Site(Type scanned, Assembly engine, bool inLoading, bool compilerGenerated, long? lastConstant, Type? constrained) {
            Scanned = scanned;
            Engine = engine;
            InLoading = inLoading;
            CompilerGenerated = compilerGenerated;
            LastConstant = lastConstant;
            Constrained = constrained;
        }

        public Type Scanned { get; }
        public Assembly Engine { get; }
        public bool InLoading { get; }
        public bool CompilerGenerated { get; }
        public long? LastConstant { get; }
        public Type? Constrained { get; }
    }

    private readonly Assembly _engine;
    private readonly List<Finding> _findings = new List<Finding>();

    /// <param name="engine">The assembly whose engine namespace is allowed (types of it outside that namespace are not).</param>
    public IlReferenceScanner(Assembly engine) {
        _engine = engine;
    }

    public IReadOnlyList<Finding> Findings => _findings;

    /// <summary>How many member references were judged (a scan that decodes nothing would pass vacuously).</summary>
    public int MemberReferences { get; private set; }

    public static bool IsCompilerGenerated(Type t) {
        for (var x = t; x != null; x = x.DeclaringType) {
            if (x.IsDefined(typeof(CompilerGeneratedAttribute), false) || x.Name.StartsWith("<", StringComparison.Ordinal)) {
                return true;
            }
        }
        return false;
    }

    /// <summary>Scans the given types as engine code; <paramref name="inLoading"/> grants the .Loading allowances.</summary>
    public void Scan(Type type, bool inLoading) {
        var generated = IsCompilerGenerated(type);
        Site Ctx(long? constant = null, Type? constrained = null) => new Site(type, _engine, inLoading, generated, constant, constrained);
        if (type.BaseType != null) {
            CheckType(type.BaseType, Ctx(), "base type");
        }
        var inherited = type.BaseType?.GetInterfaces() ?? Type.EmptyTypes;
        foreach (var i in type.GetInterfaces().Except(inherited)) {
            CheckType(i, Ctx(), "interface"); // interfaces a base class brings (Exception's ISerializable) are the base's
        }
        const BindingFlags all = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        foreach (var f in type.GetFields(all)) {
            CheckType(f.FieldType, Ctx(), "field " + f.Name);
        }
        var methods = type.GetMethods(all).Cast<MethodBase>().Concat(type.GetConstructors(all));
        foreach (var m in methods) {
            var site = m.Name;
            foreach (var p in m.GetParameters()) {
                CheckType(p.ParameterType, Ctx(), site + " parameter " + p.Name);
            }
            if (m is MethodInfo mi) {
                CheckType(mi.ReturnType, Ctx(), site + " return");
            }
            var body = m.GetMethodBody();
            if (body is null) {
                continue;
            }
            foreach (var local in body.LocalVariables) {
                CheckType(local.LocalType, Ctx(), site + " local");
            }
            ScanIl(m, body.GetILAsByteArray(), type, Ctx);
        }
    }

    private void ScanIl(MethodBase method, byte[] il, Type type, Func<long?, Type?, Site> ctx) {
        var typeArgs = method.DeclaringType != null && method.DeclaringType.IsGenericType ? method.DeclaringType.GetGenericArguments() : null;
        var methodArgs = method.IsGenericMethod ? method.GetGenericArguments() : null;
        var module = method.Module;
        var pos = 0;
        long? lastConstant = null;
        Type? constrained = null;
        while (pos < il.Length) {
            OpCode op;
            var b = il[pos++];
            op = b == 0xFE ? TwoByte[il[pos++]] : OneByte[b];
            long? constant = null;
            switch (op.OperandType) {
                case OperandType.InlineNone:
                    if (op.Value >= OpCodes.Ldc_I4_M1.Value && op.Value <= OpCodes.Ldc_I4_8.Value) {
                        constant = op.Value - OpCodes.Ldc_I4_0.Value;
                    }
                    break;
                case OperandType.ShortInlineI:
                    if (op == OpCodes.Ldc_I4_S) {
                        constant = unchecked((sbyte)il[pos]);
                    }
                    pos += 1;
                    break;
                case OperandType.ShortInlineBrTarget:
                case OperandType.ShortInlineVar:
                    pos += 1;
                    break;
                case OperandType.InlineVar:
                    pos += 2;
                    break;
                case OperandType.InlineI:
                    if (op == OpCodes.Ldc_I4) {
                        constant = BitConverter.ToInt32(il, pos);
                    }
                    pos += 4;
                    break;
                case OperandType.InlineBrTarget:
                case OperandType.InlineString:
                case OperandType.ShortInlineR:
                    pos += 4;
                    break;
                case OperandType.InlineSig:
                    pos += 4;
                    _findings.Add(new Finding(type, method.Name, "calli", "indirect call"));
                    break;
                case OperandType.InlineI8:
                case OperandType.InlineR:
                    pos += 8;
                    break;
                case OperandType.InlineSwitch:
                    pos += 4 + 4 * BitConverter.ToInt32(il, pos);
                    break;
                case OperandType.InlineMethod:
                case OperandType.InlineField:
                case OperandType.InlineType:
                case OperandType.InlineTok: {
                    var token = BitConverter.ToInt32(il, pos);
                    pos += 4;
                    var member = module.ResolveMember(token, typeArgs, methodArgs);
                    if (op == OpCodes.Constrained && member is Type ct) {
                        constrained = ct;
                        CheckType(ct, ctx(null, null), method.Name);
                        continue; // the prefix applies to the next call
                    }
                    CheckMember(member!, ctx(lastConstant, constrained), method.Name);
                    constrained = null;
                    break;
                }
                default:
                    throw new InvalidOperationException("unknown operand type " + op.OperandType);
            }
            lastConstant = constant;
        }
    }

    private void Report(Site site, string where, string reference, string? reason) {
        if (reason != null) {
            _findings.Add(new Finding(site.Scanned, where, reference, reason));
        }
    }

    private static string Describe(MemberInfo m) {
        if (m is MethodBase mb) {
            return mb.DeclaringType?.FullName + "::" + mb.Name + "(" + string.Join(", ", mb.GetParameters().Select(p => p.ParameterType.Name)) + ")";
        }
        return m.DeclaringType?.FullName + "::" + m.Name;
    }

    private void CheckMember(MemberInfo member, Site site, string where) {
        switch (member) {
            case Type t:
                CheckType(t, site, where);
                return;
            case FieldInfo f:
                CheckType(f.DeclaringType!, site, where);
                CheckType(f.FieldType, site, where);
                return;
            case MethodBase m:
                MemberReferences++;
                if (m.DeclaringType != null) {
                    CheckType(m.DeclaringType, site, where);
                }
                if (m is MethodInfo mi) {
                    foreach (var a in mi.IsGenericMethod ? mi.GetGenericArguments() : Type.EmptyTypes) {
                        CheckType(a, site, where);
                    }
                }
                Report(site, where, Describe(m), EngineRules.MemberReason(m, site));
                return;
        }
    }

    private readonly HashSet<string> _typeSeen = new HashSet<string>(StringComparer.Ordinal);

    private void CheckType(Type t, Site site, string where) {
        while (t.HasElementType) {
            t = t.GetElementType()!;
        }
        if (t.IsGenericParameter) {
            return;
        }
        if (t.IsGenericType && !t.IsGenericTypeDefinition) {
            foreach (var a in t.GetGenericArguments()) {
                CheckType(a, site, where);
            }
            t = t.GetGenericTypeDefinition();
        }
        var key = site.Scanned.FullName + "|" + site.InLoading + "|" + t.AssemblyQualifiedName;
        if (!_typeSeen.Add(key)) {
            return;
        }
        Report(site, where, t.FullName ?? t.Name, EngineRules.TypeReason(t, site));
    }
}

/// <summary>The allowlist of design 6.1 item 2 and the culture bans of 6.2.</summary>
internal static class EngineRules
{
    private static readonly HashSet<Type> Numeric = new HashSet<Type>
    {
        typeof(byte), typeof(sbyte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong),
        typeof(float), typeof(double), typeof(decimal), typeof(System.Numerics.BigInteger), typeof(DateTime), typeof(DateTimeOffset), typeof(TimeSpan),
    };

    private static readonly string[] AllowedNamespaces =
    {
        "System.Collections", "System.Collections.Generic", "System.Collections.ObjectModel", "System.Linq", "System.Text",
        "System.Text.RegularExpressions", "System.Globalization",
    };

    private static readonly string[] DeniedSystemTypes =
    {
        "Environment", "AppDomain", "GC", "Activator", "Console", "Random", "Convert", "TimeZone", "TimeZoneInfo",
    };

    public static string? TypeReason(Type t, IlReferenceScanner.Site site) {
        var ns = t.Namespace ?? "";
        var name = t.Name;
        if (t.Assembly == site.Scanned.Assembly && ns == (site.Scanned.Namespace ?? "")) {
            return null; // the scanned type's own namespace (planted test types and their compiler-generated helpers)
        }
        if (t.Assembly == site.Engine && (ns == IlReferenceScanner.EngineNamespace || ns.StartsWith(IlReferenceScanner.EngineNamespace + ".", StringComparison.Ordinal))) {
            if (ns.EndsWith(".Motifs", StringComparison.Ordinal) && !site.Scanned.Namespace!.EndsWith(".Motifs", StringComparison.Ordinal)) {
                return "only .Motifs may reference .Motifs";
            }
            return null;
        }
        if (t.Assembly == site.Engine || t.Assembly == site.Scanned.Assembly) {
            if ((t.FullName ?? "").StartsWith("<PrivateImplementationDetails>", StringComparison.Ordinal)) {
                return null; // compiler support (string switch hashes, array initialisers)
            }
            if (ns == "System.Runtime.CompilerServices" || ns == "Microsoft.CodeAnalysis") {
                return null; // embedded compiler attributes
            }
        }
        if (ns.StartsWith("CompMs.", StringComparison.Ordinal) || ns == "CompMs") {
            return "another CompMs.Common namespace (formula, data object, parser or sibling): not allowed in the engine";
        }
        if (ns.StartsWith("Rfx.", StringComparison.Ordinal) || ns.StartsWith("Riken.", StringComparison.Ordinal)) {
            return "MS-FINDER or MS-DIAL types are not allowed in the engine";
        }
        if (ns.StartsWith("NCDK", StringComparison.Ordinal)) {
            return site.Scanned.Namespace!.EndsWith(".Motifs", StringComparison.Ordinal) ? null : "NCDK only in .Motifs";
        }
        if (ns == "System") {
            if (DeniedSystemTypes.Contains(name)) {
                if (name == "Environment" && site.CompilerGenerated) {
                    return null; // judged per member: only the iterator's managed thread id
                }
                return "System." + name + " (process state, nondeterminism or culture) is not allowed";
            }
            return null;
        }
        if (AllowedNamespaces.Contains(ns)) {
            return null;
        }
        if (ns == "System.Numerics") {
            return name == "BigInteger" ? null : "only BigInteger of System.Numerics";
        }
        if (ns == "System.Security.Cryptography") {
            return name == "SHA256" || name == "SHA256CryptoServiceProvider" || name == "HashAlgorithm" ? null : "only the SHA256 types of System.Security.Cryptography";
        }
        if (ns == "System.IO") {
            if (name == "Stream" || name == "MemoryStream") {
                return null;
            }
            if (site.InLoading && (name == "File" || name == "Directory" || name == "Path" || name == "SearchOption" || name == "DirectoryNotFoundException")) {
                return null;
            }
            return "System.IO." + name + (site.InLoading ? "" : " outside .Loading") + " is not allowed";
        }
        if (ns == "System.Reflection") {
            return site.InLoading && name == "Assembly" ? null : "reflection is not allowed" + (site.InLoading ? "" : " outside .Loading");
        }
        if (ns == "System.Runtime.CompilerServices") {
            return name == "RuntimeHelpers" || name.EndsWith("Attribute", StringComparison.Ordinal) || name == "IsExternalInit" ? null : "compiler services other than attributes and InitializeArray";
        }
        if (ns.StartsWith("System.Threading", StringComparison.Ordinal)) {
            return "threading is not allowed (no parallelism, no tasks, no locks)";
        }
        if (ns.StartsWith("System.Diagnostics", StringComparison.Ordinal)) {
            return "System.Diagnostics (Stopwatch, Debug, Process) is not allowed";
        }
        if (ns.StartsWith("System.Net", StringComparison.Ordinal) || ns.StartsWith("System.Runtime.InteropServices", StringComparison.Ordinal)) {
            return ns + " is not allowed";
        }
        return "not on the allowlist (" + (t.FullName ?? name) + ")";
    }

    private static bool HasParameter(MethodBase m, params Type[] types) => m.GetParameters().Any(p => types.Contains(p.ParameterType));

    private static bool IsString(Type t) => t == typeof(string);

    public static string? MemberReason(MethodBase m, IlReferenceScanner.Site site) {
        var t = m.DeclaringType;
        if (t is null) {
            return null;
        }
        var name = m.Name;
        var ps = m.GetParameters();
        var gt = t.IsGenericType ? t.GetGenericTypeDefinition() : t;
        var targs = t.IsGenericType ? t.GetGenericArguments() : Type.EmptyTypes;
        // process state, reflection, nondeterminism
        if (t == typeof(Environment)) {
            return site.CompilerGenerated && name == "get_CurrentManagedThreadId" ? null : "System.Environment." + name;
        }
        if (t == typeof(Type)) {
            if (site.InLoading && (name == "GetTypeFromHandle" || name == "get_Assembly")) {
                return null;
            }
            return "System.Type." + name + " (reflection) is not allowed" + (site.InLoading ? "" : " outside .Loading");
        }
        if (t == typeof(Assembly)) {
            return site.InLoading && (name == "GetManifestResourceNames" || name == "GetManifestResourceStream") ? null : "Assembly." + name;
        }
        if (t == typeof(DateTime) && (name == "get_Now" || name == "get_UtcNow" || name == "get_Today")) {
            return "the clock is not allowed";
        }
        if (t == typeof(DateTimeOffset) && (name == "get_Now" || name == "get_UtcNow")) {
            return "the clock is not allowed";
        }
        if (t == typeof(Guid) && name == "NewGuid") {
            return "Guid.NewGuid is not allowed";
        }
        if (t == typeof(RuntimeHelpers) && name != "InitializeArray") {
            return "RuntimeHelpers." + name;
        }
        // culture: formatting and parsing without a provider
        if (Numeric.Contains(t) && name == "ToString" && !ps.Any(p => typeof(IFormatProvider).IsAssignableFrom(p.ParameterType))) {
            return t.Name + ".ToString without an IFormatProvider";
        }
        if (t == typeof(object) && name == "ToString" && site.Constrained != null && Numeric.Contains(site.Constrained)) {
            return site.Constrained.Name + ".ToString through a constrained call";
        }
        if (Numeric.Contains(t) && (name == "Parse" || name == "TryParse") && !ps.Any(p => typeof(IFormatProvider).IsAssignableFrom(p.ParameterType))) {
            return t.Name + "." + name + " without an IFormatProvider";
        }
        if (t == typeof(string)) {
            switch (name) {
                case "Format":
                    return ps.Length > 0 && ps[0].ParameterType == typeof(IFormatProvider) ? null : "string.Format without an IFormatProvider";
                case "Concat":
                    return ps.All(p => IsString(p.ParameterType) || p.ParameterType == typeof(string[]) || p.ParameterType == typeof(IEnumerable<string>))
                        ? null : "string.Concat of boxed values";
                case "Join":
                    return ps.Skip(1).All(p => p.ParameterType == typeof(string[]) || p.ParameterType == typeof(IEnumerable<string>) || IsString(p.ParameterType) || p.ParameterType == typeof(int))
                        && !m.IsGenericMethod ? null : "string.Join of boxed values";
                case "Compare":
                    return HasParameter(m, typeof(StringComparison)) ? null : "string.Compare without a StringComparison";
                case "CompareTo":
                    return "string.CompareTo is culture-sensitive";
                case "StartsWith":
                case "EndsWith":
                case "IndexOf":
                case "LastIndexOf":
                    return ps.Length > 0 && IsString(ps[0].ParameterType) && !HasParameter(m, typeof(StringComparison))
                        ? "string." + name + "(string) without a StringComparison" : null;
                case "ToUpper":
                case "ToLower":
                    return "string." + name + " is culture-sensitive; use the invariant form";
            }
        }
        if (t == typeof(char) && (name == "ToUpper" || name == "ToLower") && ps.Length == 1) {
            return "char." + name + " is culture-sensitive";
        }
        if (t == typeof(System.Text.StringBuilder)) {
            if (name == "AppendFormat") {
                return ps.Length > 0 && ps[0].ParameterType == typeof(IFormatProvider) ? null : "StringBuilder.AppendFormat without an IFormatProvider";
            }
            if (name == "Append" || name == "Insert") {
                var types = ps.Select(p => p.ParameterType).ToArray();
                var allowed = types.All(p => p == typeof(string) || p == typeof(char) || p == typeof(char[]) || p == typeof(int) || p == typeof(System.Text.StringBuilder));
                // (char, int) is a repeat count and (string, int, int) a substring: no number is formatted.
                var formats = types.Length == 1 && types[0] == typeof(int) || name == "Insert" && types.Length == 2 && types[1] == typeof(int);
                return allowed && !formats ? null : "StringBuilder." + name + "(" + string.Join(", ", types.Select(x => x.Name)) + ") formats with the current culture";
            }
        }
        // default-comparer string sorts
        if (gt == typeof(List<>) && targs[0] == typeof(string) && (name == "Sort" || name == "BinarySearch") && !ps.Any(p => typeof(IComparer<string>).IsAssignableFrom(p.ParameterType) || p.ParameterType == typeof(Comparison<string>))) {
            return "List<string>." + name + " with the default (culture) comparer";
        }
        if (t == typeof(Array) && name == "Sort" && m is MethodInfo am && am.IsGenericMethod && am.GetGenericArguments()[0] == typeof(string) && !ps.Any(p => p.ParameterType.IsGenericType && p.ParameterType.GetGenericTypeDefinition() == typeof(IComparer<>))) {
            return "Array.Sort<string> with the default (culture) comparer";
        }
        if (t == typeof(Enumerable) && m is MethodInfo em && em.IsGenericMethod) {
            var g = em.GetGenericArguments();
            if ((name == "OrderBy" || name == "OrderByDescending" || name == "ThenBy" || name == "ThenByDescending") && ps.Length == 2 && g.Length == 2 && g[1] == typeof(string)) {
                return "Enumerable." + name + " by a string key without a comparer";
            }
            if ((name == "Min" || name == "Max") && g[0] == typeof(string) && ps.Length == 1) {
                return "Enumerable." + name + " of strings uses the culture comparer";
            }
        }
        if (gt == typeof(Comparer<>) && targs[0] == typeof(string) && name == "get_Default") {
            return "Comparer<string>.Default is culture-sensitive";
        }
        if ((gt == typeof(SortedSet<>) || gt == typeof(SortedDictionary<,>) || gt == typeof(SortedList<,>)) && targs[0] == typeof(string) && m.IsConstructor
            && !ps.Any(p => typeof(IComparer<string>).IsAssignableFrom(p.ParameterType))) {
            return gt.Name + " of strings without a comparer";
        }
        // RegexOptions.IgnoreCase without CultureInvariant
        if (t == typeof(System.Text.RegularExpressions.Regex) && ps.Any(p => p.ParameterType == typeof(System.Text.RegularExpressions.RegexOptions)) && site.LastConstant.HasValue) {
            var options = (System.Text.RegularExpressions.RegexOptions)site.LastConstant.Value;
            if ((options & System.Text.RegularExpressions.RegexOptions.IgnoreCase) != 0 && (options & System.Text.RegularExpressions.RegexOptions.CultureInvariant) == 0) {
                return "RegexOptions.IgnoreCase without CultureInvariant";
            }
        }
        return null;
    }
}
