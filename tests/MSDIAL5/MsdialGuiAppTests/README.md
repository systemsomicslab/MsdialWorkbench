# MsdialGuiAppTests

The `Window` category covers tests that show a real WPF window or dialog: it pops up (some of them on
screen, taking the focus) while the run goes on. Leave them out while you work on something else:

```powershell
dotnet test tests/MSDIAL5/MsdialGuiAppTests --filter "TestCategory!=Window"
```

Run them on their own:

```powershell
dotnet test tests/MSDIAL5/MsdialGuiAppTests --filter "TestCategory=Window"
```

Run all tests without a filter before submitting changes, or when changing the views these tests cover.
Pull request and master CI runs include them in the main test step. Give a test that shows a window
`[TestCategory("Window")]`.
