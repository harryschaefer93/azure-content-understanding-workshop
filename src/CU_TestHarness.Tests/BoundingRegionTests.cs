using CU_TestHarness.Models;
using CU_TestHarness.Services;

namespace CU_TestHarness.Tests;

public class BoundingRegionTests
{
    [Fact]
    public void ParseFieldsFromRawJson_ExtractsBoundingRegions_FromDocumentFields()
    {
        var result = new AnalysisViewModel
        {
            RawJson = """
            {
                "result": {
                    "contents": [
                        {
                            "analyzeResult": {
                                "documents": [
                                    {
                                        "fields": {
                                            "BorrowerName": {
                                                "value": "John Doe",
                                                "confidence": 0.97,
                                                "type": "string",
                                                "boundingRegions": [
                                                    {
                                                        "pageNumber": 1,
                                                        "polygon": [1.5, 2.3, 3.2, 2.3, 3.2, 2.6, 1.5, 2.6]
                                                    }
                                                ]
                                            }
                                        }
                                    }
                                ]
                            }
                        }
                    ]
                }
            }
            """
        };

        ContentUnderstandingService.ParseFieldsFromRawJson(result);

        var field = Assert.Single(result.Fields);
        Assert.Equal("BorrowerName", field.Name);
        Assert.Equal("John Doe", field.Value);
        Assert.NotNull(field.BoundingRegions);
        var region = Assert.Single(field.BoundingRegions);
        Assert.Equal(1, region.PageNumber);
        Assert.Equal(8, region.Polygon.Count);
        Assert.Equal(1.5, region.Polygon[0]);
        Assert.Equal(2.3, region.Polygon[1]);
    }

    [Fact]
    public void ParseFieldsFromRawJson_ExtractsBoundingRegions_FromDirectFields()
    {
        var result = new AnalysisViewModel
        {
            RawJson = """
            {
                "result": {
                    "contents": [
                        {
                            "fields": {
                                "LoanAmount": {
                                    "value": "$250,000",
                                    "confidence": 0.95,
                                    "type": "currency",
                                    "boundingRegions": [
                                        {
                                            "pageNumber": 2,
                                            "polygon": [4.0, 5.0, 6.0, 5.0, 6.0, 5.5, 4.0, 5.5]
                                        }
                                    ]
                                }
                            }
                        }
                    ]
                }
            }
            """
        };

        ContentUnderstandingService.ParseFieldsFromRawJson(result);

        var field = Assert.Single(result.Fields);
        Assert.Equal("LoanAmount", field.Name);
        Assert.NotNull(field.BoundingRegions);
        var region = Assert.Single(field.BoundingRegions);
        Assert.Equal(2, region.PageNumber);
        Assert.Equal(8, region.Polygon.Count);
    }

    [Fact]
    public void ParseFieldsFromRawJson_NoBoundingRegions_FieldStillExtracted()
    {
        var result = new AnalysisViewModel
        {
            RawJson = """
            {
                "result": {
                    "contents": [
                        {
                            "fields": {
                                "Summary": {
                                    "value": "A brief summary",
                                    "confidence": 0.88,
                                    "type": "string"
                                }
                            }
                        }
                    ]
                }
            }
            """
        };

        ContentUnderstandingService.ParseFieldsFromRawJson(result);

        var field = Assert.Single(result.Fields);
        Assert.Equal("Summary", field.Name);
        Assert.Null(field.BoundingRegions);
    }

    [Fact]
    public void ParseFieldsFromRawJson_MultipleBoundingRegions_OnDifferentPages()
    {
        var result = new AnalysisViewModel
        {
            RawJson = """
            {
                "result": {
                    "contents": [
                        {
                            "analyzeResult": {
                                "documents": [
                                    {
                                        "fields": {
                                            "Address": {
                                                "value": "123 Main St",
                                                "confidence": 0.92,
                                                "type": "string",
                                                "boundingRegions": [
                                                    {
                                                        "pageNumber": 1,
                                                        "polygon": [1.0, 1.0, 3.0, 1.0, 3.0, 1.5, 1.0, 1.5]
                                                    },
                                                    {
                                                        "pageNumber": 2,
                                                        "polygon": [2.0, 3.0, 5.0, 3.0, 5.0, 3.3, 2.0, 3.3]
                                                    }
                                                ]
                                            }
                                        }
                                    }
                                ]
                            }
                        }
                    ]
                }
            }
            """
        };

        ContentUnderstandingService.ParseFieldsFromRawJson(result);

        var field = Assert.Single(result.Fields);
        Assert.NotNull(field.BoundingRegions);
        Assert.Equal(2, field.BoundingRegions.Count);
        Assert.Equal(1, field.BoundingRegions[0].PageNumber);
        Assert.Equal(2, field.BoundingRegions[1].PageNumber);
    }

    [Fact]
    public void ParseFieldsFromRawJson_PolygonTooShort_ExcludesBoundingRegion()
    {
        var result = new AnalysisViewModel
        {
            RawJson = """
            {
                "result": {
                    "contents": [
                        {
                            "fields": {
                                "ShortPoly": {
                                    "value": "test",
                                    "boundingRegions": [
                                        {
                                            "pageNumber": 1,
                                            "polygon": [1.0, 2.0]
                                        }
                                    ]
                                }
                            }
                        }
                    ]
                }
            }
            """
        };

        ContentUnderstandingService.ParseFieldsFromRawJson(result);

        var field = Assert.Single(result.Fields);
        Assert.Null(field.BoundingRegions);
    }

    [Fact]
    public void BoundingRegion_Model_StoresPolygonCorrectly()
    {
        var br = new BoundingRegion
        {
            PageNumber = 3,
            Polygon = [1.5, 2.3, 3.2, 2.3, 3.2, 2.6, 1.5, 2.6]
        };

        Assert.Equal(3, br.PageNumber);
        Assert.Equal(8, br.Polygon.Count);
        Assert.Equal(1.5, br.Polygon[0]);
        Assert.Equal(2.6, br.Polygon[7]);
    }

    [Fact]
    public void ExtractedField_HasBoundingRegions_ReportsCorrectly()
    {
        var field = new ExtractedField
        {
            Name = "TestField",
            Value = "TestValue",
            BoundingRegions =
            [
                new BoundingRegion { PageNumber = 1, Polygon = [1.0, 1.0, 2.0, 1.0, 2.0, 2.0, 1.0, 2.0] }
            ]
        };

        Assert.NotNull(field.BoundingRegions);
        Assert.Single(field.BoundingRegions);
        Assert.Equal(1, field.BoundingRegions[0].PageNumber);
    }

    [Fact]
    public void ExtractedField_WithoutBoundingRegions_IsNull()
    {
        var field = new ExtractedField
        {
            Name = "TestField",
            Value = "TestValue"
        };

        Assert.Null(field.BoundingRegions);
    }
}
