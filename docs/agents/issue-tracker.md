# Issue tracker: GitHub

Use GitHub Issues for lightweight tracking. This repo does not use a formal triage workflow.

## Conventions

- **Create an issue**: `gh issue create --title "..." --body "..."`
- **Read an issue**: `gh issue view <number> --comments`
- **List issues**: `gh issue list --state open`
- **Comment on an issue**: `gh issue comment <number> --body "..."`
- **Close an issue**: `gh issue close <number> --comment "..."`

Use `git remote -v` when you need to confirm the repository target; `gh` will infer it from the clone.

## Pull requests

Treat PRs separately from issues. Do not pull external PRs into an issue triage queue.

- **Read a PR**: `gh pr view <number> --comments`
- **View the diff**: `gh pr diff <number>`
- **Comment on a PR**: `gh pr comment <number> --body "..."`
- **Close a PR**: `gh pr close <number>`

## When a skill says "publish to the issue tracker"

GitHub Issues are where users report errors. Do not post specs, PRDs or plans there.

Write them as a local Markdown file under `docs/specs/` instead, named after the change (e.g. `docs/specs/peak-spot-table-refresh-waits-for-edit.md`). `docs/specs/` is for local working notes and is ignored by `.gitignore`; never force-add it. Skip any triage label the skill asks for. When the work becomes a PR, carry what the reader needs into the PR description.

Create a GitHub issue only when the user asks for one.

## When a skill says "fetch the relevant ticket"

Run `gh issue view <number> --comments`.
