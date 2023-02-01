using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.Common.Utility {
    public sealed class ByteConvertion {
        private ByteConvertion() { }
        public static int ToByteCount<T>(T obj) {
            var type = typeof(T);
            if (type == typeof(int)) return 4;
            else if (type == typeof(float)) return 4;
            else if (type == typeof(double)) return 8;
            else if (type == typeof(bool)) return 1;
            else if (type == typeof(long)) return 8;
            else if (type == typeof(short)) return 2;
            else if (type == typeof(sbyte)) return 1;
            else if (type == typeof(byte)) return 1;
            else if (type == typeof(ushort)) return 2;
            else if (type == typeof(uint)) return 4;
            else if (type == typeof(ulong)) return 8;
            else if (type == typeof(char)) return 2;
            else if (type == typeof(decimal)) return 16;
            else if (type == typeof(string)) {
                return GetStringByteSize((string)(object)obj);
            }
            return -1; // error
        }

        public static int GetStringByteSize(string obj) {
            return obj.Length * 2;
        }
    }
}
