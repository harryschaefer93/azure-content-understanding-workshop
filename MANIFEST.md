# Project Manifest — Azure Content Understanding Workshop

> **Last updated:** 2026-03-27

## Overview

Workshop accelerator for Azure AI Content Understanding (CU) and Document Intelligence (DI). Includes Bicep + Terraform IaC, a Blazor Server test harness with 6 use cases, C# and Python notebooks, and synthetic sample documents. All authentication uses Microsoft Entra ID (API keys disabled).

> **Copilot:** See [`.github/copilot-instructions.md`](.github/copilot-instructions.md) for session rules.

---

## Repository Layout

| Path | Purpose | Status |
|------|---------|--------|
| `infra/bicep/` | Bicep templates (source of truth) — single or cross-region deployment | Done |
| `infra/bicep/azuredeploy.json` | Compiled ARM template for Deploy to Azure button | Done |
| `infra/deploy.tf` | Terraform — mirrors Bicep, single or cross-region | Done |
| `infra/defaults-body.json` | PATCH body for CU model routing defaults | Done |
| `infra/terraform.tfvars.example` | Example variable values | Done |
| `src/CU_TestHarness/` | Blazor Server test harness — 6 pages, 5 shared components, 6 workshop UCs | Done |
| `src/CU_TestHarness.Tests/` | xUnit unit tests (26 tests) | Done |
| `notebook/CU-API-Testing-Guide.ipynb` | C# Polyglot Notebook — full CU REST API walkthrough | Done |
| `notebook/CU-API-Testing-Guide-Python.ipynb` | Python notebook — same walkthrough | Done |
| `sample-docs/` | Synthetic PDFs (5 commitment letters, 2 title searches) + test manifests | Done |
| `README.md` | Architecture diagrams (Mermaid), flow diagrams, and workshop use-case table | Done |

## Deployment Modes

| Mode | Description | Default |
|------|-------------|---------|
| Single-region | One account hosts CU + models | Yes |
| Cross-region | Separate Models account with managed-identity RBAC | Optional |

## Blazor Test Harness Features

### Pages (6)

- **Home** (`/`) — UC1–UC6 tile dashboard
- **Analyzers** (`/analyzers`, `/schema-editor`) — Manage tab (browse/edit/delete/create analyzers, quick test) + Auto-Detect tab (upload samples → detect fields → generate & create analyzer). Covers UC1, UC2, UC6.
- **Test Suite** (`/test`, `/validate`) — Test Suite mode (batch testing with expected values, auto-populate, semantic matching, UC3) + Cross-Doc Validation mode (N-document consistency matrix, UC5)
- **Analyze** (`/analyze`) — file upload, field extraction, PDF viewer with bounding box overlays, NL query (UC4)
- **Compare** (`/compare`) — CU vs DI side-by-side analysis
- **Settings** (`/settings`) — model profile selection, endpoint config, connection test

### Shared Components (5)

- **FileUploadArea** — drag-drop file upload with customizable hint, accepts, and multi-file support
- **AnalyzerSelect** — analyzer dropdown with prebuilt/custom grouping and optional custom ID toggle
- **FieldsTable** — field/value/confidence table with click-to-highlight support
- **ResultsTabs** — tabbed results view (Fields, JSON, Markdown) with copy-to-clipboard
- **CostCard** — cost estimate display card with configurable detail level

### Other

- **Architecture** (`/architecture.html`) — CU vs DI architecture comparison reference

## Model Profiles

| Profile | SKU | Data Residency |
|---------|-----|----------------|
| GPT-4.1 Global Standard | GlobalStandard | May leave region |
| GPT-4.1 Mini Global Standard | GlobalStandard | May leave region |
| GPT-4.1 Mini Standard | Standard | Region-guaranteed |
| GPT-4o Standard | Standard | Region-guaranteed |
| Ada 002 | Standard | Region-guaranteed |
| Embedding 3 Large | Standard | Region-guaranteed |
| Embedding 3 Small | Standard | Region-guaranteed |

## Known Issues / Decisions

- **IaC source of truth**: Bicep is canonical. Terraform mirrors it. Future infra changes go to Bicep first.
- **CU API template schemas**: `classify` and `generate` field methods may trigger `InvalidFieldSchema` errors. `extract`-only schemas work reliably.
- **Custom analyzer `baseAnalyzerId`**: CU API requires root-level base IDs (`prebuilt-document`, `prebuilt-image`, etc.). Derived analyzers like `prebuilt-layout` are not valid.
- **Analyzer ID naming**: CU API rejects hyphens (`-`). All templates use underscores (`_`). 3-layer client-side validation.
- **CU defaults API version**: Must use `2025-11-01` (GA). Preview versions return 404 in some regions. Content-Type must be `application/json`.
- **Foundry Portal connection**: Must be established manually before defaults PATCH works. Not automatable via IaC.
- **RBAC for developers**: `Cognitive Services User` role must be assigned on the CU resource for each user.
- **Semantic matching**: Uses the active completion model (GPT-4.1 Mini default) via a separate Models endpoint for LLM-powered field comparison.
- **Cost estimates**: Based on published pricing tables + actual token/page counts. Rates are hardcoded in `CostEstimator.cs`.
- **Blazor harness runs locally**: Files are sent to Azure CU endpoint for analysis, not processed locally.

---

## Next Steps

| # | Task | Status |
|---|------|--------|
| 1 | Tag cleaned repo as v1.0 | Pending |
| 2 | UC6 corrective feedback loop via CU Studio (arriving ~April 1) | Deferred |
| 3 | Validate cost estimates against actual workshop usage | Pending |
| 4 | UI improvements pass — consolidated 11 pages → 6 pages + 5 shared components | Done |
