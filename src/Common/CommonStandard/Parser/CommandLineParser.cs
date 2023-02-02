using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CompMs.Common.Parser
{
    public sealed class CommandLineParser<T> where T : new() {
        private readonly IArgument[] _positions;
        private readonly Dictionary<string, IArgument> _arguments;

        public CommandLineParser() {
            Type t = typeof(T);
            var positions = new Dictionary<int, IArgument>();
            var keywords = new Dictionary<string, IArgument>();
            foreach (var info in t.GetProperties()) {
                {
                    PositionArgumentAttribute att = (PositionArgumentAttribute)info.GetCustomAttribute(typeof(PositionArgumentAttribute));
                    if (att != null) {
                        positions[att.Position] = new PrimitiveArgument(info);
                    }
                }

                foreach (ShortStyleArgumentAttribute att in info.GetCustomAttributes(typeof(ShortStyleArgumentAttribute))) {
                    keywords[att.ShortName] = new PrimitiveArgument(info);
                }

                foreach (LongStyleArgumentAttribute att in info.GetCustomAttributes(typeof(LongStyleArgumentAttribute))) {
                    if (PrimitiveArrayArgument.IsAcceptable(info)) {
                        keywords[att.LongName] = new PrimitiveArrayArgument(info, att.Length);
                    }
                    else if (PrimitiveArgument.IsAcceptable(info)) {
                        keywords[att.LongName] = new PrimitiveArgument(info);
                    }
                }
            }

            _positions = new PrimitiveArgument[positions.Count];
            foreach (var pos in positions) {
                if ((uint)pos.Key >= (uint)_positions.Length) {
                    throw new ArgumentException($"Position is too large");
                }
                _positions[pos.Key] = pos.Value;
            }
            _arguments = keywords;
        }

        public T Parse(string[] args) {
            var result = new T();
            int i = 0;
            for (; i < _positions.Length;) {
                var args_ = new string[args.Length - i];
                Array.Copy(args, i, args_, 0, args_.Length);
                if (_positions[i].IsValid(args_)) {
                    i += _positions[i].Set(result, args_);
                }
            }
            for (; i < args.Length - 1;) {
                var args_ = new string[args.Length - i - 1];
                Array.Copy(args, i + 1, args_, 0, args_.Length);
                if (_arguments[args[i]].IsValid(args_)) {
                    i += _arguments[args[i]].Set(result, args_) + 1;
                }
            }
            return result;
        }
    }

    public static class CommandLineParser {
        public static T Parse<T>(string[] args) where T : new() {
            return new CommandLineParser<T>().Parse(args);
        }
    }

    internal interface IArgument {
        bool IsValid(string[] args);
        int Set<T>(T data, string[] args);
    }

    internal sealed class PrimitiveArgument : IArgument {
        private readonly PropertyInfo _info;

        public PrimitiveArgument(PropertyInfo info) {
            _info = info ?? throw new ArgumentNullException(nameof(info));
        }

        public bool IsValid(string[] args) {
            var arg = args[0];
            var type = _info.PropertyType;
            if (type == typeof(int)) {
                return int.TryParse(arg, out _);
            }
            else if (type == typeof(uint)) {
                return uint.TryParse(arg, out _);
            }
            else if (type == typeof(byte)) {
                return byte.TryParse(arg, out _);
            }
            else if (type == typeof(sbyte)) {
                return sbyte.TryParse(arg, out _);
            }
            else if (type == typeof(long)) {
                return long.TryParse(arg, out _);
            }
            else if (type == typeof(ulong)) {
                return ulong.TryParse(arg, out _);
            }
            else if (type == typeof(float)) {
                return float.TryParse(arg, out _);
            }
            else if (type == typeof(double)) {
                return double.TryParse(arg, out _);
            }
            else if (type == typeof(bool)) {
                return bool.TryParse(arg, out _);
            }
            else if (type == typeof(char)) {
                return arg.Length == 1;
            }
            else if (type == typeof(string)) {
                return true;
            }
            else {
                throw new NotSupportedException($"{type} is not supported");
            }
        }

        public int Set<T>(T data, string[] args) {
            var arg = args[0];
            var type = _info.PropertyType;
            if (type == typeof(int)) {
                _info.SetValue(data, int.Parse(arg));
            }
            else if (type == typeof(uint)) {
                _info.SetValue(data, uint.Parse(arg));
            }
            else if (type == typeof(byte)) {
                _info.SetValue(data, byte.Parse(arg));
            }
            else if (type == typeof(sbyte)) {
                _info.SetValue(data, sbyte.Parse(arg));
            }
            else if (type == typeof(long)) {
                _info.SetValue(data, long.Parse(arg));
            }
            else if (type == typeof(ulong)) {
                _info.SetValue(data, ulong.Parse(arg));
            }
            else if (type == typeof(float)) {
                _info.SetValue(data, float.Parse(arg));
            }
            else if (type == typeof(double)) {
                _info.SetValue(data, double.Parse(arg));
            }
            else if (type == typeof(bool)) {
                _info.SetValue(data, bool.Parse(arg));
            }
            else if (type == typeof(char)) {
                _info.SetValue(data, arg[0]);
            }
            else if (type == typeof(string)) {
                _info.SetValue(data, arg);
            }
            else {
                throw new NotSupportedException($"{type} is not supported");
            }
            return 1;
        }

        public static bool IsAcceptable(PropertyInfo info) {
            return new[] {
                typeof(bool),
                typeof(byte), typeof(sbyte),
                typeof(int), typeof(uint),
                typeof(long), typeof(ulong),
                typeof(float), typeof(double),
                typeof(char), typeof(string),
            }
            .Contains(info.PropertyType);
        }
    }

    internal sealed class PrimitiveArrayArgument : IArgument {
        private readonly PropertyInfo _info;
        private readonly int _length;

        public PrimitiveArrayArgument(PropertyInfo info, int length) {
            _info = info ?? throw new ArgumentNullException(nameof(info));
            _length = length;
        }

        public bool IsValid(string[] args) {
            if (args.Length < _length) {
                return false;
            }
            var type = _info.PropertyType;
            var args_ = new string[_length];
            Array.Copy(args, args_, _length);
            if (type == typeof(int[])) {
                return args_.All(arg => int.TryParse(arg, out _));
            }
            else if (type == typeof(uint[])) {
                return args_.All(arg => uint.TryParse(arg, out _));
            }
            else if (type == typeof(byte[])) {
                return args_.All(arg => byte.TryParse(arg, out _));
            }
            else if (type == typeof(sbyte[])) {
                return args_.All(arg => sbyte.TryParse(arg, out _));
            }
            else if (type == typeof(long[])) {
                return args_.All(arg => long.TryParse(arg, out _));
            }
            else if (type == typeof(ulong[])) {
                return args_.All(arg => ulong.TryParse(arg, out _));
            }
            else if (type == typeof(float[])) {
                return args_.All(arg => float.TryParse(arg, out _));
            }
            else if (type == typeof(double[])) {
                return args_.All(arg => double.TryParse(arg, out _));
            }
            else if (type == typeof(bool[])) {
                return args_.All(arg => bool.TryParse(arg, out _));
            }
            else if (type == typeof(char[])) {
                return args_.All(arg => arg.Length == 1);
            }
            else if (type == typeof(string[])) {
                return true;
            }
            else {
                throw new NotSupportedException($"{type} is not supported");
            }
        }

        public int Set<T>(T data, string[] args) {
            var type = _info.PropertyType;
            var args_ = new string[_length];
            Array.Copy(args, args_, _length);
            if (type == typeof(int[])) {
                _info.SetValue(data, args_.Select(int.Parse).ToArray());
            }
            else if (type == typeof(uint[])) {
                _info.SetValue(data, args_.Select(uint.Parse).ToArray());
            }
            else if (type == typeof(byte[])) {
                _info.SetValue(data, args_.Select(byte.Parse).ToArray());
            }
            else if (type == typeof(sbyte[])) {
                _info.SetValue(data, args_.Select(sbyte.Parse).ToArray());
            }
            else if (type == typeof(long[])) {
                _info.SetValue(data, args_.Select(long.Parse).ToArray());
            }
            else if (type == typeof(ulong[])) {
                _info.SetValue(data, args_.Select(ulong.Parse).ToArray());
            }
            else if (type == typeof(float[])) {
                _info.SetValue(data, args_.Select(float.Parse).ToArray());
            }
            else if (type == typeof(double[])) {
                _info.SetValue(data, args_.Select(double.Parse).ToArray());
            }
            else if (type == typeof(bool[])) {
                _info.SetValue(data, args_.Select(bool.Parse).ToArray());
            }
            else if (type == typeof(char[])) {
                _info.SetValue(data, args_.Select(arg => arg[0]).ToArray());
            }
            else if (type == typeof(string[])) {
                _info.SetValue(data, args_);
            }
            else {
                throw new NotSupportedException($"{type} is not supported");
            }

            return _length;
        }

        public static bool IsAcceptable(PropertyInfo info) {
            return new[] {
                typeof(bool[]),
                typeof(byte[]), typeof(sbyte[]),
                typeof(int[]), typeof(uint[]),
                typeof(long[]), typeof(ulong[]),
                typeof(float[]), typeof(double[]),
                typeof(char[]), typeof(string[]),
            }
            .Contains(info.PropertyType);
        }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class PositionArgumentAttribute : Attribute {
        public PositionArgumentAttribute(int position) {
            Position = position;
        }

        public int Position { get; }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
    public sealed class ShortStyleArgumentAttribute : Attribute {
        public ShortStyleArgumentAttribute(string shortName) {
            if (!shortName.StartsWith("-")) {
                throw new ArgumentException("Short style option should start with '-'.");
            }
            ShortName = shortName;
        }

        public string ShortName { get; }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
    public sealed class LongStyleArgumentAttribute : Attribute {
        public LongStyleArgumentAttribute(string longName, int length = 1) {
            if (!longName.StartsWith("--")) {
                throw new ArgumentException("Long style option should start with '--'.");
            }
            LongName = longName;
            Length = length;
        }

        public string LongName { get; }
        public int Length { get; }
    }
}
