# ROB --- CIP RECIPE SECURITY, APPROVAL & AUDIT TRAIL

## High-Level Authorization for Recipe Editing / Locked Daily Production Recipes

**Project:** Schlenker Monoblock Real Juice\
**Platform:** Siemens TIA Portal V19 / WinCC Unified\
**Purpose:** define strict authorization, approval, locking and
traceability rules for CIP recipes.

------------------------------------------------------------------------

# 1. Core Rule

CIP recipe **execution** and CIP recipe **engineering/editing** are two
different permissions.

The normal machine operator must be able to run an approved cleaning
recipe without being able to alter its chemistry, sequence, duration or
advance mode.

A recipe created and approved for the customer's daily cleaning cycle
shall therefore be executable by the Operator but **locked against
editing**.

------------------------------------------------------------------------

# 2. Authorization Levels

Use the existing global WinCC Unified user/session system. Do not create
a separate CIP login database.

## Operator

Allowed: - view approved recipes; - select an approved production/daily
CIP recipe; - start CIP when PLC permissives are satisfied; - use
`NEXT STEP` when the active recipe explicitly requires MANUAL NEXT; -
acknowledge CIP Complete; - view current step, medium, remaining time,
alarms and diagnostics; - abort/stop only according to the approved PLC
operating philosophy.

Not allowed: - edit Medium; - edit Duration; - edit AUTO / MANUAL
NEXT; - add/delete/enable/disable steps; - overwrite an approved
recipe; - create a new recipe; - change engineering limits; - unlock a
protected recipe.

## Maintenance

Default philosophy: - Operator permissions; - diagnostics and
maintenance visibility; - **no recipe editing by default**.

Recipe editing may only be granted to Maintenance later by explicit
project approval.

## Engineer / Administrator

Allowed: - create a new CIP recipe; - edit recipe steps; - select
allowed media; - edit duration; - select AUTO / MANUAL NEXT; -
enable/disable steps; - save a new recipe; - use Save As; -
submit/finalize a recipe for approval; - inspect recipe
revision/history.

## Highest-Level Recipe Authority

Use a high authorization level such as **Engineer/Admin** or a dedicated
permission `CIP_RECIPE_ENGINEERING`.

Only this permission shall allow changes to approved/locked CIP recipes.

Do not rely only on hiding HMI buttons. PLC/HMI command authorization
must prevent unauthorized writes.

------------------------------------------------------------------------

# 3. Recipe States

Each stored recipe shall have a controlled lifecycle state:

``` text
DRAFT
APPROVED
LOCKED
SUPERSEDED
```

### DRAFT

Editable by authorized recipe engineering users.

### APPROVED

Recipe has been reviewed and is available for controlled use.

### LOCKED

Recipe is approved for routine operation and cannot be edited by
Operator or normal Maintenance users.

### SUPERSEDED

Historical recipe retained for traceability but not selectable for
normal operation.

------------------------------------------------------------------------

# 4. Daily Cleaning Recipe Philosophy

The customer may have one or more standard recipes used every day.

Example:

``` text
Recipe Name: DAILY_CIP_STANDARD
Status: LOCKED
Operator selectable: YES
Operator editable: NO
```

The Operator can run this recipe repeatedly without changing it.

If the customer requests a new cleaning program, an authorized
Engineer/Admin creates a **new revision or new recipe** rather than
casually modifying the daily approved recipe.

------------------------------------------------------------------------

# 5. Edit Page Access

The HMI Recipe Edit page shall require a high-level authorization.

If the logged-in user does not have recipe-engineering permission:

-   editing controls are disabled or inaccessible;
-   Save / Save As / Approve / Lock controls are unavailable;
-   recipe values remain read-only;
-   the user may still view the approved recipe if operationally
    appropriate.

Use the existing global User/Login area from the Production master
common frame.

------------------------------------------------------------------------

# 6. Closed Medium Selection

Medium selection must use a controlled enumeration/dropdown, never free
text.

Initial requested choices:

``` text
OFF / UNUSED
COLD WATER
HOT WATER
CAUSTIC
CITRA / NEUTRALIZER
WARM WATER
STEAM
AIR
NITROGEN
DISCHARGE
```

