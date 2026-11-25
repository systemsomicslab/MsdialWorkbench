using System.Security.Cryptography;
using System.Text;

namespace CompMs.Common.StructureFinder.DiagnosticRules.Json
{
    /// <summary>Lowercase hexadecimal SHA-256.</summary>
    /// <remarks>
    /// Under .NET Framework the CNG/CAPI provider is used, because the managed SHA256Managed refuses to run under a
    /// FIPS policy; elsewhere <see cref="SHA256.Create()"/>.
    /// </remarks>
    public static class Sha256Hex
    {
        public static string Of(byte[] data) {
#if NETFRAMEWORK
            using (var sha = new SHA256CryptoServiceProvider()) {
                return ToHex(sha.ComputeHash(data));
            }
#else
            using (var sha = SHA256.Create()) {
                return ToHex(sha.ComputeHash(data));
            }
#endif
        }

        /// <summary>The SHA-256 of a value's canonical JSON bytes.</summary>
        public static string OfCanonical(JsonValue value) => Of(CanonicalJson.ToBytes(value));

        private static string ToHex(byte[] hash) {
            const string hex = "0123456789abcdef";
            var sb = new StringBuilder(hash.Length * 2);
            foreach (var b in hash) {
                sb.Append(hex[b >> 4]);
                sb.Append(hex[b & 0xF]);
            }
            return sb.ToString();
        }
    }
}
