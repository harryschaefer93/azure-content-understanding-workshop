# Infrastructure

Infrastructure-as-code for Azure AI Content Understanding. **Bicep is the source of truth**; Terraform mirrors it.

## Deployment Modes

### Single-Region (default)

One Azure AI Services account hosts both the CU endpoint and all model deployments. Simplest setup — no cross-resource RBAC needed.

| Resource | Purpose |
|----------|---------|
| Resource Group | Container for all resources |
| CU AI Services account | Hosts CU endpoint + model deployments |
| 7 Model Deployments | GPT-4.1, GPT-4.1 Mini (x2), GPT-4o, Ada-002, Embed-3-Large, Embed-3-Small |

### Cross-Region (optional)

Separate Models account in a second region. Use when CU and models must be in different regions (e.g., for capacity or data residency).

| Resource | Purpose |
|----------|---------|
| Resource Group | Container for all resources |
| CU AI Services account | Hosts the CU endpoint |
| Models AI Services account | Hosts model deployments in a separate region |
| Role Assignment | CU managed identity -> Cognitive Services User on Models |
| 7 Model Deployments | On the Models account |

## Option A: Bicep (recommended)

Click the **Deploy to Azure** button in the root README, or deploy manually:

```bash
az deployment group create \
  --resource-group <your-rg> \
  --template-file infra/bicep/main.bicep \
  --parameters prefix=contoso location=eastus
```

For cross-region, add `modelsLocation`:

```bash
az deployment group create \
  --resource-group <your-rg> \
  --template-file infra/bicep/main.bicep \
  --parameters prefix=contoso location=canadacentral modelsLocation=canadaeast
```

## Option B: Terraform

```bash
cd infra
cp terraform.tfvars.example terraform.tfvars   # edit with your values
terraform init
terraform apply -var-file="terraform.tfvars"
```

For cross-region, uncomment the `cross_region`, `models_account_name`, and `models_location` variables in your tfvars.

## Post-Deploy: Set Model Defaults

After deployment completes:

1. In the **Azure AI Foundry Portal**, add the Models account (or CU account for single-region) as a connected resource.
2. Update `defaults-body.json` — replace `yourconnectionname` with the account name without hyphens.
3. PATCH the defaults:

```bash
TOKEN=$(az account get-access-token --resource https://cognitiveservices.azure.com --query accessToken -o tsv)
curl -s -X PATCH "https://<cu-account>.cognitiveservices.azure.com/contentunderstanding/defaults?api-version=2025-11-01" \
  -H "Content-Type: application/json" -H "Authorization: Bearer $TOKEN" \
  -d @defaults-body.json
```

> **Note:** API version must be `2025-11-01` (GA). Content-Type must be `application/json`.

## Naming Convention

- `{prefix}-cu` — CU endpoint (e.g., `contoso-cu`)
- `{prefix}-models` — Models host, cross-region only (e.g., `contoso-models`)

## Security

- `local_auth_enabled = false` / `disableLocalAuth: true` — API key auth is disabled.
- All access requires **Microsoft Entra ID** (DefaultAzureCredential).
- Assign **Cognitive Services User** role to each developer/tester on the CU resource.
