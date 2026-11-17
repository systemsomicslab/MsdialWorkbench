# MsdialCoreTestAppTests

Run the faster tests during development:

```powershell
dotnet test tests/MSDIAL5/MsdialCoreTestAppTests --filter "TestCategory!=Integration"
```

Run the integration tests separately:

```powershell
dotnet test tests/MSDIAL5/MsdialCoreTestAppTests --filter "TestCategory=Integration"
```

The `Integration` category covers tests that generate mzML files and run LC-MS processing across
multiple components, including peak detection, automatic retention-time correction, alignment,
and annotation. Run all tests without a filter before submitting changes, or when changing these
workflows:

```powershell
dotnet test tests/MSDIAL5/MsdialCoreTestAppTests
```

Pull request and master CI runs include a dedicated `Test LC-MS integration` step. The main test
step runs the rest of the solution's tests. Add `--configuration "Release vendor unsupported"`
if using the vendor-unsupported build configuration.
