# Now Responding API (A1)

`GET /api/v4/Calls/{callId}/responding`

Who is currently responding to a call — the question the Core dashboard, the
BigBoard, the chatbot, and (via SparkOps) the dispatch board all answer. One
call per request; department and `CanUserViewCall` authorization apply exactly
as on `GetCallExtraData`.

## Response

```json
{
  "Data": {
    "CallId": "12",
    "FirstArrivalAt": "2026-01-12T15:06:00Z",
    "FirstArrivalEta": null,
    "Personnel": [
      {
        "UserId": "...", "Name": "Tammy Adams",
        "GroupId": "4", "Group": "Station 1",
        "StatusId": 2, "StatusText": "Responding", "StatusColor": "#00aaff",
        "Timestamp": "2026-01-12T15:01:00Z",
        "Bucket": "enroute",
        "EtaAt": "2026-01-12T15:16:00Z",
        "StaffingText": "Available", "StaffingColor": "label-success"
      }
    ],
    "Units": [
      {
        "UnitId": 2, "Name": "Engine 1", "Type": "Engine",
        "GroupId": "8", "Group": "Station 3",
        "StateId": 5, "StateText": "Responding", "StateColor": "#00aaff",
        "Timestamp": "2026-01-12T15:02:00Z",
        "Bucket": "enroute"
      }
    ]
  },
  "Status": 200, "Version": "...", "Timestamp": "..."
}
```

## Semantics

**"Current" is an intersection, not a history scan.** The call-scoped queries
(`GetActionLogsForCallAsync`, `GetUnitStatesForCallAsync`) return every status
change made during the call. A responder is included only when their *latest
department-wide* status (the `GetLastActionLogsForDepartmentAsync` row, or the
latest unit state) is still the call-tied one. Someone who responded and then
went back to Standing By drops off — which is what "currently responding"
means and what the old Core dashboard computed inline.

**Buckets.** `enroute` or `onscene`; anything else is excluded.

| Default status | Bucket |
|---|---|
| `ActionTypes.Responding`, `RespondingToScene`, `RespondingToStation` | enroute |
| `ActionTypes.OnScene` | onscene |
| `UnitStateTypes.Responding`, `Enroute` | enroute |
| `UnitStateTypes.OnScene`, `Staging` | onscene |

Custom states (> 25) classify by their `BaseType`: Responding/Enroute/
Transporting → enroute; OnScene/MadeContact/AtPatient/Staging/Searching →
onscene. This is the chatbot `CallRespondersActionHandler`'s classification,
extracted into `CallRespondingService` so the two cannot drift.

**First arrival.** `FirstArrivalAt` is the earliest on-scene timestamp in the
call's *history* — a unit that arrived and later returned still arrived.
`FirstArrivalEta` is `EtaPulledOn + Eta seconds` from the earliest current
en-route person, and only while nobody is on scene; it is null when
`GeoService` has no route (`Eta <= 0`). Nothing is estimated client-side.

**Staffing** is the person's latest `UserState`, resolved through
`GetCustomPersonnelStaffingAsync` (default `UserStateTypes` text for ≤ 25).

## Files

- `Core/Resgrid.Model/CallRespondingSnapshot.cs` — DTOs + bucket constants
- `Core/Resgrid.Model/Services/ICallRespondingService.cs`
- `Core/Resgrid.Services/CallRespondingService.cs` (registered in `ServicesModule`)
- `Web/Resgrid.Web.Services/Models/v4/Calls/CallRespondingResult.cs`
- `Web/Resgrid.Web.Services/Controllers/v4/CallsController.cs` (`GetCallResponding`)
- `Tests/Resgrid.Tests/Services/CallRespondingServiceTests.cs`

## Follow-ons

- A2: Core dashboard panel + BigBoard widget consuming this endpoint.
- The chatbot handler still owns its own copy of the classification for
  localized text; migrating it onto `CallRespondingService` is a follow-on.