Only commissioned/available media may be executable.

If a medium has no verified physical interface, it must remain
unavailable/TBC and cannot be activated simply by recipe selection.

------------------------------------------------------------------------

# 7. Recipe Change Audit Trail

Every recipe modification shall create an audit record.

Minimum information:

-   timestamp;
-   logged-in username;
-   user role;
-   recipe name;
-   recipe revision;
-   action type;
-   step number;
-   previous value;
-   new value;
-   approval/lock action where applicable.

Actions to record include:

``` text
RECIPE_CREATED
RECIPE_EDITED
STEP_ENABLED
STEP_DISABLED
MEDIUM_CHANGED
DURATION_CHANGED
ADVANCE_MODE_CHANGED
RECIPE_SAVED
RECIPE_APPROVED
RECIPE_LOCKED
RECIPE_UNLOCKED
RECIPE_SUPERSEDED
```

Where WinCC Unified native audit functionality is available and
appropriate, use it rather than creating an unnecessary duplicate
system.

------------------------------------------------------------------------

# 8. Execution Audit / Batch History

Keep recipe engineering changes separate from runtime execution history.

For each CIP run, record where technically supported:

-   start timestamp;
-   finish timestamp;
-   logged-in operator;
-   recipe name;
-   recipe revision;
-   completed / aborted / faulted;
-   each executed step;
-   selected medium;
-   programmed duration;
-   actual elapsed duration;
-   AUTO or MANUAL NEXT;
-   NEXT STEP operator action where applicable;
-   major CIP faults/aborts.

This provides evidence of **what recipe was actually run**, not merely
what recipe currently exists.

------------------------------------------------------------------------

# 9. Recipe Revision Control

An approved recipe shall carry a revision identifier.

Concept:

``` text
DAILY_CIP_STANDARD
Revision: 03
Status: LOCKED
```

When a locked recipe needs modification:

1.  authorized user opens it;
2.  create a controlled new revision / Save As;
3.  previous approved revision remains historically identifiable;
4.  edit the new DRAFT;
5.  validate;
6.  approve;
7.  lock;
8.  make the new revision operational;
9.  supersede the old revision if appropriate.

Do not overwrite history invisibly.

------------------------------------------------------------------------

# 10. Approval Rule

A recipe must not become an Operator-selectable production recipe merely
because someone pressed Save.

Use a deliberate approval step.

Minimum philosophy:

``` text
SAVE = stores DRAFT
APPROVE = authorizes recipe for operational selection
LOCK = prevents routine editing
```

If project scope does not support separate APPROVE and LOCK commands,
combine them only after explicit approval of that simplified workflow.

------------------------------------------------------------------------

# 11. PLC Protection

The PLC shall not trust arbitrary HMI recipe values blindly.

Before CIP Start:

-   recipe must be valid;
-   recipe must be approved for operation;
-   every enabled medium must be commissioned/available;
-   duration must be inside engineering limits;
-   step codes must be valid;
-   sequence data must be internally consistent;
-   CIP interface must be ready;
-   machine state/permissives must be valid.

Unauthorized HMI writes must not bypass PLC validation.

------------------------------------------------------------------------

# 12. Editing While CIP Is Running

Recipe editing shall not alter the active running sequence unexpectedly.

Preferred rule:

-   Active CIP recipe instance is frozen/snapshotted at Start.
-   Editing stored recipes during an active CIP is prohibited or
    isolated from the running instance.
-   Changes become effective only on a subsequent CIP run after
    Save/Approval.

Do not allow a duration or chemical to change halfway through an active
step because someone edited the stored recipe.

------------------------------------------------------------------------

# 13. Manual Next Security

`MANUAL NEXT` means operator-authorized transition between automatically
controlled steps.

Operator may press `NEXT STEP` only when:

-   PLC state = `WAITING FOR NEXT STEP`;
-   current timed phase is complete;
-   transition/safe state is satisfied;
-   no blocking fault exists;
-   recipe remains valid.

Log the username and timestamp of the NEXT STEP action where practical.

NEXT STEP must never directly energize a medium output.

------------------------------------------------------------------------

