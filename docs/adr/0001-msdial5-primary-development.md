# ADR 0001: Treat MSDIAL5 as the primary development target

## Status

Accepted

## Context

This repository contains multiple product areas under `src/`, including `Common`, `MSDIAL4`, `MSDIAL5`, `CompMs.App`, `MSFINDER`, and `RawDataApp`.

The codebase is not cleanly separated by ownership, and `Common` contains shared foundation code that is used by both MSDIAL4 and MSDIAL5. In practice, current development work is focused on MSDIAL5 unless a task explicitly says otherwise.

Without an explicit default, engineers and agents can waste time deciding which code path to follow first, or accidentally optimize for the wrong product line.

## Decision

Treat `src/MSDIAL5/` as the primary development target for this repository. Treat `src/Common/` as the shared foundation that MSDIAL5 depends on and read it alongside MSDIAL5 for default work.

When a task does not specify a different scope:

- Prefer MSDIAL5 implementations and call sites.
- Read and update Common when the change touches shared behavior, shared abstractions, or code reused by MSDIAL5.
- Only widen scope to MSDIAL4, CompMs.App, MSFINDER, or RawDataApp when the task explicitly requires it or the change crosses boundaries.

## Consequences

- Default work is faster because the first place to look is unambiguous.
- Shared changes still need to be checked carefully in Common because that code affects more than one product line.
- MSDIAL4 remains relevant for compatibility checks, but it is not the default target for new work.

## Notes

This ADR records the working default established for this repository. If the product strategy changes, add a new ADR that supersedes this one.
