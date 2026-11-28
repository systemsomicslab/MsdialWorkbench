using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Interop;

namespace CompMs.App.Msdial.View.Chart;

/// <summary>
/// Reads and writes the Windows clipboard directly, waiting while another program has it open.
/// </summary>
/// <remarks>
/// Many programs open the clipboard every time it changes: Excel, the Windows clipboard history, mouse and keyboard utilities, remote desktop.
/// WPF's Clipboard gives up on a write after about a second, a read fails at once if it meets one of those moments,
/// and DataGrid and TextBox drop a failed copy without a word, so the previous contents are what gets pasted.
/// Worse, a WPF copy first offers its data to be rendered on request. The clipboard history asks for it at once and keeps the clipboard open
/// until this thread answers, but this thread sleeps in WPF's retry loop waiting for that same clipboard: the copy fails after a second,
/// and the clipboard stays locked for about ten seconds, until the history service gives up.
/// Here the data is put on the clipboard as it is, so nobody waits on this thread, every access tries again every few milliseconds until the timeout,
/// the waits answer render requests left by other copies, a write is checked by reading the text back, and the caller learns when the clipboard could not be used.
/// </remarks>
internal static class WindowsClipboard
{
    /// <summary>How long a copy or paste waits for the clipboard before it gives up and says so.</summary>
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(2);

    private const uint CF_UNICODETEXT = 13;
    private const uint GMEM_MOVEABLE = 0x0002;
    private const uint PM_NOREMOVE = 0x0000;
    private const uint PM_QS_SENDMESSAGE = 0x00400000;
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(5);
    private static readonly uint HtmlFormat = RegisterClipboardFormat("HTML Format");
    private static readonly uint CsvFormat = RegisterClipboardFormat("Csv");

    // The clipboard needs an owner window for SetClipboardData. A message-only window serves, so no visible window is needed.
    [ThreadStatic] private static HwndSource? _owner;

    /// <summary>
    /// What a copy puts on the clipboard: tab-separated text, and the HTML and CSV that DataGrid also writes (null to leave them out).
    /// </summary>
    public sealed class Content(string text, string? html = null, string? csv = null)
    {
        public string Text { get; } = text;
        public string? Html { get; } = html;
        public string? Csv { get; } = csv;
    }

    /// <summary>
    /// Replaces the clipboard contents and checks that the text reads back. False when the clipboard stayed busy until the timeout; the previous contents may still be there.
    /// </summary>
    public static bool TryWrite(Content content, TimeSpan timeout) {
        var elapsed = Stopwatch.StartNew();
        var owner = Owner();
        while (true) {
            if (OpenClipboard(owner)) {
                bool written;
                try {
                    written = EmptyClipboard()
                        && Set(CF_UNICODETEXT, Encoding.Unicode, content.Text)
                        && (content.Html is null || Set(HtmlFormat, Encoding.UTF8, content.Html))
                        && (content.Csv is null || Set(CsvFormat, Encoding.Default, content.Csv));
                }
                finally {
                    CloseClipboard();
                }
                if (written && TryReadText(Remaining(timeout, elapsed), out var back) && back == content.Text) {
                    return true;
                }
            }
            if (elapsed.Elapsed >= timeout) {
                return false;
            }
            Wait();
        }
    }

    /// <summary>
    /// Reads the text on the clipboard: null when it holds no text. False when the clipboard stayed busy until the timeout.
    /// </summary>
    public static bool TryReadText(TimeSpan timeout, out string? text) {
        text = null;
        var elapsed = Stopwatch.StartNew();
        while (!OpenClipboard(IntPtr.Zero)) {
            if (elapsed.Elapsed >= timeout) {
                return false;
            }
            Wait();
        }
        try {
            var handle = GetClipboardData(CF_UNICODETEXT);
            if (handle == IntPtr.Zero) {
                return true;
            }
            var pointer = GlobalLock(handle);
            if (pointer == IntPtr.Zero) {
                return true;
            }
            try {
                var chars = (int)((ulong)GlobalSize(handle) / 2);
                var raw = Marshal.PtrToStringUni(pointer, chars);
                var end = raw.IndexOf('\0');
                text = end >= 0 ? raw.Substring(0, end) : raw;
                return true;
            }
            finally {
                GlobalUnlock(handle);
            }
        }
        finally {
            CloseClipboard();
        }
    }

    // Waits a moment before the next try, letting through the messages other programs send to this thread's windows meanwhile.
    // A program that opened the clipboard may be asking this thread to render what an earlier WPF copy left there to be rendered on request
    // (WPF and OLE copies do that), and it keeps the clipboard open until it gets an answer. Posted messages stay queued, so nothing else runs here.
    private static void Wait() {
        PeekMessage(out _, IntPtr.Zero, 0, 0, PM_NOREMOVE | PM_QS_SENDMESSAGE);
        Thread.Sleep(PollInterval);
    }

    private static TimeSpan Remaining(TimeSpan timeout, Stopwatch elapsed) {
        var remaining = timeout - elapsed.Elapsed;
        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }

    private static IntPtr Owner() {
        _owner ??= new HwndSource(new HwndSourceParameters("MS-DIAL clipboard") { ParentWindow = new IntPtr(-3), WindowStyle = 0, });
        return _owner.Handle;
    }

    // Puts one format on the open clipboard. The clipboard owns the memory once SetClipboardData succeeds.
    private static bool Set(uint format, Encoding encoding, string value) {
        var bytes = encoding.GetBytes(value + "\0");
        var handle = GlobalAlloc(GMEM_MOVEABLE, (UIntPtr)bytes.Length);
        if (handle == IntPtr.Zero) {
            return false;
        }
        var pointer = GlobalLock(handle);
        if (pointer == IntPtr.Zero) {
            GlobalFree(handle);
            return false;
        }
        try {
            Marshal.Copy(bytes, 0, pointer, bytes.Length);
        }
        finally {
            GlobalUnlock(handle);
        }
        if (SetClipboardData(format, handle) == IntPtr.Zero) {
            GlobalFree(handle);
            return false;
        }
        return true;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Message
    {
        public IntPtr Window;
        public uint Id;
        public IntPtr WParam;
        public IntPtr LParam;
        public uint Time;
        public int X;
        public int Y;
    }

    [DllImport("user32.dll")] private static extern bool PeekMessage(out Message message, IntPtr window, uint first, uint last, uint remove);
    [DllImport("user32.dll", SetLastError = true)] private static extern bool OpenClipboard(IntPtr owner);
    [DllImport("user32.dll", SetLastError = true)] private static extern bool CloseClipboard();
    [DllImport("user32.dll", SetLastError = true)] private static extern bool EmptyClipboard();
    [DllImport("user32.dll", SetLastError = true)] private static extern IntPtr SetClipboardData(uint format, IntPtr memory);
    [DllImport("user32.dll", SetLastError = true)] private static extern IntPtr GetClipboardData(uint format);
    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)] private static extern uint RegisterClipboardFormat(string name);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern IntPtr GlobalAlloc(uint flags, UIntPtr bytes);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern IntPtr GlobalLock(IntPtr memory);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool GlobalUnlock(IntPtr memory);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern IntPtr GlobalFree(IntPtr memory);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern UIntPtr GlobalSize(IntPtr memory);
}
