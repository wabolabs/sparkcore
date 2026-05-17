# SparkOps Core

**Self-hosted computer-aided dispatch (CAD), resource management, and logistics for emergency services.**

SparkOps Core is a community-maintained fork of [Resgrid Core](https://github.com/Resgrid/Core) (Apache 2.0), hardened for fully self-hosted, open-source deployments. All cloud-service dependencies are replaced with open-source alternatives; no vendor account or paid API key is required to run a production instance.

[![CI/CD](https://github.com/wabolabs/sparkcore/actions/workflows/dotnet.yml/badge.svg)](https://github.com/wabolabs/sparkcore/actions/workflows/dotnet.yml)
[![License](https://img.shields.io/github/license/wabolabs/sparkcore.svg)](LICENSE)

---

## Table of Contents

- [Features](#features)
- [Container images](#container-images)
- [Quick start](#quick-start)
- [Configuration](#configuration)
- [Building from source](#building-from-source)
- [Architecture](#architecture)
- [Contributing](#contributing)
- [Attribution and license](#attribution-and-license)

---

## Features

- **Personnel management** — contacts, certifications, roles, status, and availability
- **Unit support** — apparatus and team tracking with AVL, accountability, and logging
- **Groups and locations** — hierarchical organization for large or dispersed agencies
- **Computer-aided dispatch** — manual and automatic call creation and unit dispatch
- **Messaging and chat** — P2P, group, and command channels
- **Duty shift system** — assigned and signup shifts with swap/trade and attendance validation
- **Learning management** — training materials, documents, video links, and quizzes
- **Run logs and reporting** — call, training, and meeting reports; CSV export
- **Calendar** — RSVP events and activity scheduling
- **Inventory management** — perishable and durable inventory at apparatus, personnel, and location level
- **Document storage** — department- and group-scoped document hosting
- **Notification service** — alerts for staffing, availability, and status changes
- **Department linking** — mutual-aid and multi-agency cooperation
- **REST API** — full API for integration and mobile app support

---

## Container images

Images are published to the GitHub Container Registry on every push to `master`:

| Service | Image |
|---|---|
| Web (main UI) | `ghcr.io/wabolabs/sparkcore/web:latest` |
| API | `ghcr.io/wabolabs/sparkcore/api:latest` |
| Eventing | `ghcr.io/wabolabs/sparkcore/eventing:latest` |
| MCP | `ghcr.io/wabolabs/sparkcore/mcp:latest` |
| TTS | `ghcr.io/wabolabs/sparkcore/tts:latest` |
| Worker | `ghcr.io/wabolabs/sparkcore/worker:latest` |

Versioned tags follow `1.0.<run_number>` semver. `latest` always points to the most recent successful build.

---

## Quick start

> Prerequisites: Docker Compose v2, PostgreSQL 15+, RabbitMQ 3.12+, Redis 7+.

1. Pull the compose file and create a config:

   ```bash
   curl -O https://raw.githubusercontent.com/wabolabs/sparkcore/master/Docker/docker-compose.yml
   cp ResgridConfig.example.json ResgridConfig.json
   # edit ResgridConfig.json — set database, Redis, and RabbitMQ connection strings
   ```

2. Start the stack:

   ```bash
   docker compose up -d
   ```

3. Open `http://localhost` in your browser.

### PDF generation (worker image)

The worker container has Chromium installed at `/usr/bin/chromium`. Set the config key so PuppeteerSharp can find it:

```json
{ "PrintConfig.ChromiumExecutablePath": "/usr/bin/chromium" }
```

Or pass it as an environment variable using the `RESGRID:PrintConfig:ChromiumExecutablePath` key.

---

## Configuration

Configuration is loaded from `ResgridConfig.json` (placed next to the executable) or environment variables.

- **JSON key format:** `"ClassName.FieldName": "value"` — e.g., `"DataConfig.ConnectionString": "Host=..."`
- **Environment variable format:** `RESGRID:ClassName:FieldName` — e.g., `RESGRID:DataConfig:ConnectionString`

Key config classes:

| Class | Purpose |
|---|---|
| `DataConfig` | PostgreSQL connection strings |
| `CacheConfig` | Redis connection string |
| `ServiceBusConfig` | RabbitMQ connection string |
| `SystemBehaviorConfig` | Environment name, feature flags |
| `EmailConfig` | SMTP settings |
| `PrintConfig` | Chromium path for PDF generation |

---

## Building from source

Requires [.NET 9 SDK](https://dotnet.microsoft.com/download).

```bash
git clone https://github.com/wabolabs/sparkcore.git
cd sparkcore
dotnet restore
dotnet build Resgrid.sln
dotnet test --configuration Release
```

Build Docker images locally:

```bash
docker build -f Web/Resgrid.Web/Dockerfile -t sparkcore/web .
docker build -f Workers/Resgrid.Workers.Console/Dockerfile -t sparkcore/worker .
```

---

## Architecture

SparkOps Core is a .NET 9 monolith split across 30+ projects in a layered architecture:

```
Config → Model → Services → Repositories / Providers → Web / Workers
```

- **Config** (`Core/Resgrid.Config/`) — static config classes, no external dependencies
- **Model** (`Core/Resgrid.Model/`) — entities, enums, service/repository interfaces
- **Services** (`Core/Resgrid.Services/`) — business logic
- **Repositories** (`Repositories/`) — PostgreSQL via Dapper
- **Providers** (`Providers/`) — Redis, RabbitMQ, email, PDF, geolocation, …
- **Web** (`Web/`) — ASP.NET Core MVC + REST API + supporting endpoints
- **Workers** (`Workers/`) — background job processing

Dependency injection uses [Autofac](https://autofac.org/) with module-based registration. See [`CLAUDE.md`](CLAUDE.md) for a detailed developer reference.

---

## Contributing

See [`.github/CONTRIBUTING.md`](.github/CONTRIBUTING.md).

---

## Attribution and license

SparkOps Core is based on **[Resgrid Core](https://github.com/Resgrid/Core)**, copyright 2021 the Resgrid Core Authors and [Resgrid, LLC](https://resgrid.com). Used under the [Apache License 2.0](LICENSE).

All modifications in this repository are copyright their respective contributors and are also released under the [Apache License 2.0](LICENSE).
