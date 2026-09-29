# Upstream issue drafts

Not filed yet. Each draft is written to be pasted straight into a GitHub
issue against [Resgrid/Core](https://github.com/Resgrid/Core). Filed so far:
[#536](https://github.com/Resgrid/Core/issues/536) (MSSQL migration,
`ConstraintExists`).

Keep this file to *proposed* text. Once an issue is filed, record the number
here and move the discussion to the issue itself.

---

## Draft 1 — SaveCallNote ignores a caller-supplied timestamp

**Title:** `v4 SaveCallNote ignores any client-supplied timestamp`

**Labels:** `bug`, `api`

### Summary

`POST /api/v4/CallNotes/SaveCallNote` always stamps the note with the
server's current UTC time. There is no way for a client that knows when a
note was actually taken — a CAD chronology being ingested after the fact —
to have that time stored.

### Evidence

`Web/Resgrid.Web.Services/Controllers/v4/CallNotesController.cs`:

```csharp
var note = new CallNote();
note.CallId = int.Parse(input.CallId);
note.Timestamp = DateTime.UtcNow;      // fixed at save time
note.Note = input.Note;
```

`Models/v4/CallNotes/SaveCallNoteInput.cs` has no timestamp property, so
there is nowhere for a caller's value to arrive even if the controller
wanted it.

Verified against a live `4.862.0` instance on 2026-09-28. Posting the same
note with each of `Timestamp`, `TimestampUtc`, `TimestampLocal`,
`DateTimeUtc` and `CreatedOn` — five plausible field names — all stored the
server's `now()`:

| Field sent | Value sent | Stored |
|---|---|---|
| `Timestamp` | `2026-09-28T16:44:00+00:00` | `2026-09-28 20:41:44` |
| `TimestampUtc` | `2026-09-28T16:44:00+00:00` | `2026-09-28 20:41:54` |
| `TimestampLocal` | `2026-09-28T12:44:00` | `2026-09-28 20:41:54` |
| `DateTimeUtc` | `2026-09-28T16:44:00Z` | `2026-09-28 20:41:54` |
| `CreatedOn` | `2026-09-28T16:44:00Z` | `2026-09-28 20:41:54` |

### Impact

A call's notes are its chronology. When every note is stamped at publish
time, a backfill of a day of history puts "dispatched", "en route", "on
scene" and "cleared" all at the same instant, and the order the department
reads on the run card is lost. This is not cosmetic for anyone importing
historical CAD data: `callnotes.Timestamp` becomes meaningless as an
ordering key, and clients are pushed into embedding the real time in the
note text instead.

### Requested change

Add an optional `Timestamp` to `SaveCallNoteInput` and use it when present,
falling back to `DateTime.UtcNow` when omitted so existing clients are
unaffected. Two safety rules matter:

- A value carrying **no** offset should be interpreted as UTC. Reading it as
  the server's local zone silently shifts the chronology by the offset.
- A value in the future should be clamped, or a bad client can pin a note
  above live traffic indefinitely.

A reference implementation exists on the fork
(`wabolabs/sparkcore`, branch `sparkops/reseat`, commit `ce047e2d`) with 8
tests and an end-to-end proof, if that saves anyone time. Happy to open it
as a PR against upstream if useful.

### Workaround for anyone hitting this today

Prefix the time into the note text. That is what the fork does — IaR's
`09/28/2026 05:55:31  H1618 - enroute` — but it makes the field useless for
sorting and forces every consumer to parse free text.

---

## Draft 2 — images cannot be built without a paid Docker subscription

**Title:** `All Dockerfiles base on the private dhi.io registry, so the repo cannot be built without a Docker subscription`

**Labels:** `bug`, `build`, `docker`

### Summary

Every Dockerfile in the tree bases on `dhi.io/*`, Docker's hardened-images
registry. That registry requires authentication against a paid Docker
subscription. `docker pull` on any of those refs returns `unauthorized`, so
neither `docker build` nor `docker compose build` can produce an image from a
clean checkout. This affects contributors and self-hosters alike, and it is
not gated behind any flag or opt-in.

### Evidence

Eight Dockerfiles reference it (10 refs total):

```
Web/Resgrid.Web.Broker/Dockerfile
Web/Resgrid.Web.Eventing/Dockerfile
Web/Resgrid.Web.Mcp/Dockerfile
Web/Resgrid.Web.Services/Dockerfile
Web/Resgrid.Web.Tts/Dockerfile
Web/Resgrid.Web/Dockerfile
Workers/Resgrid.TrackerGateway/Dockerfile
Workers/Resgrid.Workers.Console/Dockerfile
```

The two refs are the same everywhere:

```
FROM dhi.io/aspnetcore:9.0.16-debian13@sha256:961647e80202ce33fc06472dda4e7ae2d2bc56d819aee6742602f70047b13dc7 AS base
FROM dhi.io/dotnet:9.0.314-sdk-debian13@sha256:a3acd51de0af79878e26292b3053aab513c6ca4476ddb2f9f11adb9c04aa7c89 AS build
```

```
$ docker pull dhi.io/aspnetcore:9.0.16-debian13
Error response from daemon: Head "https://dhi.io/v2/aspnetcore/manifests/9.0.16-debian13": unauthorized: Unauthorized
```

### Why it is not a one-line swap

The Dockerfiles are written *around* the properties of those images, so
replacing the base means reviewing the code that compensates for it:

- **Fake tzdata.** The DHI images mark `tzdata` as installed in `dpkg` while
  shipping none of the zone files, so a plain `apt-get install tzdata` is a
  silent no-op. Six of the Dockerfiles work around this with
  `--reinstall` plus `test -f /usr/share/zoneinfo/...` assertions, and the
  final stages copy `/usr/share/zoneinfo` in explicitly. Without the
  workaround, `TimeZoneInfo`/`TZConvert` throw `TimeZoneNotFoundException` at
  runtime — which means the workaround is load-bearing, not cosmetic.
- **No shell.** Several Dockerfiles note "these hardened (distroless) images
  have no shell, so we can't chain with `sh -c`", and
  `Workers/Resgrid.TrackerGateway/Dockerfile` copies `/bin/dash` into the
  final image specifically to have one. Any replacement base changes that
  assumption.

### Suggested change

Base the public images on `mcr.microsoft.com/dotnet/aspnet:9.0` and
`mcr.microsoft.com/dotnet/sdk:9.0`, which are public and require no
credentials. A working reference exists on the fork (`wabolabs/sparkcore`,
branch `sparkops/reseat`, commit `a575c8e9`) covering all eight Dockerfiles.

If the hardened images are wanted for the *published* images specifically,
the usual split is to keep `dhi.io` behind a build arg that defaults to MCR,
so a source build works out of the box and only CI (which has the
credentials) pulls the hardened refs.

### Two unrelated failures this masks

While proving the images build on MCR, two further defects surfaced in
`Web/Resgrid.Web`. Neither is about the base image, but both mean the image
does not build even once the registry problem is solved, so they are worth
knowing before someone attempts this:

- **The SPA build races MSBuild.** `Resgrid.Web.csproj`'s `BuildClientApps`
  target runs `npm install` during publish, after which MSBuild immediately
  execs the esbuild binary vite just downloaded — in the same uncommitted
  overlayfs layer. Linux rejects that exec with `ETXTBSY`. Working around
  that gets you `StaticWebAssets` compression failing instead with
  `asset ... can not be found`, because it races the SPA output. The fix is
  to build the SPA in its own committed layers and gate the MSBuild target
  behind an env var (the fork uses `SKIP_NPM_BUILD=1`).
- **`.dockerignore` does not exclude the host-built SPA output.**
  `wwwroot/js/ng` and `Areas/User/Apps/dist` are gitignored but still travel
  into the build context via `COPY . .`, and their stale chunk filenames
  collide with the in-image build's output. This makes the failure
  intermittent in a confusing way: a clean CI checkout builds, a developer
  who ran a host build first does not.

---

## Draft 3 — (placeholder) MSSQL migration `ConstraintExists`

Already filed as [#536](https://github.com/Resgrid/Core/issues/536). Left
here only so the numbering is not reused.