# 14. Customer-Specific Recipes

Support multiple approved recipes where required, for example:

``` text
DAILY_CIP_STANDARD
PRODUCT_CHANGE_CIP
WEEKLY_DEEP_CLEAN
CUSTOMER_RECIPE_01
```

Names are examples only.

The customer can run the appropriate approved recipe. Creation or
modification remains restricted to the high authorization level.

------------------------------------------------------------------------

# 15. HMI Visual Requirements

The Recipe Edit and CIP Runtime pages must use the REV25 Production
master common frame:

-   identical Common Header;
-   identical Common Status;
-   identical Nav/Back;
-   identical Common Buttons;
-   identical global User/Login area;
-   identical fonts, dimensions, coordinates, colors and spacing.

Inside the Recipe Editor clearly display:

-   Recipe Name;
-   Revision;
-   State: DRAFT / APPROVED / LOCKED / SUPERSEDED;
-   Last Modified By;
-   Last Modified Date/Time;
-   Approved/Locked By where available;
-   10 recipe rows;
-   Save / Save As;
-   approval/lock controls only for authorized users.

------------------------------------------------------------------------

# 16. Alarm / Event Integration

Security events shall not become process alarms unless operationally
relevant.

However, clearly handle:

-   unauthorized edit attempt;
-   invalid recipe;
-   attempt to start unapproved recipe;
-   recipe changed/not approved;
-   unavailable medium;
-   recipe data corruption.

Do not allow alarm acknowledgement to grant recipe permissions.

------------------------------------------------------------------------

# 17. Validation Tests for Rob

Rob shall verify:

-   [ ] Operator can select approved recipe.
-   [ ] Operator can run approved recipe.
-   [ ] Operator cannot edit Medium.
-   [ ] Operator cannot edit Duration.
-   [ ] Operator cannot edit Advance Mode.
-   [ ] Operator cannot Save/Overwrite.
-   [ ] Maintenance cannot edit unless explicitly authorized.
-   [ ] Engineer/Admin can create/edit DRAFT.
-   [ ] Engineer/Admin can Save As.
-   [ ] Approval changes recipe availability correctly.
-   [ ] Lock prevents unauthorized modification.
-   [ ] Previous revision remains traceable.
-   [ ] Audit records username/time/change.
-   [ ] Active running recipe cannot be altered by editing stored data.
-   [ ] Manual NEXT records/uses the current authorized user.
-   [ ] Logout/role change immediately removes editing capability.
-   [ ] PLC rejects invalid/unapproved recipe execution.

------------------------------------------------------------------------

# 18. Deliverables From Rob

Provide:

1.  authorization matrix;
2.  HMI object-level permission list;
3.  recipe state/revision implementation;
4.  recipe edit-page screenshots for Operator and Engineer/Admin;
5.  audit-trail implementation/evidence;
6.  example recipe revision history;
7.  example CIP execution record;
8.  PLC validation evidence;
9.  HMI and PLC compile results;
10. test checklist with PASS/FAIL;
11. list of any WinCC Unified audit limitations or
    licensing/configuration requirements discovered.

------------------------------------------------------------------------

# 19. Master Instruction to Rob

**Separate CIP recipe execution from CIP recipe engineering. The normal
Operator shall be able to select and run an approved locked
daily-cleaning recipe but shall not be able to modify Medium, Duration,
Advance Mode, step enablement or recipe structure. Recipe
creation/editing shall require a high-level Engineer/Admin authorization
using the existing global WinCC Unified user session. Use controlled
medium selections only. Implement recipe lifecycle/revision control so a
saved DRAFT is not automatically treated as an approved production
recipe. Approved daily recipes shall be lockable. Changes to locked
recipes must produce a controlled new revision rather than silently
overwriting history. Record who changed what and when, and separately
record which recipe/revision was executed for each CIP run. Freeze the
active recipe instance during execution so stored recipe edits cannot
change an active CIP. Preserve PLC validation, interlocks, customer CIP
handshake and all safety/process authority. Do not implement
authorization only as cosmetic HMI hiding; unauthorized writes and
unapproved recipe execution must be prevented by the control
architecture.**
