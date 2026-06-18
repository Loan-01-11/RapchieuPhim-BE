using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using RapchieuPhim.API.Constants;

namespace RapchieuPhim.API.DTO.DTORequest
{
    // ── Đăng nhập / kiểm tra Google ──────────────────────────────────────
    public class GoogleAuthRequest
    {
        [Required(ErrorMessage = ValidationMessages.GoogleIdTokenRequired)]
        [DefaultValue("mock-google-test")]
        public string IdToken { get; set; } = null!;
    }
}
