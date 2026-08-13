# ADR-006: Local JSONL Telemetry with a Future Exporter Port

- Status: Accepted
- Date: 2026-08-10
- Requirements: 13.1, 13.2, 13.3, 13.5

## Context

The first runtime needs correlated diagnostics and auditability on a local
engineering workstation. No approved centralized observability destination,
retention policy, or sensitive-data contract exists yet.

## Decision

Emit versioned, redacted JSON Lines events through a `TelemetryPort`. Correlate
run, transition, tool, and model calls. Store local telemetry in the run
directory. Defer OpenTelemetry export behind an optional adapter.

## Alternatives considered

- OpenTelemetry SDK as a core dependency: deferred until an exporter target and
  retention/privacy policy exist.
- Plain-text logs: rejected because stable correlation and automated assertions
  require structured fields.
- No telemetry beyond final reports: rejected because failures and retries would
  be difficult to diagnose.

## Consequences

- A centralized redaction layer is mandatory before persistence.
- JSONL schema changes are versioned.
- Local files must have a documented retention and sanitization policy.

## Review triggers

- A centralized monitoring target is approved.
- Retention volume exceeds local storage constraints.
- Cross-host correlation becomes a product requirement.
