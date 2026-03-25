namespace CU_TestHarness.Models;

public static class AnalyzerTemplates
{
    public static string FieldExtraction(string analyzerId = "custom_title_search", string completionModel = "gpt-4.1", string embeddingModel = "text-embedding-ada-002") => $$"""
    {
      "analyzerId": "{{analyzerId}}",
      "baseAnalyzerId": "prebuilt-document",
      "models": {
        "completion": "{{completionModel}}",
        "embedding": "{{embeddingModel}}"
      },
      "fieldSchema": {
        "fields": {
          "DocumentType": {
            "type": "string",
            "method": "classify",
            "description": "Type of the document, e.g. Title Search, Certificate, Endorsement"
          },
          "Province": {
            "type": "string",
            "method": "extract",
            "description": "Province or territory where the property is located"
          },
          "PropertyAddress": {
            "type": "string",
            "method": "extract",
            "description": "Full civic address of the property"
          },
          "LegalDescription": {
            "type": "string",
            "method": "extract",
            "description": "Legal land description (e.g. Lot, Plan, Block)"
          },
          "TitleNumber": {
            "type": "string",
            "method": "extract",
            "description": "Title number or registration number"
          },
          "RegisteredOwner": {
            "type": "string",
            "method": "extract",
            "description": "Name(s) of the registered owner(s)"
          },
          "RegistrationDate": {
            "type": "string",
            "method": "extract",
            "description": "Date the title was registered"
          },
          "Encumbrances": {
            "type": "array",
            "method": "extract",
            "description": "List of encumbrances, liens, or charges registered against the title",
            "items": {
              "type": "object",
              "properties": {
                "Type": { "type": "string", "description": "Type of encumbrance (e.g. Mortgage, Caveat, Easement)" },
                "RegistrationNumber": { "type": "string", "description": "Registration number of the encumbrance" },
                "RegisteredDate": { "type": "string", "description": "Date the encumbrance was registered" },
                "InFavourOf": { "type": "string", "description": "Party in whose favour the encumbrance is registered" }
              }
            }
          },
          "Summary": {
            "type": "string",
            "method": "generate",
            "description": "A brief plain-language summary of the title search results including any notable encumbrances"
          }
        }
      }
    }
    """;

    public static string DocumentClassification(string analyzerId = "custom_doc_classifier", string completionModel = "gpt-4.1", string embeddingModel = "text-embedding-ada-002") => $$"""
    {
      "analyzerId": "{{analyzerId}}",
      "baseAnalyzerId": "prebuilt-document",
      "models": {
        "completion": "{{completionModel}}",
        "embedding": "{{embeddingModel}}"
      },
      "fieldSchema": {
        "fields": {
          "DocumentCategory": {
            "type": "string",
            "method": "classify",
            "description": "High-level category: TitleSearch, Certificate, Endorsement, Invoice, CoverLetter, Other"
          },
          "DocumentSubType": {
            "type": "string",
            "method": "classify",
            "description": "Specific sub-type within the category (e.g. ON Title Search, BC Title Search, AB Title Search)"
          },
          "Confidence": {
            "type": "string",
            "method": "generate",
            "description": "Explanation of why this classification was chosen"
          }
        }
      }
    }
    """;

    public static string RagSearch(string analyzerId = "custom_rag_search", string completionModel = "gpt-4.1", string embeddingModel = "text-embedding-ada-002") => $$"""
    {
      "analyzerId": "{{analyzerId}}",
      "baseAnalyzerId": "prebuilt-documentSearch"
    }
    """;

    public static string CtiClassification(string analyzerId = "custom_cti_classifier", string completionModel = "gpt-4.1", string embeddingModel = "text-embedding-ada-002") => $$"""
    {
      "analyzerId": "{{analyzerId}}",
      "baseAnalyzerId": "prebuilt-document",
      "models": {
        "completion": "{{completionModel}}",
        "embedding": "{{embeddingModel}}"
      },
      "fieldSchema": {
        "fields": {
          "DocumentCategory": {
            "type": "string",
            "method": "classify",
            "description": "Primary document category found in a CTI bundle: TitleSearch, CertificateOfTitle, Transfer, Mortgage, Caveat, Discharge, Easement, Lien, CourtOrder, Survey, CoverLetter, Invoice, Other"
          },
          "Province": {
            "type": "string",
            "method": "classify",
            "description": "Canadian province or territory: AB, BC, MB, NB, NL, NS, NT, NU, ON, PE, QC, SK, YT"
          },
          "DocumentDate": {
            "type": "string",
            "method": "extract",
            "description": "Primary date on the document (registration date, issue date, or effective date)"
          },
          "ReferenceNumber": {
            "type": "string",
            "method": "extract",
            "description": "Primary reference or registration number on the document"
          },
          "Parties": {
            "type": "array",
            "method": "extract",
            "description": "Names of parties involved (grantor, grantee, mortgagor, mortgagee, caveator, etc.)",
            "items": {
              "type": "object",
              "properties": {
                "Name": { "type": "string", "description": "Full name of the party" },
                "Role": { "type": "string", "description": "Role of the party (e.g. Registered Owner, Mortgagee, Caveator)" }
              }
            }
          },
          "Summary": {
            "type": "string",
            "method": "generate",
            "description": "One-paragraph plain-language summary of this document's purpose and key details"
          }
        }
      }
    }
    """;

