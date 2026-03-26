# Copilot Instructions — Content Understanding Workshop Repo

## Tech Stack

- **.NET 10** / Blazor Server (Interactive SSR) — `net10.0` TFM
- **Azure SDKs:** Azure.AI.ContentUnderstanding 1.0.2, Azure.AI.OpenAI 2.1.0, Azure.Identity 1.19.0
- **IaC:** Bicep (source of truth) + Terraform (mirror)
- **Testing:** xUnit (26 tests in `CU_TestHarness.Tests`)
- **Notebooks:** C# Polyglot (.NET Interactive) + Python 3.10+
- **Azure Services:** AI Content Understanding (API `2025-11-01`), Document Intelligence (API `2024-11-30`), Azure OpenAI (GPT-4.1, 4.1 Mini, 4o, embedding models)

## Security & Authentication

- **All Azure auth uses `DefaultAzureCredential`** — API keys are disabled (`disableLocalAuth: true`).
- Never use `AzureKeyCredential` or hardcode API keys, connection strings, or secrets.
- Developers need the **Cognitive Services User** role assigned on the CU resource.
- `HttpClient` is always registered via DI (`AddHttpClient<T>`) — never `new HttpClient()`.
- Configuration uses `IOptions<T>` bound from `appsettings.json` sections — no inline secrets.

## Manifest Rule

**After completing any task** that adds, removes, renames, or modifies files or changes project status, **update [`MANIFEST.md`](../MANIFEST.md)**:

1. Update the **"Last updated"** date at the top.
2. Add/remove/update rows in the relevant section.
3. If a **Next Steps** item is completed, mark it done or remove it. Add new next steps if the work creates follow-ups.
4. Log any new decisions or issues in **Known Issues / Decisions**.

Do not skip this step — the manifest is the single source of truth for project status.

## README Rule

**After completing any task** that adds, removes, renames, or modifies files, changes routes/pages, or alters how the project works, **update the relevant README files**:

1. **Root [`README.md`](../README.md)** — Developer-facing overview of the repo.
2. **[`infra/README.md`](../infra/README.md)** — Infrastructure deployment guide (Bicep + Terraform).

Do not skip this step — these READMEs are the first thing developers and customers read.

## Folder Conventions

- **`infra/`** — Bicep + Terraform IaC for deploying CU + Models resources.
- **`src/CU_TestHarness/`** — Blazor Server test harness (upload, analyze, compare, schema CRUD).
- **`src/CU_TestHarness.Tests/`** — xUnit unit tests for the harness (26 tests).
- **`notebook/`** — C# Polyglot and Python notebooks for CU REST API testing.
- **`sample-docs/`** — Synthetic sample PDFs for workshop exercises.

If you need to understand why a folder exists or what it contains, check these docs first:
1. **`README.md`** for high-level folder purpose and contents.
2. **`MANIFEST.md`** for current status and project decisions.

## File Hygiene

- **No `.env` files** committed — only `.env.sample` templates.
- **No `.terraform/` or state files** committed.
- **No `bin/` or `obj/`** committed.

## IaC

- **Bicep is the source of truth** (`infra/bicep/`). Terraform (`infra/deploy.tf`) mirrors it.
- Infra changes go to Bicep first, then update Terraform to match.
- `azuredeploy.json` is a compiled ARM template for the Deploy to Azure button — regenerate after Bicep changes.
- TPM defaults: 100K for GPT models, 50K for embedding models.
- CU defaults API version must be `2025-11-01` (GA). Preview versions return 404.

## CU API Constraints

- **Analyzer IDs cannot contain hyphens** (`-`). Use underscores (`_`) only.
- **`baseAnalyzerId` must be a root-level prebuilt** (`prebuilt-document`, `prebuilt-image`, etc.). Derived analyzers like `prebuilt-layout` are not valid.
- **`classify` and `generate` field methods** may trigger `InvalidFieldSchema` errors. Prefer `extract` where possible.
- **CU API version:** `2025-11-01` (GA). Use this for all CU REST calls.
- **DI API version:** `2024-11-30`. Use this for Document Intelligence calls.

## Blazor Harness Conventions

- Services are registered as `AddHttpClient<T>()` or `AddSingleton<T>()` in `Program.cs`.
- Configuration is bound via `IOptions<ContentUnderstandingOptions>` and `IOptions<DocumentIntelligenceOptions>`.
- Razor pages live in `Components/Pages/`, shared components in `Components/Shared/`.
- One component per `.razor` file. `@inject` directives and parameters at the top.
- Files uploaded by users exist only in memory during analysis — never written to disk.

## Testing

- 26 xUnit tests in `src/CU_TestHarness.Tests/`.
- Run with `dotnet test` before marking work complete.
- New service logic should have corresponding unit tests.

## Notebooks

- **C# Polyglot Notebooks** (`.ipynb`, `.NET (C#)` kernel). Use `@enum` (not `enum_values`) for C# reserved word escaping in anonymous objects.
- **Python Notebook** (`notebook/CU-API-Testing-Guide-Python.ipynb`). Dependencies in `notebook/requirements.txt`.
- Both notebooks target the same CU REST API walkthrough — keep them in sync when updating API examples.
