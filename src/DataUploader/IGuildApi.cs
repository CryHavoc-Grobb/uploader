using System.Text.Json.Serialization;
using Refit;

namespace DataUploader;

internal interface IGuildApi
{
    [Put("/bank")]
    Task UpdateGuildBankAsync(
        UpdateGuildBankRequest request, 
        [Authorize("token")] string token);
}

internal sealed class UpdateGuildBankRequest
{
    [JsonPropertyName("items")]
    public List<GuildBankItem> Items { get; set; } = [];
}