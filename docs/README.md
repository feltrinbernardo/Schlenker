# Documentation index

This directory contains cross-project documentation. Product-specific release
material remains beside the product source so requirements, implementation, and
evidence can be reviewed together.

## Cross-project documents

- [Automation development pipeline](automation-development-pipeline.md): the
  lifecycle and deliverables for the Siemens TIA Portal workstream.

## Product documentation

- `REV12/00_README_FIRST.md`: REV12 scope, import order, validation status, and
  engineering limitations.
- `REV12/Documentation/`: REV12 engineering changes, mappings, and acceptance
  material.
- `fixtures/references/tia-import-rev10/INTEGRATION.md`: provenance and limits
  for the external Siemens functional reference.
- `logs/`: dated setup decisions and append-only request audit entries.

## Local-only material

The following paths are intentionally ignored and must not be linked as durable
repository documentation:

- `.modification_logic/`: incoming or working specifications awaiting curation.
- `img-log/`: local visual references and screenshots.
- `outputs/` and `runs/`: generated archives and evidence.
- `programmer_progetv19/` and `fixtures/projects/`: native engineering projects.

Move reusable, reviewed text specifications into a tracked product or
cross-project documentation directory before proposing them for GitHub.
