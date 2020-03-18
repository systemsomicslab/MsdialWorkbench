using System;
using System.Globalization;

namespace NCDK.Common.Base
{
    internal static class Preconditions
    {
        public static int CheckNotNull(int? arg, string message)
        {
            if (!arg.HasValue)
                throw new NullReferenceException(message);
            return arg.Value;
        }

        public static T CheckNotNull<T>(T arg, string message)
        {
            if (arg == null)
                throw new NullReferenceException(message);
            return arg;
        }

        public static T CheckNotNull<T>(T arg)
        {
            if (arg == null)
                throw new ArgumentNullException(nameof(arg));
            return arg;
        }

        public static T CheckNotNull<T>(T arg, string paramName, string message)
        {
            if (arg == null)
                throw new ArgumentNullException(paramName, message);
            return arg;
        }

        public static void CheckArgument(bool condition, string message, params object[] parameters)
        {
            if (!condition)
            {
                if (!(parameters == null || parameters.Length == 0))
                {
                    message = string.Format(NumberFormatInfo.InvariantInfo, message, parameters);
                }
                throw new ArgumentException(message);
            }
        }
    }
}
