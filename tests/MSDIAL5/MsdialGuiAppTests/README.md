# MsdialGuiAppTests

The `UI` category covers tests that show a real WPF window or dialog: it pops up (some of them on
screen, taking the focus) while the run goes on. A plain `dotnet test` leaves them out; the default
filter is set in the root `Directory.Build.props`. The same category marks the tests of
`MsdialGuiAppUiTests`, which drive a built `MSDIAL.exe`.

Run them on their own:

```powershell
dotnet test tests/MSDIAL5/MsdialGuiAppTests --filter "TestCategory=UI"
```

Run everything, the UI tests included:

```powershell
dotnet test tests/MSDIAL5/MsdialGuiAppTests -p:RunAllTests=true
```

Any `--filter` replaces the default one, so CI, which gives its own filter, runs the UI tests in the
main test step. Run them before submitting changes to the views they cover. Give a test that shows a
window `[TestCategory("UI")]`.

The default filter applies to `dotnet test` only. Visual Studio's Test Explorer does not read it, so it
lists and runs the UI tests with the rest (the category shows there as a trait).
