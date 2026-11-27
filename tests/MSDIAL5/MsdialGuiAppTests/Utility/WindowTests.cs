using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace CompMs.App.Msdial.Utility.Tests;

/// <summary>
/// Tests that show a real WPF window or dialog, which pops up (and may take the focus) while the run goes on.
/// They run only when asked: tag them <c>[TestCategory(WindowTests.Category)]</c> and call <see cref="RequireOptIn"/>
/// before the window is made, and they report Inconclusive unless <c>MSDIAL_RUN_WINDOW_TESTS=1</c>.
/// CI sets the variable, so they still run there. To run them locally (PowerShell):
/// <code>
/// $env:MSDIAL_RUN_WINDOW_TESTS = "1"; dotnet test tests/MSDIAL5/MsdialGuiAppTests --filter "TestCategory=Window"
/// </code>
/// </summary>
internal static class WindowTests
{
    public const string Category = "Window";
    public const string Variable = "MSDIAL_RUN_WINDOW_TESTS";

    public static void RequireOptIn() {
        if (Environment.GetEnvironmentVariable(Variable) != "1") {
            Assert.Inconclusive($"This test shows a window. Set {Variable}=1 to run it (see Utility/WindowTests.cs).");
        }
    }
}
