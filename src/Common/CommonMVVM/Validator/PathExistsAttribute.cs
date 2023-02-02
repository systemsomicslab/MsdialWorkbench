using System.ComponentModel.DataAnnotations;
using System.IO;

namespace CompMs.CommonMVVM.Validator
{
    public class PathExistsAttribute : ValidationAttribute
    {
        public bool IsFile { get; set; } = false;
        public bool IsDirectory { get; set; } = false;

        public override bool IsValid(object value) {
            var path = value as string;
            return !string.IsNullOrEmpty(path) && (IsFile && File.Exists(path) || IsDirectory && Directory.Exists(path));
        }
    }
}
