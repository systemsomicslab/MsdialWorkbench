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

Create a GitHub issue.

## When a skill says "fetch the relevant ticket"

Run `gh issue view <number> --comments`.
