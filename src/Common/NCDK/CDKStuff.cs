/* 
 * Copyright (C) 2018  Kazuya Ujihara <ujihara.kazuya@gmail.com>
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation; either version 2.1
 * of the License, or (at your option) any later version.
 * All we ask is that proper credit is given for our work, which includes
 * - but is not limited to - adding the above copyright notice to the beginning
 * of your source code files, and to any copyright notice that you may distribute
 * with programs based on this work.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT Any WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;

namespace NCDK
{
    [System.AttributeUsage(AttributeTargets.Property | AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    sealed class AlwaysErrorAttribute : Attribute
    {
        // This is a positional argument
        public AlwaysErrorAttribute()
        {
        }
    }

    internal static class CDKStuff
    {
        public static IReadOnlyList<TSource> ToReadOnlyList<TSource>(this IEnumerable<TSource> source)
        {
            switch (source)
            {
                case IReadOnlyList<TSource> list:
                    return list;
                case IList<TSource> list:
                    return new ReadOnlyCollection<TSource>(list);
                default:
                    return new List<TSource>(source);
            }
        }

        private static Dictionary<string, string> TypeNameConvesionMap { get; } = new Dictionary<string, string>()
        {
            ["AtomContainer2"] = "AtomContainer",
        };

        private static Dictionary<string, string> PropertyNameConvesionMap { get; } = new Dictionary<string, string>()
        {
            ["Atoms"] = "#A",
            ["Abundance"] = "AB",
            ["AtomicNumber"] = "AN",
            ["AtomTypeName"] = "N",
            ["Order"] = "#O",
            ["BondOrderSum"] = "BOS",
            ["Bonds"] = "#B",
            ["Charge"] = "C",
            ["CovalentRadius"] = "CR",
            ["CTerminus"] = "C",
            ["ElectronCount"] = "EC",
            ["ExactMass"] = "EM",
            ["FormalCharge"] = "FC",
            ["FractionalPoint3D"] = "F3D",
            ["FormalNeighbourCount"] = "NC",
            ["Hybridization"] = "H",
            ["Id"] = "ID",
            ["ImplicitHydrogenCount"] = "HC",
            ["LonePairs"] = "#LP",
            ["Mappings"] = "#M",
            ["MassNumber"] = "MN",
            ["MaxBondOrder"] = "MBO",
            ["MonomerName"] = "M",
            ["MonomerType"] = "T",
            ["NTerminus"] = "N",
            ["Point2D"] = "2D",
            ["Point3D"] = "3D",
            ["Reactions"] = "R",
            ["SingleElectrons"] = "#SE",
            ["SpaceGroup"] = "SG",
            ["Stereo"] = "#S",
            ["StereoElements"] = "#ST",
            ["StereoParity"] = "SP",
            ["StrandName"] = "N",
            ["StrandType"] = "T",
            ["Symbol"] = "S",
            ["Valency"] = "EV",
        };

        private static readonly HashSet<string> listSkips = new HashSet<string>()
        {
            "Begin",
            "Builder",
            "Count",
            "ElectronContainer",
            "Element",
            "End",
            "Index",
            "IsPlaced",
            "IsVisited",
            "Name",
            "Notification",
            "SpaceGroup",
        };

        private static readonly HashSet<string> listTreatAsChemObjects = new HashSet<string>()
        {
            "Agents",
            "AssociatedAtoms",
            "Atoms",
            "Bonds",
            "LonePairs",
            "Mappings",
            "Products",
            "Reactants",
            "Reactions",
            "SingleElectrons",
            "StereoElements",
        };

        /// <summary>
        /// Utility method to convert an object especially <see cref="IChemObject"/> to string.
        /// </summary>
        /// <param name="obj">The object to convert.</param>
        /// <returns>The string expression of <paramref name="obj"/></returns>
        public static string ToString(object obj)
        {
            return new StringMaker(obj).ToString();
        }

        class StringMaker
        {
            private readonly List<object> outputtedList;
            private readonly object obj;

            public StringMaker(object obj, StringMaker parent = null)
            {
                this.outputtedList = parent == null ? new List<object>() : parent.outputtedList;
                switch (obj)
                {
                    case AtomRef o: obj = o.Deref(); break;
                    case BondRef o: obj = o.Deref(); break;
                }
                this.obj = obj;
            }

            public override string ToString()
            {
                var twice = outputtedList.Contains(obj);
                var sb = new StringBuilder();
                string typeName = obj.GetType().Name;
                if (TypeNameConvesionMap.ContainsKey(typeName))
                    typeName = TypeNameConvesionMap[typeName];
                sb.Append(typeName);
                sb.Append("(");
                var list = new List<string> { obj.GetHashCode().ToString() };
                if (obj is IEnumerable<IChemObject> v)
                {
                    var count = v.Count();
                    if (count > 0)
                    {
                        var s = string.Join(", ", v.Select(o => ToString(o)));
                        list.Add($"#:{count}[{s}]");
                    }
                    else
                    {
                        list.Add($"#:{count}");
                    }
                }
                if (!twice)
                {
                    outputtedList.Add(obj);
                    list.AddRange(PropertiesAsStrings(obj));
                }
                sb.Append(string.Join(", ", list));
                sb.Append(")");
                return sb.ToString();
            }

            private string ToString(object o)
            {
                if (o == null)
                    return "null";
                if (o is IChemObject)
                {
                    return new StringMaker((IChemObject)o, this).ToString();
                }
                else
                    return o.ToString();
            }

            interface IA
            {
                string Name { get; }
                object Value { get; }
                Type PropertyType { get; }
            }

            class APropertyInfo
                : IA
            {
                private readonly PropertyInfo p;
                private readonly object obj;

                public APropertyInfo(PropertyInfo p, object obj)
                {
                    this.p = p;
                    this.obj = obj;
                }

                public string Name => p.Name;
                public object Value
                {
                    get
                    {
                        var a = p.GetCustomAttribute<AlwaysErrorAttribute>();
                        if (a != null)
                            return null;
                        return p.GetValue(obj);
                    }
                }

                public Type PropertyType => p.PropertyType;
            }

            class ADicProperties
                : IA
            {
                private readonly KeyValuePair<object, object> p;
                private readonly object obj;

                public ADicProperties(KeyValuePair<object, object> p, object obj)
                {
                    this.p = p;
                    this.obj = obj;
                }

                public string Name => p.Key.ToString();
                public object Value => p.Value;
                public Type PropertyType => Value?.GetType();
            }

            private IEnumerable<IA> MakeIAs(object obj)
            {
                var x = obj.GetType().GetProperties().Select(a => new APropertyInfo(a, obj)).Cast<IA>();
                if (obj is IChemObject co)
                {
                    x = x.Concat(co.GetProperties().Select(a => new ADicProperties(a, obj)));
                }
                return x;
            }

            public IEnumerable<string> PropertiesAsStrings(object obj)
            {
                var namesFound = new List<string>();
                foreach (var p in MakeIAs(obj))
                {
                    if (namesFound.Contains(p.Name))
                        continue;
                    namesFound.Add(p.Name);
                    string str = null;
                    try
                    {
                        var ptype = p.PropertyType;
                        if (ptype == null)
                            continue;
                        if (listSkips.Contains(p.Name))
                            continue;
                        if (!PropertyNameConvesionMap.TryGetValue(p.Name, out string name))
                            name = p.Name;
                        if (listTreatAsChemObjects.Contains(p.Name))
                        {
                            if (!(p.Value is IEnumerable<IChemObject> v))
                                continue;
                            if (v.Count() == 0)
                                continue;
                            str = $"{name}:{v.Count()}[{string.Join(", ", v.Select(o => ToString(o)))}]";
                        }
                        else
                        {
                            if (ptype != typeof(string) && typeof(System.Collections.IEnumerable).IsAssignableFrom(ptype))
                                continue;
                            var v = p.Value;
                            if (v == null)
                                continue;
                            if (ptype.IsEnum)
                            {
                                if ((int)v == 0)
                                    continue;
                            }
                            else if (ptype.IsValueType)
                            {
                                switch (v)
                                {
                                    case bool n: if (!n) continue; break;
                                    case char n: if (n == 0) continue; break;
                                    case short n: if (n == 0) continue; break;
                                    case int n: if (n == 0) continue; break;
                                    case long n: if (n == 0) continue; break;
                                    case float n: if (n == 0) continue; break;
                                    case double n: if (n == 0) continue; break;
                                    default: break;
                                }
                            }
                            var sb = new StringBuilder();
                            sb.Append(name);
                            if (ptype != typeof(bool))
                            {
                                sb.Append(":");
                                if (ptype == typeof(string))
                                    sb.Append("\"");
                                sb.Append(ToString(v));
                                if (ptype == typeof(string))
                                    sb.Append("\"");
                            }
                            str = sb.ToString();
                        }
                    }
                    catch (Exception)
                    {
                    }
                    if (str != null)
                        yield return str;
                }
                yield break;
            }
        }
    }
}
