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

## Draft 2 — (placeholder) MSSQL migration `ConstraintExists`

Already filed as [#536](https://github.com/Resgrid/Core/issues/536). Left
here only so the numbering is not reused.
