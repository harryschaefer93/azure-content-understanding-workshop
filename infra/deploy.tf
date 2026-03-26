# ====================================================================
# Azure AI Content Understanding — Infrastructure Deployment
# ====================================================================
#
# Single Terraform file deploying:
#   1. Resource Group
#   2. Content Understanding AI Services account — hosts the CU endpoint
#   3. (Optional) Models AI Services account — hosts GPT + embedding
#      deployments in a separate region (cross-region mode)
#   4. 7 model deployments (GPT + embedding)
#   5. (Cross-region only) Role assignment: CU managed identity →
#      Cognitive Services User on Models account
#
# Deployment modes:
#   Single-region (default):  One account hosts both CU and models.
#   Cross-region (optional):  Set cross_region = true to create a
#                             separate Models account with RBAC.
#
# Usage:
#   terraform init
#   terraform plan  -var="cu_account_name=contoso-cu"
#   terraform apply -var="cu_account_name=contoso-cu"
#
# ====================================================================

terraform {
  required_version = ">= 1.5"
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.0"
    }
  }
}

provider "azurerm" {
  features {}
}

# ====================================================================
# Variables
# ====================================================================

variable "resource_group_name" {
  description = "Name of the resource group to create."
  type        = string
  default     = "CU-Workshop-RG"
}

variable "resource_group_location" {
  description = "Azure location for the resource group."
  type        = string
  default     = "eastus"
}

variable "cu_account_name" {
  description = "Globally unique name for the Content Understanding AI Services account. Example: contoso-cu (2-64 chars)."
  type        = string

  validation {
    condition     = length(var.cu_account_name) >= 2 && length(var.cu_account_name) <= 64
    error_message = "Account name must be 2-64 characters."
  }
}

variable "cu_location" {
  description = "Location for the Content Understanding resource. Must support Azure AI Content Understanding."
  type        = string
  default     = "eastus"
}

variable "cross_region" {
  description = "Enable cross-region mode. When true, creates a separate Models account in models_location with managed-identity RBAC. When false (default), all resources share the CU account."
  type        = bool
  default     = false
}

variable "models_account_name" {
  description = "Globally unique name for the Models AI Services account (cross-region only). Example: contoso-models (2-64 chars)."
  type        = string
  default     = ""

  validation {
    condition     = var.models_account_name == "" || (length(var.models_account_name) >= 2 && length(var.models_account_name) <= 64)
    error_message = "Account name must be empty or 2-64 characters."
  }
}

variable "models_location" {
  description = "Location for the Models resource (cross-region only)."
  type        = string
  default     = ""
}

variable "sku" {
  description = "SKU name for AI Services accounts."
  type        = string
  default     = "S0"
}

variable "tags" {
  description = "Tags to apply to all resources."
  type        = map(string)
  default     = {}
}

variable "gpt41_capacity" {
  description = "Capacity (1K TPM units) for gpt-4.1 GlobalStandard deployment."
  type        = number
  default     = 100
}

variable "gpt41_mini_capacity" {
  description = "Capacity (1K TPM units) for gpt-4.1-mini GlobalStandard deployment."
  type        = number
  default     = 100
}

variable "gpt41_mini_standard_capacity" {
  description = "Capacity (1K TPM units) for gpt-4.1-mini Standard (region-guaranteed) deployment."
  type        = number
  default     = 100
}

variable "gpt4o_standard_capacity" {
  description = "Capacity (1K TPM units) for gpt-4o Standard (region-guaranteed) deployment."
  type        = number
  default     = 100
}

variable "embedding_ada_capacity" {
  description = "Capacity (1K TPM units) for text-embedding-ada-002 deployment."
  type        = number
  default     = 50
}

variable "embedding_3large_capacity" {
  description = "Capacity (1K TPM units) for text-embedding-3-large deployment."
  type        = number
  default     = 50
}

variable "embedding_3small_capacity" {
  description = "Capacity (1K TPM units) for text-embedding-3-small deployment."
  type        = number
  default     = 50
}

# ====================================================================
# Locals
# ====================================================================

locals {
  models_account_id = var.cross_region ? azurerm_cognitive_account.models[0].id : azurerm_cognitive_account.cu.id
  models_account_name = var.cross_region ? var.models_account_name : var.cu_account_name
}

# ====================================================================
# Resource Group
# ====================================================================

resource "azurerm_resource_group" "rg" {
  name     = var.resource_group_name
  location = var.resource_group_location

  tags = var.tags
}

# ====================================================================
# Content Understanding AI Services Account
# Hosts the CU endpoint — this is what the notebook / test harness calls.
# ====================================================================

resource "azurerm_cognitive_account" "cu" {
  name                          = var.cu_account_name
  location                      = var.cu_location
  resource_group_name           = azurerm_resource_group.rg.name
  kind                          = "AIServices"
  sku_name                      = var.sku
  custom_subdomain_name         = var.cu_account_name
  public_network_access_enabled = true
  local_auth_enabled            = false

  identity {
    type = "SystemAssigned"
  }

  tags = var.tags
}

