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

## [Unreleased] — Phase 3: Payment removal

- Removed `Stripe.net` NuGet package from all projects (Resgrid.Model, Resgrid.Services, Resgrid.Web, Resgrid.Web.Services).
- Deleted all Stripe facade interfaces and implementations (`IStripeChargeServiceFacade`, `IStripeInvoiceServiceFacade`, `IStripeSubscriptionServiceFacade` and their implementations).
- Deleted `IPaymentProviderService` and `PaymentProviderService` (Stripe webhook processing service).
- `LimitsService` rewritten: all limit checks return `true`; all limit counts return `int.MaxValue`. No plan or billing API calls.
- `SubscriptionsService` rewritten: always returns the free plan from the local database; all Stripe/Paddle methods are no-op stubs returning safe defaults. No external billing API calls.
- `PaymentQueueLogic`: all payment event cases replaced with a log-and-discard no-op.
- `StripeController` (API webhook endpoint): replaced with 200 OK stub to avoid Stripe delivery errors on misconfigured instances.
- Removed `StripeConfiguration.ApiKey` initialization from `Web` and `Web.Services` startup.
- Stubbed `UpdateBillingInfo`, `ValidateCoupon`, and the Stripe cancel path in `SubscriptionController`; removed Stripe charge data from `ViewInvoiceView` and the invoice Razor view.
- Removed all stray `using Stripe;` / `using Stripe.Checkout;` directives across the codebase.
- Added `System.Configuration.ConfigurationManager` package to `Resgrid.Web.Eventing` (pre-existing missing dependency exposed by the Stripe removal).

---

*Based on Resgrid Core — copyright 2021 the Resgrid Core Authors and Resgrid, LLC.*
*Released under the Apache License 2.0.*