    public static string MultiProvinceTitleSearch(string analyzerId = "custom_multi_province_title", string completionModel = "gpt-4.1", string embeddingModel = "text-embedding-ada-002") => $$"""
    {
      "analyzerId": "{{analyzerId}}",
      "baseAnalyzerId": "prebuilt-document",
      "models": {
        "completion": "{{completionModel}}",
        "embedding": "{{embeddingModel}}"
      },
      "fieldSchema": {
        "fields": {
          "Province": {
            "type": "string",
            "method": "classify",
            "description": "Canadian province or territory where the property is located: AB, BC, MB, NB, NL, NS, NT, NU, ON, PE, QC, SK, YT"
          },
          "TitleNumber": {
            "type": "string",
            "method": "extract",
            "description": "Title number, parcel identifier, or registration number (format varies by province)"
          },
          "PropertyAddress": {
            "type": "string",
            "method": "extract",
            "description": "Full civic or municipal address of the property"
          },
          "LegalDescription": {
            "type": "string",
            "method": "extract",
            "description": "Legal land description including lot, plan, block, section, township, range, or PID as applicable"
          },
          "RegisteredOwner": {
            "type": "string",
            "method": "extract",
            "description": "Name(s) of all registered owner(s), including tenancy type if stated"
          },
          "RegistrationDate": {
            "type": "string",
            "method": "extract",
            "description": "Date the title or most recent transfer was registered"
          },
          "TitleStatus": {
            "type": "string",
            "method": "classify",
            "description": "Current status of the title: Active, Cancelled, Historical, Pending"
          },
          "Encumbrances": {
            "type": "array",
            "method": "extract",
            "description": "All encumbrances, charges, liens, caveats, and interests registered against the title",
            "items": {
              "type": "object",
              "properties": {
                "Type": { "type": "string", "description": "Type of encumbrance (Mortgage, Caveat, Easement, Lien, Utility Right-of-Way, etc.)" },
                "RegistrationNumber": { "type": "string", "description": "Registration or instrument number" },
                "RegisteredDate": { "type": "string", "description": "Date registered" },
                "InFavourOf": { "type": "string", "description": "Party in whose favour the encumbrance is registered" },
                "Amount": { "type": "string", "description": "Dollar amount if applicable (e.g. mortgage principal)" }
              }
            }
          },
          "MunicipalAddress": {
            "type": "string",
            "method": "extract",
            "description": "Municipal or mailing address if different from property address"
          },
          "Summary": {
            "type": "string",
            "method": "generate",
            "description": "Plain-language summary including province, ownership, key encumbrances, and any notable flags for a title reviewer"
          }
        }
      }
    }
    """;

