using System.Text.Json.Serialization;

namespace RapchieuPhim.API.DTOs.DTORequest
{
    // ─── SEPAY WEBHOOK PAYLOAD ───────────────────────────────────────────────────
    /// <summary>
    /// Dữ liệu Sepay gửi về khi phát hiện tài khoản ngân hàng nhận được tiền chuyển khoản.
    /// </summary>
    public class SepayWebhookRequest
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("gateway")]
        public string? Gateway { get; set; }             // Tên ngân hàng (VD: MBBank, TPBank...)

        [JsonPropertyName("transactionDate")]
        public string? TransactionDate { get; set; }     // Thời điểm chuyển khoản

        [JsonPropertyName("accountNumber")]
        public string? AccountNumber { get; set; }       // Số tài khoản nhận tiền

        [JsonPropertyName("transferAmount")]
        public decimal AmountIn { get; set; }            // Số tiền chuyển vào (Sepay gửi về là transferAmount)

        [JsonPropertyName("transferType")]
        public string? TransferType { get; set; }

        [JsonPropertyName("content")]
        public string? TransactionContent { get; set; }  // Nội dung chuyển khoản (Sepay gửi về là content)

        [JsonPropertyName("referenceCode")]
        public string? ReferenceNumber { get; set; }     // Mã tham chiếu ngân hàng (Sepay gửi về là referenceCode)

        [JsonPropertyName("body")]
        public string? Body { get; set; }
    }
}
