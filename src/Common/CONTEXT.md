# Common

Shared foundation code used across the repository, including code consumed by MSDIAL4, MSDIAL5, and other applications.

## What lives here

- Shared utilities, abstractions, and base infrastructure.
- Code that is reused by both MSDIAL4 and MSDIAL5, even when it has not been fully separated yet.

## How to work here

- Treat this as the shared base layer when a change affects multiple product lines.
- If you are changing behavior used by MSDIAL5, check the MSDIAL5 context as well.
- If you are changing behavior used by MSDIAL4, check the MSDIAL4 context as well.

## Vocabulary

- Prefer the terms already used by the code in this area.
- If a shared concept needs a precise definition, record it in the most relevant product context first, then reference it here if it is truly cross-cutting.
