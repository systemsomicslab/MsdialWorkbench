# CONTEXT MAP

This repository is split into several code contexts. Read the context file for the area you are working in, then read any ADRs that apply.

## Active development context

- **MSDIAL5** — the primary development target. Unless told otherwise, work in `src/MSDIAL5/` and the shared foundation in `src/Common/`.

## Repository contexts

| Context | Scope | Read this |
| --- | --- | --- |
| Common | Shared foundation code used by MSDIAL4, MSDIAL5, and other apps, even though it is not cleanly separated today. | `src/Common/CONTEXT.md` |
| MSDIAL4 | MSDIAL4-era application and supporting libraries. | `src/MSDIAL4/CONTEXT.md` |
| MSDIAL5 | Current mainline MSDIAL development. Prefer this unless told otherwise. | `src/MSDIAL5/CONTEXT.md` |
| CompMs.App | Non-MSDIAL applications and tools under the CompMs.App umbrella. | `src/CompMs.App/CONTEXT.md` |
| MSFINDER | MSFINDER application and supporting libraries. | `src/MSFINDER/CONTEXT.md` |
| RawDataApp | Raw data conversion and viewer applications. | `src/RawDataApp/CONTEXT.md` |

## Reading rules

- If you are working in **MSDIAL5**, also read **Common** because it contains the shared base code that MSDIAL5 builds on.
- If your change touches code that is shared across MSDIAL4 and MSDIAL5, treat **Common** as the first stop and check both contexts for call sites.
- If your task is about one of the other application areas, prefer that context's `CONTEXT.md` first and only widen the scope if the change crosses boundaries.