    public static string CommitmentLetter(string analyzerId = "custom_commitment_letter", string completionModel = "gpt-4.1", string embeddingModel = "text-embedding-ada-002") => $$"""
    {
      "analyzerId": "{{analyzerId}}",
      "baseAnalyzerId": "prebuilt-document",
      "models": {
        "completion": "{{completionModel}}",
        "embedding": "{{embeddingModel}}"
      },
      "fieldSchema": {
        "fields": {
          "Borrowers": {
            "type": "array",
            "method": "extract",
            "description": "All borrower names listed on the commitment letter. Extract each borrower as a separate item with name components.",
            "items": {
              "type": "object",
              "properties": {
                "FirstName": { "type": "string", "description": "Borrower's first (given) name" },
                "MiddleName": { "type": "string", "description": "Borrower's middle name or initial, if present" },
                "LastName": { "type": "string", "description": "Borrower's last (family/surname) name" }
              }
            }
          },
          "PropertyStreetNumber": {
            "type": "string",
            "method": "extract",
            "description": "Street number of the mortgaged property address"
          },
          "PropertyUnitNumber": {
            "type": "string",
            "method": "extract",
            "description": "Unit or suite number of the mortgaged property, if applicable"
          },
          "PropertyStreetName": {
            "type": "string",
            "method": "extract",
            "description": "Street name of the mortgaged property address"
          },
          "PropertyCity": {
            "type": "string",
            "method": "extract",
            "description": "City or municipality of the mortgaged property"
          },
          "PropertyProvince": {
            "type": "string",
            "method": "extract",
            "description": "Province or territory of the mortgaged property"
          },
          "PropertyPostalCode": {
            "type": "string",
            "method": "extract",
            "description": "Postal code of the mortgaged property"
          },
          "PropertyAddressFull": {
            "type": "string",
            "method": "extract",
            "description": "Full property address as a single string, exactly as it appears on the document"
          },
          "LenderName": {
            "type": "string",
            "method": "extract",
            "description": "Name of the lending institution or mortgagee"
          },
          "MortgageNumber": {
            "type": "string",
            "method": "extract",
            "description": "Mortgage number, loan number, or commitment reference number"
          },
          "LoanAmount": {
            "type": "string",
            "method": "extract",
            "description": "Total mortgage or loan amount (dollar value)"
          },
          "InterestRate": {
            "type": "string",
            "method": "extract",
            "description": "Interest rate on the mortgage (percentage)"
          },
          "Term": {
            "type": "string",
            "method": "extract",
            "description": "Mortgage term length (e.g. 5 years)"
          },
          "AmortizationPeriod": {
            "type": "string",
            "method": "extract",
            "description": "Amortization period (e.g. 25 years)"
          },
          "ClosingDate": {
            "type": "string",
            "method": "extract",
            "description": "Closing or funding date for the mortgage"
          },
          "SolicitorName": {
            "type": "string",
            "method": "extract",
            "description": "Name of the solicitor or law firm handling the transaction"
          },
          "SolicitorAddress": {
            "type": "string",
            "method": "extract",
            "description": "Full mailing address of the solicitor or law firm"
          },
          "SolicitorConditions": {
            "type": "array",
            "method": "extract",
            "description": "All solicitor conditions or requirements listed in the commitment letter. This field may span multiple pages — extract every condition even if the list continues across page boundaries.",
            "items": {
              "type": "object",
              "properties": {
                "ConditionNumber": { "type": "string", "description": "Condition number or sequence identifier, if present" },
                "ConditionText": { "type": "string", "description": "Full text of the solicitor condition" }
              }
            }
          },
          "Summary": {
            "type": "string",
            "method": "generate",
            "description": "A plain-language summary of the commitment letter including lender, borrowers, property, loan details, and any notable conditions"
          }
        }
      }
    }
    """;

    public static string EnhancedTitleSearch(string analyzerId = "custom_enhanced_title_search", string completionModel = "gpt-4.1", string embeddingModel = "text-embedding-ada-002") => $$"""
    {
      "analyzerId": "{{analyzerId}}",
      "baseAnalyzerId": "prebuilt-document",
      "models": {
        "completion": "{{completionModel}}",
        "embedding": "{{embeddingModel}}"
      },
      "fieldSchema": {
        "fields": {
          "Province": {
            "type": "string",
            "method": "classify",
            "description": "Canadian province or territory where the property is located: AB, BC, MB, NB, NL, NS, NT, NU, ON, PE, QC, SK, YT"
          },
          "TitleNumber": {
            "type": "string",
            "method": "extract",
            "description": "Title number, parcel identifier, or registration number (format varies by province)"
          },
          "PropertyAddress": {
            "type": "string",
            "method": "extract",
            "description": "Full civic or municipal address of the property"
          },
          "ShortLegal": {
            "type": "string",
            "method": "extract",
            "description": "Short legal description or abbreviated legal land description as shown on the title"
          },
          "LegalDescription": {
            "type": "string",
            "method": "extract",
            "description": "Full legal land description including lot, plan, block, section, township, range, or PID as applicable"
          },
          "RegisteredOwners": {
            "type": "array",
            "method": "extract",
            "description": "All registered owners listed on the title. Extract each owner as a separate item with name components.",
            "items": {
              "type": "object",
              "properties": {
                "FirstName": { "type": "string", "description": "Owner's first (given) name" },
                "MiddleName": { "type": "string", "description": "Owner's middle name or initial, if present" },
                "LastName": { "type": "string", "description": "Owner's last (family/surname) name" }
              }
            }
          },
          "RegistrationDate": {
            "type": "string",
            "method": "extract",
            "description": "Date the title or most recent transfer was registered"
          },
          "TitleStatus": {
            "type": "string",
            "method": "classify",
            "description": "Current status of the title: Active, Cancelled, Historical, Pending"
          },
          "Encumbrances": {
            "type": "array",
            "method": "extract",
            "description": "All encumbrances, charges, liens, caveats, and interests registered against the title. This field may span multiple pages.",
            "items": {
              "type": "object",
              "properties": {
                "Type": { "type": "string", "description": "Type of encumbrance (Mortgage, Caveat, Easement, Lien, Utility Right-of-Way, etc.)" },
                "RegistrationNumber": { "type": "string", "description": "Registration or instrument number" },
                "RegisteredDate": { "type": "string", "description": "Date registered" },
                "InFavourOf": { "type": "string", "description": "Party in whose favour the encumbrance is registered" },
                "Amount": { "type": "string", "description": "Dollar amount if applicable (e.g. mortgage principal)" }
              }
            }
          },
          "MunicipalAddress": {
            "type": "string",
            "method": "extract",
            "description": "Municipal or mailing address if different from property address"
          },
          "Summary": {
            "type": "string",
            "method": "generate",
            "description": "Plain-language summary including province, ownership, key encumbrances, and any notable flags for a title reviewer"
          }
        }
      }
    }
    """;