# ====================================================================
# Models AI Services Account (cross-region only)
# Hosts GPT and embedding model deployments in a separate region.
# CU calls into this account via its managed identity.
# ====================================================================

resource "azurerm_cognitive_account" "models" {
  count = var.cross_region ? 1 : 0

  name                          = var.models_account_name
  location                      = var.models_location
  resource_group_name           = azurerm_resource_group.rg.name
  kind                          = "AIServices"
  sku_name                      = var.sku
  custom_subdomain_name         = var.models_account_name
  public_network_access_enabled = true
  local_auth_enabled            = false

  identity {
    type = "SystemAssigned"
  }

  tags = var.tags
}

# ====================================================================
# Model Deployments
# Deployed on the Models account (cross-region) or CU account (single-region).
# ====================================================================

resource "azurerm_cognitive_deployment" "gpt41" {
  name                 = "gpt-41"
  cognitive_account_id = local.models_account_id

  model {
    format = "OpenAI"
    name   = "gpt-4.1"
  }

  sku {
    name     = "GlobalStandard"
    capacity = var.gpt41_capacity
  }
}

resource "azurerm_cognitive_deployment" "gpt41_mini" {
  name                 = "gpt-41-mini"
  cognitive_account_id = local.models_account_id

  model {
    format = "OpenAI"
    name   = "gpt-4.1-mini"
  }

  sku {
    name     = "GlobalStandard"
    capacity = var.gpt41_mini_capacity
  }

  depends_on = [azurerm_cognitive_deployment.gpt41]
}

resource "azurerm_cognitive_deployment" "gpt41_mini_standard" {
  name                 = "gpt-41-mini-std"
  cognitive_account_id = local.models_account_id

  model {
    format = "OpenAI"
    name   = "gpt-4.1-mini"
  }

  sku {
    name     = "Standard"
    capacity = var.gpt41_mini_standard_capacity
  }

  depends_on = [azurerm_cognitive_deployment.gpt41_mini]
}

resource "azurerm_cognitive_deployment" "gpt4o_standard" {
  name                 = "gpt-4o-std"
  cognitive_account_id = local.models_account_id

  model {
    format  = "OpenAI"
    name    = "gpt-4o"
    version = "2024-11-20"
  }

  sku {
    name     = "Standard"
    capacity = var.gpt4o_standard_capacity
  }

  depends_on = [azurerm_cognitive_deployment.gpt41_mini_standard]
}

resource "azurerm_cognitive_deployment" "embedding_ada" {
  name                 = "text-embedding-ada-002"
  cognitive_account_id = local.models_account_id

  model {
    format = "OpenAI"
    name   = "text-embedding-ada-002"
  }

  sku {
    name     = "Standard"
    capacity = var.embedding_ada_capacity
  }

  depends_on = [azurerm_cognitive_deployment.gpt4o_standard]
}

resource "azurerm_cognitive_deployment" "embedding_3large" {
  name                 = "text-embedding-3-large"
  cognitive_account_id = local.models_account_id

  model {
    format = "OpenAI"
    name   = "text-embedding-3-large"
  }

  sku {
    name     = "Standard"
    capacity = var.embedding_3large_capacity
  }

  depends_on = [azurerm_cognitive_deployment.embedding_ada]
}

resource "azurerm_cognitive_deployment" "embedding_3small" {
  name                 = "text-embedding-3-small"
  cognitive_account_id = local.models_account_id

  model {
    format = "OpenAI"
    name   = "text-embedding-3-small"
  }

  sku {
    name     = "Standard"
    capacity = var.embedding_3small_capacity
  }

  depends_on = [azurerm_cognitive_deployment.embedding_3large]
}

# ====================================================================
# Role Assignment: CU → Models (cross-region only)
# ====================================================================
# Grants the CU resource's managed identity "Cognitive Services User"
# on the Models account for Entra ID cross-resource authentication.

resource "azurerm_role_assignment" "cu_to_models" {
  count = var.cross_region ? 1 : 0

  scope                = azurerm_cognitive_account.models[0].id
  role_definition_name = "Cognitive Services User"
  principal_id         = azurerm_cognitive_account.cu.identity[0].principal_id
  principal_type       = "ServicePrincipal"
  skip_service_principal_aad_check = true
}

# ====================================================================
# Outputs
# ====================================================================

output "cu_endpoint" {
  description = "CU endpoint URL — use this in notebooks, test harness, and curl commands."
  value       = azurerm_cognitive_account.cu.endpoint
}

output "cu_resource_id" {
  description = "Resource ID of the Content Understanding account."
  value       = azurerm_cognitive_account.cu.id
}

output "models_endpoint" {
  description = "Models endpoint — same as CU endpoint in single-region mode; separate in cross-region mode."
  value       = var.cross_region ? azurerm_cognitive_account.models[0].endpoint : azurerm_cognitive_account.cu.endpoint
}

output "models_resource_id" {
  description = "Resource ID of the account hosting model deployments."
  value       = local.models_account_id
}

output "models_connection_name" {
  description = "Connection name for defaults-body.json (account name without hyphens)."
  value       = replace(local.models_account_name, "-", "")
}
