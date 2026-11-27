# MsdialGuiAppTests

The `Window` category covers tests that show a real WPF window or dialog: it pops up (some of them on
screen, taking the focus) while the run goes on. A plain `dotnet test` leaves them out; the default
filter is set in the root `Directory.Build.props`.

Run them on their own:

```powershell
dotnet test tests/MSDIAL5/MsdialGuiAppTests --filter "TestCategory=Window"
```

Run everything, the Window tests included:

```powershell
dotnet test tests/MSDIAL5/MsdialGuiAppTests -p:RunAllTests=true
```

Any `--filter` replaces the default one, so CI, which gives its own filter, runs the Window tests in the
main test step. Run them before submitting changes to the views they cover. Give a test that shows a
window `[TestCategory("Window")]`.
