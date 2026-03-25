# Infrastructure — Terraform

Single-file Terraform deployment for Azure AI Content Understanding with cross-resource model routing.

## What Gets Deployed

| Resource | Purpose | Default Region |
|----------|---------|----------------|
| Resource Group | Container for all resources | Canada Central |
| CU AI Services account | Hosts the Content Understanding endpoint | Canada Central |
| Models AI Services account | Hosts GPT + embedding model deployments | Canada East |
| Role Assignment | CU managed identity → Cognitive Services User on Models | — |
| 7 Model Deployments | GPT-4.1, GPT-4.1 Mini (×2), GPT-4o, Ada-002, Embed-3-Large, Embed-3-Small | Canada East |

## Usage

```bash
cd infra
cp terraform.tfvars.example terraform.tfvars   # edit with your values
terraform init
terraform plan -var-file="terraform.tfvars"
terraform apply -var-file="terraform.tfvars"
```

## Post-Deploy: Set Model Defaults

After Terraform finishes:

1. In the **Azure AI Foundry Portal**, add the Models account as a connected resource on the CU account.
2. Update `defaults-body.json` — replace `yourconnectionname` with the actual AOAI connection name (Models account name without hyphens).
3. PATCH the defaults:

```bash
TOKEN=$(az account get-access-token --resource https://cognitiveservices.azure.com --query accessToken -o tsv)
curl -s -X PATCH "https://<cu-account>.cognitiveservices.azure.com/contentunderstanding/defaults?api-version=2025-11-01" \
  -H "Content-Type: application/json" -H "Authorization: Bearer $TOKEN" \
  -d @defaults-body.json
```

> **Note:** API version must be `2025-11-01` (GA). Content-Type must be `application/json`.

## Naming Convention

- `{prefix}-cu-{region}` — CU endpoint (e.g., `contoso-cu-cc`)
- `{prefix}-models-{region}` — Models host (e.g., `contoso-models-ce`)

## Security

- `local_auth_enabled = false` — API key auth is disabled on both accounts.
- All access requires **Microsoft Entra ID** (DefaultAzureCredential).
- Assign **Cognitive Services User** role to each developer/tester on the CU resource.
