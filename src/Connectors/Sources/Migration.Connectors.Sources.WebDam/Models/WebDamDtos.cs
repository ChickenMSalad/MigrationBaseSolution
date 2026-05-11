using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Migration.Connectors.Sources.WebDam.Models;

public sealed class WebDamFolderDto
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("parent")]
    public string? Parent { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

public sealed class WebDamMiniFolderDto
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

public sealed class WebDamAssetDto
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("filename")]
    public string? Filename { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("filesize")]
    public string? Filesize { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("filetype")]
    public string? Filetype { get; set; }

    [JsonPropertyName("folder")]
    public WebDamMiniFolderDto? Folder { get; set; }
}

public sealed class WebDamFolderAssetsResponse
{
    [JsonPropertyName("total_count")]
    public int TotalCount { get; set; }

    [JsonPropertyName("offset")]
    public int Offset { get; set; }

    [JsonPropertyName("limit")]
    public int Limit { get; set; }

    [JsonPropertyName("items")]
    public List<JsonElement> Items { get; set; } = new();
}

public sealed class WebDamOAuthTokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = "bearer";

    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; } = string.Empty;
}

public sealed class WebDamXmpSchemaResponse
{
    [JsonPropertyName("xmpschema")]
    public List<WebDamXmpSchemaFieldDto> XmpSchema { get; set; } = new();
}

public sealed class WebDamXmpSchemaFieldDto
{
    [JsonPropertyName("field")]
    public string? Field { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("label")]
    public string? Label { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("searchable")]
    public string? Searchable { get; set; }

    [JsonPropertyName("position")]
    public string? Position { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("values")]
    public List<string>? Values { get; set; }
}
