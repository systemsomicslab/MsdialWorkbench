using RDotNet;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Statistics;

/// <summary>
/// The in-process R that runs the Notame scripts through R.NET.
/// </summary>
/// <remarks>
/// R is single threaded and checks the C stack of the thread that started it, so every call goes
/// through one dedicated thread with a large stack, for the whole lifetime of the app. R can be
/// started only once per process, so the R folder of the first run is used until MS-DIAL restarts.
/// </remarks>
internal static class NotameRSession
{
    private const int StackSize = 64 * 1024 * 1024;
    private static readonly BlockingCollection<Action> _work = new BlockingCollection<Action>();
    private static readonly object _lock = new object();
    private static Thread? _thread;
    private static REngine? _engine;
    private static string? _rHome;

    /// <summary>Runs <paramref name="work"/> on the R thread, starting R from <paramref name="rHome"/> first if needed.</summary>
    public static Task<T> RunAsync<T>(string rHome, Func<REngine, T> work) {
        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        EnsureThread();
        _work.Add(() => {
            try {
                tcs.SetResult(work(GetEngine(rHome)));
            }
            catch (Exception ex) {
                tcs.SetException(ex);
            }
        });
        return tcs.Task;
    }

    public static Task RunAsync(string rHome, Action<REngine> work) {
        return RunAsync(rHome, engine => { work(engine); return true; });
    }

    private static void EnsureThread() {
        lock (_lock) {
            if (_thread is not null) {
                return;
            }
            _thread = new Thread(() => {
                foreach (var action in _work.GetConsumingEnumerable()) {
                    action();
                }
            }, StackSize) {
                IsBackground = true,
                Name = "R (Notame)",
            };
            _thread.SetApartmentState(ApartmentState.STA);
            _thread.Start();
        }
    }

    private static REngine GetEngine(string rHome) {
        var home = NormalizeRHome(rHome);
        if (_engine is not null) {
            if (!string.Equals(home, _rHome, StringComparison.OrdinalIgnoreCase)) {
                throw new InvalidOperationException($"R from {_rHome} is already running in this MS-DIAL session. Restart MS-DIAL to use R from {home}.");
            }
            return _engine;
        }
        var bin = Path.Combine(home, "bin", "x64");
        if (!File.Exists(Path.Combine(bin, "R.dll"))) {
            throw new DirectoryNotFoundException($"R.dll was not found in {bin}. Choose the folder R is installed in, e.g. C:\\Program Files\\R\\R-4.5.2.");
        }
        REngine.SetEnvironmentVariables(bin, home);
        var engine = REngine.GetInstance();
        engine.Evaluate($"Sys.setenv(PATH = paste({ToRString(bin)}, Sys.getenv('PATH'), sep = ';'))");
        // Rterm and Rgui run R in UTF-8 because their manifests ask for it; R inside MS-DIAL gets
        // the system code page instead (e.g. 932), where reading the UTF-8 files of packages
        // fails (citation() in notame::impute_rf: "invalid input found on input connection").
        engine.Evaluate("invisible(suppressWarnings(Sys.setlocale('LC_CTYPE', '.UTF-8')))");
        _engine = engine;
        _rHome = home;
        return engine;
    }

    private static string NormalizeRHome(string rHome) {
        if (string.IsNullOrWhiteSpace(rHome)) {
            throw new ArgumentException("The R folder is not set.", nameof(rHome));
        }
        return Path.GetFullPath(rHome).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    /// <summary>
    /// An R string literal for <paramref name="value"/>. Characters outside ASCII are written as
    /// \u escapes, so file paths in any language reach R unchanged whatever the code pages are.
    /// </summary>
    public static string ToRString(string value) {
        var builder = new StringBuilder(value.Length + 2).Append('"');
        for (int i = 0; i < value.Length; i++) {
            var c = value[i];
            switch (c) {
                case '\\': builder.Append(@"\\"); break;
                case '"': builder.Append("\\\""); break;
                case '\n': builder.Append(@"\n"); break;
                case '\r': builder.Append(@"\r"); break;
                case '\t': builder.Append(@"\t"); break;
                default:
                    // Fixed-width escapes: R.NET mangles the \u{...} form before R parses it.
                    if (char.IsHighSurrogate(c) && i + 1 < value.Length && char.IsLowSurrogate(value[i + 1])) {
                        builder.Append($"\\U{char.ConvertToUtf32(c, value[i + 1]):X8}");
                        i++;
                    }
                    else if (c < 0x20 || c > 0x7e) {
                        builder.Append($"\\u{(int)c:X4}");
                    }
                    else {
                        builder.Append(c);
                    }
                    break;
            }
        }
        return builder.Append('"').ToString();
    }

    /// <summary>Assigns a character value to a variable of R's global environment.</summary>
    public static void SetString(REngine engine, string name, string value) {
        engine.Evaluate($"{name} <- {ToRString(value)}");
    }

    /// <summary>Runs an R script file in R's global environment.</summary>
    public static void Source(REngine engine, string scriptPath) {
        engine.Evaluate($"source({ToRString(scriptPath)}, encoding = 'UTF-8', local = FALSE)");
    }

    /// <summary>The packages of <paramref name="packages"/> that R cannot load.</summary>
    public static string[] MissingPackages(REngine engine, IEnumerable<string> packages) {
        var vector = "c(" + string.Join(", ", packages.Select(ToRString)) + ")";
        return engine.Evaluate($"local({{ p <- {vector}; p[!vapply(p, requireNamespace, logical(1), quietly = TRUE)] }})")
            .AsCharacter()
            .ToArray();
    }
}
