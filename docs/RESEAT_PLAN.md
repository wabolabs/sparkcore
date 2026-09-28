# SparkOps Core — re-seat plan

**Status:** proposed, not started. Written 2026-09-28 against
`upstream/master` = `0bdf2c0e`.

This fork (`wabolabs/sparkcore`) was branched from `Resgrid/Core` at
`f72e3ec2` and now carries 9 substantive commits. Upstream has since moved
**438 commits** ahead. This document says what to keep, what to drop, what it
costs to replay, and in what order.

---

## 1. What the fork actually contains

Nine substantive commits (plus one merge and one `.claude/` cleanup — the
"11 ahead" figure double-counts those):

| Commit | Content | Replay cost |
|---|---|---|
| `400ee0b0` | Phases 0–2: branding, Postgres default, PuppeteerSharp PDF | 5 conflicts |
| `928395e3` | CI/CD + Dockerfiles + `CHANGELOG.md` | 7 conflicts |
| `18357b2b` | `workflow_dispatch` trigger | 1 conflict (superseded) |
| `d9a0802e` | esbuild ETXTBSY fix in web image | **clean** |
| `30d448f3` | Phase 3: remove Stripe/Paddle, unlimited mode | 5 conflicts |
| `ee78b9d5` | Phase 4: Postmark/SES → MailKit SMTP | 3 conflicts |
| `e5660b7b` | compose → GHCR, add tts/mcp services | 1 conflict |
| `58c3f08f` | CI: retry GHCR login | 1 conflict |
| `015866cf` | Untrack `.claude/settings.local.json` | 1 conflict |

Two replay clean. Seven need manual resolution.

---

## 2. What upstream has since solved — and not solved

