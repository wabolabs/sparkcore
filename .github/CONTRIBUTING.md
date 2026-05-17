# Contributing to SparkOps Core

Thank you for looking at contributing to SparkOps Core — a self-hosted emergency-services CAD platform built on top of [Resgrid](https://github.com/Resgrid/Core) (Apache 2.0).

Please take a moment to review this document to make the contribution process smooth and effective for everyone.

## Using the issue tracker

The [issue tracker](https://github.com/wabolabs/sparkcore/issues) is the preferred channel for bug reports, feature requests, and pull requests. Please respect these guidelines:

- **Do not** use the issue tracker for deployment support for your own on-premise instance.
- **Do not** derail or troll issues. Keep discussion on topic and respect others.
- **Do not** post comments consisting solely of "+1". Use GitHub reactions instead.

## Issues and labels

| Label | Meaning |
|---|---|
| `bug` | A confirmed bug in the codebase |
| `feature` | A new feature request |
| `docs` | Documentation improvement |
| `build` | CI/CD or build-system issue |
| `help wanted` | Community help needed |

## Bug reports

A good bug report is specific and reproducible. Guidelines:

1. **Search first** — check whether the issue has already been reported.
2. **Use the latest `master`** — confirm the bug exists on the current version.
3. **Isolate the problem** — describe exact reproduction steps and attach logs or config snippets (redact secrets).

Do not share deployment URLs or login credentials in issue reports.

## Feature requests

Feature requests are welcome. Briefly explain the problem it solves and how it fits the scope of a self-hosted CAD platform. Features that affect the upstream Resgrid data model should be raised upstream first.

## Pull requests

1. [Fork](https://help.github.com/articles/fork-a-repo/) the repo and create a topic branch:

   ```bash
   git clone https://github.com/<your-username>/sparkcore.git
   cd sparkcore
   git checkout -b my-feature
   ```

2. Make focused, logical commits. Follow conventional commit style where possible.

3. Ensure the solution builds and tests pass:

   ```bash
   dotnet build Resgrid.sln
   dotnet test --configuration Release
   ```

4. Push and open a PR against `master`.

**By submitting a patch you agree** to license your contribution under the [Apache License 2.0](../LICENSE).

## Code guidelines

- Target **net9.0** for all projects.
- Match the existing Autofac / Service-Locator DI style described in [CLAUDE.md](../CLAUDE.md).
- No comments unless the *why* is non-obvious. No multi-line docstrings.
- Default database target is **PostgreSQL**.

## License

Apache License v2.0 — see [LICENSE](../LICENSE).