    /// <summary>
    /// Generates an analyzer JSON schema from a list of detected field names and inferred types.
    /// Used by the Schema Builder feature (UC1) to convert auto-detected fields into a custom analyzer.
    /// </summary>
    public static string GenerateSchemaFromFields(string analyzerId, IEnumerable<(string Name, string Type, string Method, string Description)> fields, string completionModel = "gpt-4.1", string embeddingModel = "text-embedding-ada-002")
    {
        var fieldEntries = fields.Select(f =>
        {
            var escapedName = System.Text.Json.JsonEncodedText.Encode(f.Name).ToString();
            var escapedDesc = System.Text.Json.JsonEncodedText.Encode(f.Description).ToString();
            return $$"""
            "{{escapedName}}": {
              "type": "{{f.Type}}",
              "method": "{{f.Method}}",
              "description": "{{escapedDesc}}"
            }
            """;
        });

        var fieldsJson = string.Join(",\n", fieldEntries);
        return $$"""
    {
      "analyzerId": "{{analyzerId}}",
      "baseAnalyzerId": "prebuilt-document",
      "models": {
        "completion": "{{completionModel}}",
        "embedding": "{{embeddingModel}}"
      },
      "fieldSchema": {
        "fields": {
          {{fieldsJson}}
        }
      }
    }
    """;
    }

    /// <summary>
    /// Simplified overload that accepts (Name, Method) tuples — defaults type to "string" and description to the field name.
    /// </summary>
    public static string GenerateSchemaFromFields(string analyzerId, IEnumerable<(string Name, string Method)> fields, string completionModel = "gpt-4.1", string embeddingModel = "text-embedding-ada-002")
    {
        return GenerateSchemaFromFields(analyzerId,
            fields.Select(f => (f.Name, "string", f.Method, f.Name)), completionModel, embeddingModel);
    }

    public static IReadOnlyList<(string Name, string Description, Func<string, string, string, string> Generate)> All =>
    [
        ("Commitment Letter", "Extract borrower names (first/middle/last), split address components, loan details, solicitor conditions — targets common DI pain points", CommitmentLetter),
        ("Enhanced Title Search", "Extract title fields with structured owner names (first/middle/last), short legal, encumbrances — handles cross-page tables", EnhancedTitleSearch),
        ("Field Extraction (Title Search)", "Extract structured fields from title search documents — province, owners, encumbrances, legal description", FieldExtraction),
        ("CTI Document Classification", "Classify documents in a CTI bundle by type, province, and parties — covers title transfers, mortgages, caveats, discharges, etc.", CtiClassification),
        ("Multi-Province Title Search", "Extract title fields across all Canadian provinces — handles varying formats for AB, BC, ON, QC, SK, etc.", MultiProvinceTitleSearch),
        // Document Classification and RAG/Document Search excluded from workshop —
        // CU API returns InvalidFieldSchema for classify+generate field method combinations.
        // Static methods retained below for future use when CU schema validation is updated.
    ];
}