**Solved by upstream (drop the fork's version):**

- **Postgres.** Upstream now ships `Providers/Resgrid.Providers.MigrationsPg/`
  with **236 migrations, at exact parity with the 236 MSSQL ones**. This was
  Phase 1 of the fork and is now entirely redundant. Do not replay it.
- Note `DataConfig.DatabaseType` still *defaults to* `SqlServer` upstream, so
  the config change (defaulting to Postgres, clearing the hardcoded
  connection string) is still worth keeping — just not the claim that the
  fork pioneered Pg support.

**Not solved by upstream (the fork's work is still live):**

- `Providers/Resgrid.Providers.Pdf/NRecoProvider.cs` — still present.
  Upstream still depends on commercial NReco.PdfGenerator.LT.
- `Providers/Resgrid.Providers.Email/PostmarkEmailSender.cs` — still present.
- **22 files still reference Stripe.** Billing is not open-sourced upstream.
- Branding is upstream's own product identity, unchanged.

So the assumption "upstream caught up, reset and start over" is half right:
the *database* half caught up, the *product* half did not.

---

## 3. Strategy: replay, not rebase

Do **not** `git rebase` the fork branch onto upstream. The branch is a
straight line off `f72e3ec2`; rebasing 438 commits under it will surface
conflicts in commits you intend to drop anyway, and the two big semantic
rewrites (Phases 3 and 4) will resolve badly as mechanical cherry-picks.

Instead, build a **new branch off `upstream/master`** and replay deliberately:

```
git fetch upstream
git switch -c sparkops/reseat upstream/master
```

Then work in three groups:

### Group A — cheap, isolated, replay verbatim

Replay these essentially as-is. They get the fork current and CI green with
low risk.

> **Blocker found during the pilot (2026-09-28): upstream cannot be built
> at all without a paid Docker subscription.** Every Dockerfile in
> `upstream/master` bases on `dhi.io/*` — Docker's hardened-images registry
> — e.g. `dhi.io/aspnetcore:9.0.16-debian13@sha256:9616...`. Pulling it
> fails with `unauthorized`, and the Dockerfiles are written *around* it:
> the `tzdata` reinstall hack (DHI marks tzdata installed but ships no zone
> files) and the no-shell distroless `ENTRYPOINT ["./wait"]`. The fork's
> Phase 2 already replaced all of this with MCR images.
>
> This promotes the Dockerfile commit (`928395e3`) from "cheap replay" to
> **the thing that makes the tree buildable**, and it should therefore go
> *first* in Group A, not alongside CI. To prove the pilot patch, the
> Dockerfile was temporarily switched to `mcr.microsoft.com/dotnet/*:9.0`
> bases, which built cleanly — so the substitution is known-good.

- `d9a0802e` (esbuild ETXTBSY) — cherry-picks clean.
- `e5660b7b` (compose/GHCR) — 1 conflict in `Docker/docker-compose.yml`:
  keep the fork's GHCR image refs, take upstream's service definitions.
- `928395e3` (Dockerfiles) — 7 conflicts, but mechanical: upstream
  restructured the Dockerfiles, so re-apply the base-image and entrypoint
  intent rather than the literal diff. **Do this first — see the blocker
  note above.**
- `58c3f08f`, `18357b2b` (CI tweaks) — fold into the same workflow edit.

### Group B — replay-by-intent (do not cherry-pick the diff)

These rewrote large files that have changed substantially underneath. A
cherry-pick produces something worse than re-applying the goal.

- **Phase 3 (Stripe/billing removal).** Conflicts in
  `Core/Resgrid.Services/SubscriptionsService.cs` (upstream 1537L → fork
  343L), `LimitsService.cs` (299L → 64L), and
  `Workers/Resgrid.Workers.Framework/Logic/PaymentQueueLogic.cs` — **which no
  longer exists upstream at all**. Re-do by hand on the new base.
- **Phase 4 (SMTP email).** Conflicts in `EmailProviderModule.cs` and
  `PostmarkEmailSender.cs`. The MailKit `SmtpEmailSender` itself is new code
  and carries over cleanly; the conflict is only the registration and the
  deletion.

### Group C — drop

- **The Postgres half of `400ee0b0`.** Keep only the branding half.
- `015866cf` (`.claude/` untracking) — re-apply as one line in `.gitignore`.

---

## 4. Target fork scope

Five behaviours to change, agreed 2026-09-28. Each is a *patch*, not a whole
phase, and each should be its own commit with its own test.

1. **Call-note timestamps honoured.** See §5 — the pilot for this workflow.
2. **MSSQL migration path fixed.** Upstream issue
   [Resgrid/Core#536](https://github.com/Resgrid/Core/issues/536):
   `NotImplementedException: Method ConstraintExists is not supported by the
   connectionless processor` at
   `M0023_AddingPlanAddonFor10Pack.cs:11`. Replace `.Constraint(...).Exists()`
   with raw-SQL `IF EXISTS` on the MSSQL path. Complements upstream's Pg
   provider rather than duplicating it.
3. **Branding / white-label as SparkOps.** Replay the Phase 0 half of
   `400ee0b0`.
4. **Billing / limit removal.** Re-do Phase 3 by hand (largest job).
5. **CAD ingestion / dispatch-bridge integration.** New surface, in no
   existing commit — native hooks for the IaR/First Due capture pipeline
   instead of publishing purely through the v4 API.

---

## 5. Pilot: call-note timestamps

This is the cleanest first patch and proves the whole workflow. It is
self-contained and touches neither billing nor email.

### The defect

`Web/Resgrid.Web.Services/Controllers/v4/CallNotesController.cs`:

```csharp
var note = new CallNote();
note.CallId = int.Parse(input.CallId);
note.Timestamp = DateTime.UtcNow;      // <-- ignores the caller entirely
note.Note = input.Note;
```

`Models/v4/CallNotes/SaveCallNoteInput.cs` has no timestamp property, so even
if the controller wanted the caller's time there is nowhere for it to arrive.

Measured against a live 4.862.0 instance on 2026-09-28: posting with
`Timestamp`, `TimestampUtc`, `TimestampLocal`, `DateTimeUtc` and `CreatedOn`
all landed at `now()`. Nothing is honoured today.

### Why it matters operationally

A CAD run card is a chronology. When every note is stamped at publish time,
the order of "dispatched / en route / on scene / cleared" is lost, and a
backfill of a week of history has every note at the same instant. IaR's own
comment times are correct; the Resgrid side discards them.

### The patch

1. Add to `SaveCallNoteInput`:
   ```csharp
   /// <summary>
   /// Time the note was originally taken, ISO-8601. When omitted the server
   /// uses the current UTC time, preserving existing behaviour.
   /// </summary>
   public string Timestamp { get; set; }
   ```
2. In the controller, parse and clamp:
   ```csharp
   note.Timestamp = DateTime.UtcNow;
   if (!String.IsNullOrWhiteSpace(input.Timestamp) &&
       DateTime.TryParse(input.Timestamp, null,
           System.Globalization.DateTimeStyles.RoundtripKind, out var supplied))
   {
       // Accept a UTC or offset-bearing value only: a naive local time would
       // silently shift the chronology by the client's offset.
       note.Timestamp = supplied.Kind == DateTimeKind.Unspecified
           ? DateTime.SpecifyKind(supplied, DateTimeKind.Utc)
           : supplied.ToUniversalTime();
   }
   ```
   Security: the value is caller-supplied, so it must not be trusted for
   anything but display ordering. It is clamped to "not in the future" and
   left subject to the existing department-authorisation check.

### Watch out for

The controller has a two-phase write around this line — an ADP encryption
pass (`PrepareCallNoteWriteAsync`) that re-saves the row after the identity
PK exists. **Set the timestamp before the first save**; if the second save
reconstructs the `CallNote` it must carry the timestamp through, or the
round trip will overwrite it with `UtcNow` exactly as this bug does.

### Verification — done 2026-09-28

`Tests/Resgrid.Tests/CallNotes/CallNoteTimestampTests.cs`, 8 tests, all
green (`dotnet test`, NUnit + FluentAssertions, matching the project's
existing style — not xUnit).

Proven end-to-end against the live bravo stack, not just in unit tests.
Built `sparkops/api:callnote-pilot`, loaded it, pointed the live `api`
service at it, and posted five notes:

| Sent | Stored | Result |
|---|---|---|
| `2026-09-27T15:00:00Z` | `2026-09-27 15:00:00` | kept verbatim |
| `2026-09-27T11:00:00-04:00` | `2026-09-27 15:00:00` | converted correctly |
| `2026-09-27T15:00:00` (no offset) | `2026-09-27 15:00:00` | read as UTC, not shifted |
| omitted | now | backward-compatible |
| `2027-01-01T00:00:00Z` | now | clamped |

Then restored `resgridllc/resgridwebservices:4.862.0` and deleted the probe
rows. The live stack is back on the upstream image; `docker-compose.yml`
backup at `/storage/hwy58vfd/resgrid/docker-compose.pre-pilot.bak`.

**The pilot is done.** `sparkops/reseat` carries exactly one commit ahead of
`upstream/master` (three files, no drift), built and tested.

---

## 6. Licensing

This is an Apache-2.0 fork redistributed under a different product name.
That is permitted, and upstream's NOTICE and copyright headers must stay
intact — `CHANGELOG.md` asserts they are. Two commitments to keep explicit:

- Attribution preserved in footers and headers (already true).
- Upstream contributions flow back where they generalise. Issue #536 is the
  current example; the call-note timestamp limitation is a second candidate
  worth filing once the patch is proven.

---

## 7. Order of work

1. Open `sparkops/reseat` off `upstream/master`.
2. Group A — get current, CI green.
3. **Pilot: the call-note timestamp patch (§5).** Prove the workflow before
   anything large.
4. Group B — Phase 3 then Phase 4, by hand.
5. File the call-note-timestamp limitation upstream.
6. Only then the genuinely new surface: CAD ingestion / dispatch bridge.

Groups A and B can stop at any point without leaving the fork in a worse
state than today, which is why they come before the new features.
