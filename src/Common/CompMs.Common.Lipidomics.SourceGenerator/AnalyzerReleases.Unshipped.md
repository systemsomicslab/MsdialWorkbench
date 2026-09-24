; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
LSG001 | LipidSpectrumModel | Warning | An ion whose m/z or intensity cannot be generated is skipped
LSG002 | LipidSpectrumModel | Warning | An unrecognized LSILevel/diagnostic value falls back to a default
LSG003 | LipidSpectrumModel | Warning | An unknown ion list under LipidMS is ignored
