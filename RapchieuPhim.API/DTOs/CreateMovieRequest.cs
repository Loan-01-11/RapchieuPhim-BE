using System.ComponentModel.DataAnnotations;
using RapchieuPhim.API.Constants; // 🌟 Import thư mục hằng số để gọi lệnh

namespace RapchieuPhim.API.DTOs
{
    public class CreateMovieRequest
    {
        [Required(ErrorMessage = ValidationMessages.MovieTitleRequired)]
        public string Title { get; set; } = null!;

        public string? Description { get; set; }

        [Required(ErrorMessage = ValidationMessages.MovieDurationRequired)]
        public int Duration { get; set; }

        public string? Director { get; set; }
        public string? Actors { get; set; }
        public string? Language { get; set; }
        public string? Subtitles { get; set; }
        public string? AgeRating { get; set; }

        [Required(ErrorMessage = ValidationMessages.MovieReleaseDateRequired)]
        public DateTime ReleaseDate { get; set; }

        [Required(ErrorMessage = ValidationMessages.MovieEndDateRequired)]
        public DateTime EndDate { get; set; }

        public string? PosterUrl { get; set; }
        public string? TrailerUrl { get; set; }

        [Required(ErrorMessage = ValidationMessages.MovieStatusRequired)]
        public string Status { get; set; } = null!;

        // 🌟 TRƯỜNG MỚI THÊM: Nhận mảng ID các thể loại từ Frontend gửi lên (Ví dụ: [1, 3])
        // Khởi tạo sẵn '= new()' để tránh lỗi NullReferenceException nếu Frontend quên truyền trường này
        public List<int> CategoryIds { get; set; } = new();
    }
}