# Copilot Instructions — Content Understanding Workshop Repo

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
2. **[`infra/README.md`](../infra/README.md)** — Infrastructure deployment guide (Terraform).

Do not skip this step — these READMEs are the first thing developers and customers read.

## Folder Conventions

- **`infra/`** — Terraform IaC for deploying CU + Models resources.
- **`src/CU_TestHarness/`** — Blazor Server test harness (upload, analyze, compare, schema CRUD).
- **`src/CU_TestHarness.Tests/`** — xUnit unit tests for the harness.
- **`notebook/`** — C# Polyglot Notebook for CU REST API testing.
- **`sample-docs/`** — Synthetic sample PDFs for workshop exercises.

If you need to understand why a folder exists or what it contains, check these docs first:
1. **`README.md`** for high-level folder purpose and contents.
2. **`MANIFEST.md`** for current status and project decisions.

## File Hygiene

- **No `.env` files** committed — only `.env.sample` templates.
- **No `.terraform/` or state files** committed.
- **No `bin/` or `obj/`** committed.

## IaC

- Terraform (`infra/deploy.tf`) — single file deploying CU + Models accounts + role assignment.
- TPM defaults: 100K for GPT models, 50K for embedding models.

## Notebooks

- C# Polyglot Notebooks (`.ipynb`, `.NET (C#)` kernel). Use `@enum` (not `enum_values`) for C# reserved word escaping in anonymous objects.
