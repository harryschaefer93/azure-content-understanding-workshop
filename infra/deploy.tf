# ====================================================================
# Azure AI Content Understanding - Cross-Resource Deployment
# ====================================================================
#
# Single Terraform file deploying:
#   1. Resource Group (created by Terraform)
#   2. Content Understanding AI Services account (Canada Central) — hosts the CU endpoint
#   3. Models AI Services account (Canada East) — hosts GPT + embedding model deployments
#   4. Role assignment: CU managed identity → Cognitive Services User on Models account
#
# Naming convention:  {prefix}-cu-{region}   for the CU service
#                     {prefix}-models-{region} for the models host
#
# Usage:
#   terraform init
#   terraform plan  -var="cu_account_name=contoso-cu-cc" -var="models_account_name=contoso-models-ce"
#   terraform apply -var="cu_account_name=contoso-cu-cc" -var="models_account_name=contoso-models-ce"
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
  default     = "canadacentral"
}

variable "cu_account_name" {
  description = "Globally unique name for the Content Understanding AI Services account (hosts the CU endpoint). Recommended pattern: {prefix}-cu-{region}. Example: contoso-cu-cc (2-64 chars)."
  type        = string

  validation {
    condition     = length(var.cu_account_name) >= 2 && length(var.cu_account_name) <= 64
    error_message = "Account name must be 2-64 characters."
  }
}

variable "cu_location" {
  description = "Location for the Content Understanding resource."
  type        = string
  default     = "canadacentral"
}

variable "models_account_name" {
  description = "Globally unique name for the Models AI Services account (hosts GPT + embedding deployments). Recommended pattern: {prefix}-models-{region}. Example: contoso-models-ce (2-64 chars)."
  type        = string

  validation {
    condition     = length(var.models_account_name) >= 2 && length(var.models_account_name) <= 64
    error_message = "Account name must be 2-64 characters."
  }
}

variable "models_location" {
  description = "Location for the Models resource."
  type        = string
  default     = "canadaeast"
}

variable "sku" {
  description = "SKU name for both accounts."
  type        = string
  default     = "S0"
}

variable "tags" {
  description = "Tags to apply to all resources."
  type        = map(string)
  default     = {}
}

variable "gpt41_capacity" {
  description = "Capacity (1K TPM units) for gpt-4.1 deployment."
  type        = number
  default     = 100
}

variable "gpt41_mini_capacity" {
  description = "Capacity (1K TPM units) for gpt-4.1-mini deployment."
  type        = number
  default     = 100
}

variable "gpt41_mini_standard_capacity" {
  description = "Capacity (1K TPM units) for gpt-4.1-mini Standard (Canada-guaranteed) deployment."
  type        = number
  default     = 100
}

variable "gpt4o_standard_capacity" {
  description = "Capacity (1K TPM units) for gpt-4o Standard (Canada-guaranteed) deployment."
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
# Resource Group
# ====================================================================

resource "azurerm_resource_group" "rg" {
  name     = var.resource_group_name
  location = var.resource_group_location

  tags = var.tags
}

# ====================================================================
# Content Understanding AI Services Account (Canada Central)
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
# Models AI Services Account (Canada East)
# Hosts GPT-4.1, GPT-4.1-mini, and embedding model deployments.
# CU calls into this account via its managed identity.
# ====================================================================

resource "azurerm_cognitive_account" "models" {
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
# ====================================================================

resource "azurerm_cognitive_deployment" "gpt41" {
  name                 = "gpt-41"
  cognitive_account_id = azurerm_cognitive_account.models.id

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
  cognitive_account_id = azurerm_cognitive_account.models.id

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
  name                 = "gpt-41-mini-ca"
  cognitive_account_id = azurerm_cognitive_account.models.id

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
  name                 = "gpt-4o-ca"
  cognitive_account_id = azurerm_cognitive_account.models.id

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
  cognitive_account_id = azurerm_cognitive_account.models.id

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
  cognitive_account_id = azurerm_cognitive_account.models.id

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
  cognitive_account_id = azurerm_cognitive_account.models.id

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
# Role Assignment: CU → Models (Cognitive Services User)
# ====================================================================
# Grants the CU resource's managed identity "Cognitive Services User"
# on the Models account for Entra ID cross-resource authentication.

resource "azurerm_role_assignment" "cu_to_models" {
  scope                = azurerm_cognitive_account.models.id
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
  description = "Models account endpoint — used internally by CU via managed identity; also needed for AOAI connection name."
  value       = azurerm_cognitive_account.models.endpoint
}

output "models_resource_id" {
  description = "Resource ID of the Models account (hosts GPT + embedding deployments)."
  value       = azurerm_cognitive_account.models.id
}

output "models_connection_name" {
  description = "AOAI connection name for defaults-body.json (account name without hyphens)."
  value       = replace(var.models_account_name, "-", "")
}
