using RDotNet;
using System.IO;
using System.Reflection;

namespace CompMs.App.Msdial.Utility;

internal sealed class RRunner
{
    private readonly string _script;

    public RRunner(string script) {
        _script = script;
    }

    public void Run(REngine engine) {
        engine.Evaluate(_script);
    }

    public static RRunner LoadFromResource(string resource) {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(resource);
        using var reader = new StreamReader(stream);
        return new RRunner(reader.ReadToEnd());
    }
}
