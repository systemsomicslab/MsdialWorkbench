# ADR 0004: Use HttpClient for legacy HTTP APIs

## Status

Accepted

## Context

`src/Common/CommonStandard/` emitted `SYSLIB0014` warnings for `WebClient`,
`WebRequest.Create`, and `HttpWebRequest`. Microsoft recommends
`System.Net.Http.HttpClient` for new code and migration of these legacy HTTP
APIs.

The affected code is used by PubChem structure searches and Classyfire
requests. The existing public methods are synchronous, and the project targets
`netstandard2.0`, `netstandard2.1`, `net472`, `net48`, and `net8.0`.

## Decision

Replace the legacy HTTP APIs in CommonStandard with shared static
`HttpClient` instances:

- Replace `WebClient.DownloadFile` with `GetAsync` and streamed response
  content copied to a file.
- Replace `WebClient.UploadString` with `PostAsync` and `StringContent`.
- Replace `WebRequest.Create(...).GetResponse()` and `HttpWebRequest` GETs with
  `GetAsync` and `HttpResponseMessage`.
- Preserve the existing synchronous public method signatures for now by using
  synchronous bridges at the HTTP boundary.
- Reference `System.Net.Http` explicitly so all CommonStandard target
  frameworks compile.

The `HttpClient` instances are shared rather than created per request. The
existing `KeepAlive`, HTTP/1.0, and infinite-timeout settings are not carried
over mechanically; any required connection or timeout policy should be
addressed separately when runtime behavior is validated.

`NU1902` and `NU1903` remain temporary command-line-only build suppressions;
they are not added to project configuration.

## Consequences

- CommonStandard no longer emits `SYSLIB0014` for the migrated call sites.
- The migration applies consistently across all supported target frameworks.
- Synchronous callers remain source-compatible, but HTTP calls still block the
  calling thread.
- Runtime smoke tests are required for PubChem SDF downloads and Classyfire
  requests because compilation does not validate remote-service behavior.
- A future async API redesign may remove the synchronous bridges.

## References

- [SYSLIB0014 warning](https://learn.microsoft.com/en-us/dotnet/fundamentals/syslib-diagnostics/syslib0014)
- [Migrate from HttpWebRequest to HttpClient](https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/http/httpclient-migrate-from-httpwebrequest)
- [HttpClient guidelines](https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/http/httpclient-guidelines)
- [HttpClient API](https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httpclient)
