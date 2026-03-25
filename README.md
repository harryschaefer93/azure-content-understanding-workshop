# Azure Content Understanding Workshop

Accelerator repo for hands-on workshops comparing **Azure AI Content Understanding (CU)** and **Document Intelligence (DI)**. Includes infrastructure-as-code, a Blazor Server test harness covering 6 use cases, a C# Polyglot Notebook, and synthetic sample documents.

> **Tracking:** See [`MANIFEST.md`](MANIFEST.md) for project status and decisions.

## Repository Structure

```
ContentUnderstanding.sln          Solution file
infra/                            Terraform — deploys CU + Models accounts
  deploy.tf                       Single-file IaC (RG, CU, Models, RBAC)
  terraform.tfvars.example        Example variable values
  defaults-body.json              PATCH body for CU model routing defaults

src/CU_TestHarness/               Blazor Server test harness
  Components/Pages/               11 Razor pages (Analyze, Compare, Schema Builder, …)
  Models/                         View models, analyzer templates, cost estimator
  Services/                       CU + DI + Completion service clients
  wwwroot/                        Static assets incl. architecture.html

src/CU_TestHarness.Tests/         xUnit unit tests

notebook/
  CU-API-Testing-Guide.ipynb      C# Polyglot Notebook — full CU REST API walkthrough

sample-docs/                      Synthetic sample PDFs
  commitment-letters/             5 commitment letter samples
  title-search/                   2 title search samples
```

## Workshop Use Cases

| UC | Name | Harness Page |
|----|------|--------------|
| 1 | Schema Generation from samples | `/schema-builder` |
| 2 | Schema Tuning (templates) | `/schema-editor` |
| 3 | Automated Test Suite | `/test` |
| 4 | Natural Language Query | `/analyze` (NL tab) |
| 5 | Multi-Document Validation | `/validate` |
| 6 | Corrective Feedback Loop | `/feedback` |

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Terraform CLI](https://developer.hashicorp.com/terraform/install) (>= 1.5)
- [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli)
- [VS Code](https://code.visualstudio.com/) + [Polyglot Notebooks extension](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.dotnet-interactive-vscode)

## Quick Start

### 1. Deploy infrastructure

```bash
cd infra
cp terraform.tfvars.example terraform.tfvars   # edit with your values
terraform init
terraform apply -var-file="terraform.tfvars"
```

After apply, set the AOAI connection in the Foundry Portal and PATCH the defaults:

```bash
TOKEN=$(az account get-access-token --resource https://cognitiveservices.azure.com --query accessToken -o tsv)
curl -s -X PATCH "https://<cu-account>.cognitiveservices.azure.com/contentunderstanding/defaults?api-version=2025-11-01" \
  -H "Content-Type: application/json" -H "Authorization: Bearer $TOKEN" \
  -d @defaults-body.json
```

### 2. Configure the harness

Edit `src/CU_TestHarness/appsettings.json` with your CU and Models endpoints.

### 3. Run

```bash
az login
cd src/CU_TestHarness
dotnet run
```

Open `http://localhost:5000` in your browser.

### 4. Run tests

```bash
dotnet test src/CU_TestHarness.Tests/
```

## Authentication

All access uses **Microsoft Entra ID** (`DefaultAzureCredential`). API keys are disabled.
Each user needs the **Cognitive Services User** role on the CU resource.

## Resources

- [Content Understanding Overview](https://learn.microsoft.com/en-us/azure/ai-services/content-understanding/overview)
- [Content Understanding Studio](https://aka.ms/cu-studio)
- [CU REST API Reference](https://learn.microsoft.com/en-us/rest/api/contentunderstanding/operation-groups)
- [Cross-Resource Setup Guide](https://learn.microsoft.com/en-us/azure/ai-services/content-understanding/how-to/bring-your-own-cross-resource-capacity)

## License

[MIT](LICENSE)
