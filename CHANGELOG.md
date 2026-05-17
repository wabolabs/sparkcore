# Changelog

All notable changes to SparkOps Core are documented here.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).
This project uses [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [1.0.0] — 2026-05-17

Initial release of SparkOps Core — a self-hosted, fully open-source fork of
[Resgrid Core](https://github.com/Resgrid/Core) (Apache 2.0).

### Phase 0 — Brand

- Replaced user-visible "Resgrid" brand with "SparkOps Core" across all public
  and authenticated views, login/account pages, PWA web manifest, and
  Swagger/OpenAPI metadata.
- Apache 2.0 attribution preserved in all footers and copyright headers.

### Phase 1 — Database

- Switched default `DatabaseType` from `SqlServer` to `Postgres` in
  `DataConfig`, `OidcConfig`, and `WorkerConfig`.
- Cleared hardcoded SQL Server connection strings; values must now be supplied
  via `ResgridConfig.json` or environment variables.

### Phase 2 — PDF generation

- Removed commercial `NReco.PdfGenerator.LT` (wkhtmltopdf wrapper).
- Replaced with `PuppeteerSharp` v20 (headless Chromium, MIT-licensed).
- New `PuppeteerSharpProvider` implements `IPdfProvider`; `NRecoProvider` deleted.
- `PrintConfig` gains `ChromiumExecutablePath` in place of NReco licence keys.
- Worker Docker image installs Chromium via `apt` at `/usr/bin/chromium`.

### CI/CD and infrastructure

- GitHub Actions workflow rewritten:
  - Publishes Docker images to GitHub Container Registry (`ghcr.io/wabolabs/sparkcore/*`).
  - Version scheme changed from upstream `4.x.0` to `1.0.<run_number>`.
  - GitHub Release created automatically after successful image push.
- All Dockerfiles migrated from private `dhi.io` base images to official
  Microsoft Container Registry (`mcr.microsoft.com/dotnet/aspnet:9.0`,
  `mcr.microsoft.com/dotnet/runtime:9.0`, `mcr.microsoft.com/dotnet/sdk:9.0`).
- Dependabot enabled for NuGet packages, Docker base images, and GitHub Actions.
- Removed upstream-only workflows: `auto-approve`, `changerawr-sync`.
- Updated community health files (CONTRIBUTING, SUPPORT, config) to point to
  `wabolabs/sparkcore`.

---

*Based on Resgrid Core — copyright 2021 the Resgrid Core Authors and Resgrid, LLC.*
*Released under the Apache License 2.0.*
