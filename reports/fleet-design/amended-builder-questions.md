# Amended-design builder check — 2026-10-27

1. **Important — What happens to an assembly slot after automatic safety withdrawal from its rally system?** FLT-003 automatically dispatches reserved ships toward a known rally system; FLT-002/007 lets safety override that order. The draft defines Missing for manual detachment/loss, but does not define the request state or retry authority after a surviving ship withdraws. If the rally remains hazardous or forbidden by its posture, automatic Assemble retries could repeatedly send it back. Specify whether that slot becomes Blocked pending an explicitly approved rally change/resume, or whether some bounded retry rule applies. This determines request transitions, authority, reports, saved state and loop-prevention tests.

The amended Organizer accounting, Normal/Urgent ordering and temporary selection authority otherwise provide enough detail for planning.
