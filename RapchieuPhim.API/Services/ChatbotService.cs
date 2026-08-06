using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using RapchieuPhim.API.Constants;
using RapchieuPhim.API.DTO.DTORequest;
using RapchieuPhim.API.DTO.DTOResponse;
using RapchieuPhim.API.Models;

namespace RapchieuPhim.API.Services;

public interface IChatbotService
{
    string DetectIntent(string question);
    Task<ChatbotResponseDto> AskAsync(string question);
    Task<IReadOnlyList<ChatbotSuggestionDto>> GetSuggestionsAsync();
    Task<IReadOnlyList<ChatbotMovieDto>> GetUpcomingMoviesAsync(int limit = 10);
    Task<IReadOnlyList<ChatbotMovieDto>> GetNowShowingMoviesAsync(int limit = 10);
    Task<IReadOnlyList<ChatbotPromotionDto>> GetActivePromotionsAsync(int limit = 20);
    Task<IReadOnlyList<ChatbotShowtimeDto>> GetShowtimesAsync(ChatbotShowtimeQueryDto query);
    Task<ChatbotFeedbackResponseDto> SubmitFeedbackAsync(ChatbotFeedbackRequestDto request);
}

public sealed class ChatbotService : IChatbotService
{
    public const string ActivePromotions = "GET_ACTIVE_PROMOTIONS";
    public const string UpcomingMovies = "GET_UPCOMING_MOVIES";
    public const string NowShowingMovies = "GET_NOW_SHOWING_MOVIES";
    public const string MovieShowtimes = "GET_MOVIE_SHOWTIMES";
    public const string TicketPrice = "GET_TICKET_PRICE";
    public const string CinemaInformation = "GET_CINEMA_INFORMATION";
    public const string Unknown = "UNKNOWN";

    private static readonly CultureInfo VietnameseCulture = CultureInfo.GetCultureInfo("vi-VN");
    private readonly CinemaManagementContext _context;
    private readonly ILogger<ChatbotService> _logger;

    public ChatbotService(CinemaManagementContext context, ILogger<ChatbotService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public string DetectIntent(string question)
    {
        var text = Normalize(question);

        if (ContainsAny(text, "gia ve", "bao nhieu tien", "gia ghe", "ve ghe"))
            return TicketPrice;
        if (ContainsAny(text, "suat chieu", "lich chieu", "chieu luc", "may gio"))
            return MovieShowtimes;
        if (ContainsAny(text, "khuyen mai", "giam gia", "uu dai", "ma giam"))
            return ActivePromotions;
        if (ContainsAny(text, "sap chieu", "phim moi sap", "khoi chieu sap"))
            return UpcomingMovies;
        if (ContainsAny(text, "dang chieu", "hom nay co phim", "phim hom nay", "phim gi"))
            return NowShowingMovies;
        if (ContainsAny(text, "rap o dau", "rap nam o dau", "dia chi rap", "thong tin rap", "so dien thoai rap", "chi nhanh")
            || (text.Contains("rap") && ContainsAny(text, "nam o dau", "o dau", "dia chi")))
            return CinemaInformation;

        return Unknown;
    }

    public async Task<ChatbotResponseDto> AskAsync(string question)
    {
        var cleanedQuestion = question.Trim();
        var intent = DetectIntent(cleanedQuestion);

        try
        {
            return intent switch
            {
                ActivePromotions => await BuildActivePromotionsResponseAsync(),
                UpcomingMovies => await BuildUpcomingMoviesResponseAsync(),
                NowShowingMovies => await BuildNowShowingMoviesResponseAsync(),
                MovieShowtimes => await GetMovieShowtimesAsync(cleanedQuestion),
                TicketPrice => await GetTicketPricesAsync(cleanedQuestion),
                CinemaInformation => await GetCinemaInformationAsync(cleanedQuestion),
                _ => Response(false, Unknown,
                    "Mình chưa hiểu rõ câu hỏi. Bạn có thể hỏi về phim đang chiếu, phim sắp chiếu, suất chiếu, giá vé, khuyến mãi hoặc địa chỉ rạp.")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chatbot failed while handling intent {Intent}", intent);
            return Response(false, intent, "Xin lỗi, hệ thống chưa thể lấy dữ liệu lúc này. Vui lòng thử lại sau.");
        }
    }

    public async Task<IReadOnlyList<ChatbotSuggestionDto>> GetSuggestionsAsync()
    {
        var now = DateTime.Now;
        var today = DateOnly.FromDateTime(now);

        var nowShowingTitle = await _context.Movies.AsNoTracking()
            .Where(m => MovieStatus.NowShowingStatuses.Contains(m.Status)
                && (!m.ReleaseDate.HasValue || m.ReleaseDate <= today)
                && (!m.EndDate.HasValue || m.EndDate >= today))
            .OrderBy(m => m.Title)
            .Select(m => m.Title)
            .FirstOrDefaultAsync();

        var upcomingTitle = await _context.Movies.AsNoTracking()
            .Where(m => MovieStatus.ComingSoonStatuses.Contains(m.Status)
                || (m.ReleaseDate.HasValue && m.ReleaseDate > today))
            .OrderBy(m => m.ReleaseDate)
            .ThenBy(m => m.Title)
            .Select(m => m.Title)
            .FirstOrDefaultAsync();

        var cinemaName = await _context.Cinemas.AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.CinemaName)
            .Select(c => c.CinemaName)
            .FirstOrDefaultAsync();

        var promotionCode = await _context.Discounts.AsNoTracking()
            .Where(d => d.IsActive && d.StartDate <= now
                && (d.EndDate == null || d.EndDate >= now)
                && (d.MaxUsageTotal == null || d.UsedCount < d.MaxUsageTotal))
            .OrderBy(d => d.EndDate)
            .Select(d => d.DiscountCode)
            .FirstOrDefaultAsync();

        var suggestions = new List<ChatbotSuggestionDto>
        {
            new() { Intent = NowShowingMovies, Category = "movie", Question = "Hôm nay có phim gì đang chiếu?" },
            new() { Intent = UpcomingMovies, Category = "movie", Question = upcomingTitle == null ? "Có phim nào sắp chiếu?" : $"Phim {upcomingTitle} sắp chiếu khi nào?" },
            new() { Intent = MovieShowtimes, Category = "showtime", Question = nowShowingTitle == null ? "Cho mình xem lịch chiếu hôm nay" : $"Phim {nowShowingTitle} có suất chiếu lúc mấy giờ?" },
            new() { Intent = CinemaInformation, Category = "cinema", Question = cinemaName == null ? "Các rạp nằm ở đâu?" : $"Rạp {cinemaName} nằm ở đâu?" },
            new() { Intent = TicketPrice, Category = "pricing", Question = "Giá vé hiện tại bao nhiêu?" },
            new() { Intent = ActivePromotions, Category = "promotion", Question = promotionCode == null ? "Hiện có khuyến mãi nào?" : $"Khuyến mãi {promotionCode} áp dụng như thế nào?" }
        };

        return suggestions;
    }

    public async Task<IReadOnlyList<ChatbotMovieDto>> GetUpcomingMoviesAsync(int limit = 10)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        return await _context.Movies.AsNoTracking()
            .Where(m => MovieStatus.ComingSoonStatuses.Contains(m.Status)
                || (m.ReleaseDate.HasValue && m.ReleaseDate > today))
            .OrderBy(m => m.ReleaseDate)
            .ThenBy(m => m.Title)
            .Take(Math.Clamp(limit, 1, 100))
            .Select(m => new ChatbotMovieDto
            {
                MovieId = m.MovieId,
                Title = m.Title,
                Status = m.Status,
                ReleaseDate = m.ReleaseDate,
                EndDate = m.EndDate,
                Duration = m.Duration,
                AgeRating = m.AgeRating,
                PosterUrl = m.PosterUrl,
                TrailerUrl = m.TrailerUrl
            })
            .ToListAsync();
    }

    public async Task<IReadOnlyList<ChatbotMovieDto>> GetNowShowingMoviesAsync(int limit = 10)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        return await _context.Movies.AsNoTracking()
            .Where(m => MovieStatus.NowShowingStatuses.Contains(m.Status)
                && (!m.ReleaseDate.HasValue || m.ReleaseDate <= today)
                && (!m.EndDate.HasValue || m.EndDate >= today))
            .OrderBy(m => m.Title)
            .Take(Math.Clamp(limit, 1, 100))
            .Select(m => new ChatbotMovieDto
            {
                MovieId = m.MovieId,
                Title = m.Title,
                Status = m.Status,
                ReleaseDate = m.ReleaseDate,
                EndDate = m.EndDate,
                Duration = m.Duration,
                AgeRating = m.AgeRating,
                PosterUrl = m.PosterUrl,
                TrailerUrl = m.TrailerUrl
            })
            .ToListAsync();
    }

    public async Task<IReadOnlyList<ChatbotPromotionDto>> GetActivePromotionsAsync(int limit = 20)
    {
        var now = DateTime.Now;
        return await _context.Discounts.AsNoTracking()
            .Where(d => d.IsActive && d.StartDate <= now
                && (d.EndDate == null || d.EndDate >= now)
                && (d.MaxUsageTotal == null || d.UsedCount < d.MaxUsageTotal))
            .OrderBy(d => d.EndDate)
            .ThenBy(d => d.DiscountCode)
            .Take(Math.Clamp(limit, 1, 100))
            .Select(d => new ChatbotPromotionDto
            {
                DiscountId = d.DiscountId,
                Code = d.DiscountCode,
                Description = d.Description,
                DiscountType = d.DiscountType,
                DiscountValue = d.DiscountValue,
                MinimumOrderAmount = d.MinOrderAmount,
                MaximumUsagePerUser = d.MaxUsagePerUser,
                StartDate = d.StartDate,
                EndDate = d.EndDate
            })
            .ToListAsync();
    }

    public async Task<IReadOnlyList<ChatbotShowtimeDto>> GetShowtimesAsync(ChatbotShowtimeQueryDto query)
    {
        var now = DateTime.Now;
        var showtimes = _context.Showtimes.AsNoTracking()
            .Where(s => s.Status == ShowtimeMessages.StatusActive
                && s.Room.IsActive
                && s.Room.Cinema.IsActive);

        if (query.MovieId.HasValue)
            showtimes = showtimes.Where(s => s.MovieId == query.MovieId.Value);
        if (query.CinemaId.HasValue)
            showtimes = showtimes.Where(s => s.Room.CinemaId == query.CinemaId.Value);

        if (query.Date.HasValue)
        {
            var start = query.Date.Value.ToDateTime(TimeOnly.MinValue);
            var end = start.AddDays(1);
            showtimes = showtimes.Where(s => s.StartTime >= start && s.StartTime < end);
        }
        else
        {
            showtimes = showtimes.Where(s => s.StartTime >= now);
        }

        return await showtimes
            .OrderBy(s => s.StartTime)
            .Take(Math.Clamp(query.Limit, 1, 100))
            .Select(s => new ChatbotShowtimeDto
            {
                ShowtimeId = s.ShowTimeId,
                MovieId = s.MovieId,
                MovieTitle = s.Movie.Title,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                BasePrice = s.BasePrice,
                RoomId = s.RoomId,
                RoomName = s.Room.RoomName,
                CinemaId = s.Room.CinemaId,
                CinemaName = s.Room.Cinema.CinemaName,
                CinemaAddress = s.Room.Cinema.Address
            })
            .ToListAsync();
    }

    public Task<ChatbotFeedbackResponseDto> SubmitFeedbackAsync(ChatbotFeedbackRequestDto request)
    {
        var receivedAt = DateTime.UtcNow;
        var feedbackId = Guid.NewGuid();
        _logger.LogInformation(
            "Chatbot feedback {FeedbackId}: Intent={Intent}, Helpful={IsHelpful}, Question={Question}, Comment={Comment}",
            feedbackId, request.Intent.Trim(), request.IsHelpful, request.Question.Trim(), request.Comment?.Trim());

        return Task.FromResult(new ChatbotFeedbackResponseDto
        {
            FeedbackId = feedbackId,
            Accepted = true,
            Message = "Cảm ơn bạn đã gửi phản hồi.",
            ReceivedAt = receivedAt
        });
    }

    private async Task<ChatbotResponseDto> BuildActivePromotionsResponseAsync()
    {
        var now = DateTime.Now;
        var promotions = await _context.Discounts.AsNoTracking()
            .Where(d => d.IsActive
                && d.StartDate <= now
                && (d.EndDate == null || d.EndDate >= now)
                && (d.MaxUsageTotal == null || d.UsedCount < d.MaxUsageTotal))
            .OrderBy(d => d.EndDate)
            .Select(d => new
            {
                name = d.DiscountCode,
                d.Description,
                d.DiscountType,
                d.DiscountValue,
                d.MinOrderAmount,
                d.MaxUsagePerUser,
                d.EndDate
            })
            .ToListAsync();

        if (promotions.Count == 0)
            return Response(true, ActivePromotions, "Hiện tại chưa có chương trình khuyến mãi.");

        var data = promotions.Select(p => (object)new
        {
            name = p.name,
            description = p.Description,
            discount = FormatDiscount(p.DiscountType, p.DiscountValue),
            condition = BuildPromotionCondition(p.MinOrderAmount, p.MaxUsagePerUser),
            scope = "Toàn hệ thống",
            expiresAt = p.EndDate
        }).ToArray();

        var lines = promotions.Select((p, index) =>
            $"{index + 1}. {p.name}: {FormatDiscount(p.DiscountType, p.DiscountValue)}" +
            $"{(string.IsNullOrWhiteSpace(p.Description) ? string.Empty : $" — {p.Description}")}" +
            $". {BuildPromotionCondition(p.MinOrderAmount, p.MaxUsagePerUser)}" +
            $". Hết hạn: {(p.EndDate.HasValue ? p.EndDate.Value.ToString("dd/MM/yyyy HH:mm") : "không giới hạn")}");

        return Response(true, ActivePromotions,
            "Các khuyến mãi đang áp dụng:\n" + string.Join("\n", lines), data);
    }

    private async Task<ChatbotResponseDto> BuildUpcomingMoviesResponseAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var movies = await _context.Movies.AsNoTracking()
            .Where(m => MovieStatus.ComingSoonStatuses.Contains(m.Status)
                || (m.ReleaseDate.HasValue && m.ReleaseDate > today))
            .OrderBy(m => m.ReleaseDate)
            .ThenBy(m => m.Title)
            .Take(10)
            .Select(m => new
            {
                m.MovieId,
                m.Title,
                m.ReleaseDate,
                m.Duration,
                m.AgeRating,
                m.PosterUrl
            })
            .ToListAsync();

        if (movies.Count == 0)
            return Response(true, UpcomingMovies, "Hiện tại chưa có phim sắp chiếu trong hệ thống.");

        var data = movies.Select(m => (object)new
        {
            movieId = m.MovieId,
            title = m.Title,
            releaseDate = m.ReleaseDate,
            duration = m.Duration,
            ageRating = m.AgeRating,
            posterUrl = m.PosterUrl
        }).ToArray();
        var lines = movies.Select((m, index) =>
            $"{index + 1}. {m.Title} — khởi chiếu {FormatDate(m.ReleaseDate)}, {m.Duration} phút, độ tuổi {m.AgeRating ?? "chưa cập nhật"}");

        return Response(true, UpcomingMovies, "Các phim sắp chiếu:\n" + string.Join("\n", lines), data);
    }

    private async Task<ChatbotResponseDto> BuildNowShowingMoviesResponseAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var movies = await _context.Movies.AsNoTracking()
            .Where(m => MovieStatus.NowShowingStatuses.Contains(m.Status)
                && (!m.ReleaseDate.HasValue || m.ReleaseDate <= today)
                && (!m.EndDate.HasValue || m.EndDate >= today))
            .OrderBy(m => m.Title)
            .Take(10)
            .Select(m => new
            {
                m.MovieId,
                m.Title,
                m.Duration,
                m.AgeRating,
                m.PosterUrl,
                m.ReleaseDate
            })
            .ToListAsync();

        if (movies.Count == 0)
            return Response(true, NowShowingMovies, "Hiện tại chưa có phim đang chiếu trong hệ thống.");

        var data = movies.Cast<object>().ToArray();
        var lines = movies.Select((m, index) =>
            $"{index + 1}. {m.Title} — {m.Duration} phút, độ tuổi {m.AgeRating ?? "chưa cập nhật"}");
        return Response(true, NowShowingMovies, "Các phim đang chiếu:\n" + string.Join("\n", lines), data);
    }

    private async Task<ChatbotResponseDto> GetMovieShowtimesAsync(string question)
    {
        var movies = await _context.Movies.AsNoTracking()
            .Select(m => new { m.MovieId, m.Title })
            .ToListAsync();
        var movie = FindBestTextMatch(question, movies, x => x.Title);

        if (movie == null)
            return Response(false, MovieShowtimes,
                "Mình chưa nhận diện được tên phim. Bạn hãy hỏi theo mẫu: “Moana có suất chiếu lúc mấy giờ?”.");

        var now = DateTime.Now;
        var requestedDate = ExtractRequestedDate(question);
        var cinemas = await _context.Cinemas.AsNoTracking()
            .Where(c => c.IsActive)
            .Select(c => new { c.CinemaId, c.CinemaName })
            .ToListAsync();
        var requestedCinema = FindBestTextMatch(question, cinemas, x => x.CinemaName);

        var query = _context.Showtimes.AsNoTracking()
            .Where(s => s.MovieId == movie.MovieId
                && s.Status == ShowtimeMessages.StatusActive
                && s.StartTime >= now);

        if (requestedDate.HasValue)
        {
            var from = requestedDate.Value.Date;
            var to = from.AddDays(1);
            query = query.Where(s => s.StartTime >= from && s.StartTime < to);
        }
        if (requestedCinema != null)
            query = query.Where(s => s.Room.CinemaId == requestedCinema.CinemaId);

        var showtimes = await query
            .OrderBy(s => s.StartTime)
            .Take(20)
            .Select(s => new
            {
                s.ShowTimeId,
                movieId = s.MovieId,
                movieTitle = s.Movie.Title,
                s.StartTime,
                s.EndTime,
                room = s.Room.RoomName,
                cinema = s.Room.Cinema.CinemaName,
                address = s.Room.Cinema.Address,
                basePrice = s.BasePrice
            })
            .ToListAsync();

        if (showtimes.Count == 0)
            return Response(true, MovieShowtimes,
                $"Hiện chưa có suất chiếu sắp tới phù hợp cho phim {movie.Title}.");

        var lines = showtimes.Select((s, index) =>
            $"{index + 1}. {s.StartTime:dd/MM/yyyy HH:mm} — {s.cinema}, {s.room} — giá cơ bản {Money(s.basePrice)}");
        return Response(true, MovieShowtimes,
            $"Suất chiếu sắp tới của {movie.Title}:\n" + string.Join("\n", lines), showtimes.Cast<object>().ToArray());
    }

    private async Task<ChatbotResponseDto> GetTicketPricesAsync(string question)
    {
        var movieNames = await _context.Movies.AsNoTracking()
            .Select(m => new { m.MovieId, m.Title })
            .ToListAsync();
        var requestedMovie = FindBestTextMatch(question, movieNames, x => x.Title);
        if (requestedMovie != null)
        {
            var now = DateTime.Now;
            var showtimePrices = await _context.Showtimes.AsNoTracking()
                .Where(s => s.MovieId == requestedMovie.MovieId
                    && s.Status == ShowtimeMessages.StatusActive
                    && s.StartTime >= now)
                .OrderBy(s => s.StartTime)
                .Take(10)
                .Select(s => new
                {
                    s.ShowTimeId,
                    movieTitle = s.Movie.Title,
                    s.StartTime,
                    cinema = s.Room.Cinema.CinemaName,
                    room = s.Room.RoomName,
                    s.BasePrice
                })
                .ToListAsync();

            if (showtimePrices.Count > 0)
            {
                var showtimeLines = showtimePrices.Select((s, index) =>
                    $"{index + 1}. {s.StartTime:dd/MM/yyyy HH:mm} — {s.cinema}, {s.room}: giá cơ bản {Money(s.BasePrice)}");
                return Response(true, TicketPrice,
                    $"Giá các suất chiếu sắp tới của {requestedMovie.Title}:\n" + string.Join("\n", showtimeLines) +
                    "\nGiá thanh toán thực tế còn phụ thuộc loại ghế và ưu đãi.",
                    showtimePrices.Cast<object>().ToArray());
            }
        }

        var today = DateOnly.FromDateTime(DateTime.Today);
        var prices = await _context.Ticketpricings.AsNoTracking()
            .Where(p => p.IsActive && p.EffectFrom <= today && (p.EffectTo == null || p.EffectTo >= today))
            .OrderBy(p => p.SeatType)
            .ThenBy(p => p.RoomType)
            .ThenBy(p => p.DayType)
            .Select(p => new
            {
                p.PricingId,
                p.RoomId,
                RoomName = p.Room != null ? p.Room.RoomName : null,
                p.SeatType,
                p.RoomType,
                p.DayType,
                p.Price,
                p.EffectFrom,
                p.EffectTo
            })
            .ToListAsync();

        var normalizedQuestion = Normalize(question);
        var mentionedSeatType = prices.Select(p => p.SeatType).Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(x => x!.Length)
            .FirstOrDefault(x => normalizedQuestion.Contains(Normalize(x!)));
        if (mentionedSeatType != null)
            prices = prices.Where(p => string.Equals(p.SeatType, mentionedSeatType, StringComparison.OrdinalIgnoreCase)).ToList();

        if (prices.Count == 0)
            return Response(true, TicketPrice, "Hiện chưa có cấu hình giá vé phù hợp đang áp dụng.");

        var lines = prices.Select((p, index) =>
            $"{index + 1}. Ghế {p.SeatType ?? "tiêu chuẩn"}, phòng {p.RoomType ?? "tất cả"}, {p.DayType ?? "mọi ngày"}: {Money(p.Price)}");
        return Response(true, TicketPrice,
            "Giá vé đang áp dụng:\n" + string.Join("\n", lines) +
            "\nGiá thanh toán thực tế có thể phụ thuộc suất chiếu và ưu đãi được áp dụng.",
            prices.Cast<object>().ToArray());
    }

    private async Task<ChatbotResponseDto> GetCinemaInformationAsync(string question)
    {
        var cinemas = await _context.Cinemas.AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.CinemaName)
            .Select(c => new
            {
                c.CinemaId,
                c.CinemaName,
                c.Address,
                c.Phone,
                area = c.Area.AreaName
            })
            .ToListAsync();
        var match = FindBestTextMatch(question, cinemas, x => x.CinemaName);
        var selected = match == null ? cinemas : cinemas.Where(c => c.CinemaId == match.CinemaId).ToList();

        if (selected.Count == 0)
            return Response(true, CinemaInformation, "Hiện chưa có thông tin rạp đang hoạt động trong hệ thống.");

        var lines = selected.Select((c, index) =>
            $"{index + 1}. {c.CinemaName} — {c.Address}, khu vực {c.area}" +
            $"{(string.IsNullOrWhiteSpace(c.Phone) ? string.Empty : $" — Điện thoại: {c.Phone}")}");
        return Response(true, CinemaInformation,
            (match == null ? "Thông tin các rạp đang hoạt động:\n" : $"Thông tin {match.CinemaName}:\n") + string.Join("\n", lines),
            selected.Cast<object>().ToArray());
    }

    private static ChatbotResponseDto Response(bool success, string intent, string message, object[]? data = null) => new()
    {
        Success = success,
        Intent = intent,
        Message = message,
        Data = data ?? Array.Empty<object>()
    };

    private static string FormatDate(DateOnly? date) => date?.ToString("dd/MM/yyyy", VietnameseCulture) ?? "chưa cập nhật";
    private static string Money(decimal amount) => amount.ToString("N0", VietnameseCulture) + " đ";
    private static string FormatDiscount(string type, decimal value) =>
        type.Equals("Percent", StringComparison.OrdinalIgnoreCase) ? $"giảm {value:0.##}%" : $"giảm {Money(value)}";
    private static string BuildPromotionCondition(decimal minimum, int maxPerUser) =>
        $"Đơn tối thiểu {Money(minimum)}, tối đa {maxPerUser} lần/khách";

    private static bool ContainsAny(string text, params string[] values) => values.Any(text.Contains);

    private static string Normalize(string value)
    {
        var decomposed = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                builder.Append(character == 'đ' ? 'd' : character);
        }
        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private static T? FindBestTextMatch<T>(string question, IEnumerable<T> values, Func<T, string> getName) where T : class
    {
        var normalizedQuestion = Normalize(question);
        var exactMatch = values
            .Where(value => normalizedQuestion.Contains(Normalize(getName(value))))
            .OrderByDescending(value => getName(value).Length)
            .FirstOrDefault();

        if (exactMatch != null)
            return exactMatch;

        var questionTokens = GetMeaningfulTokens(normalizedQuestion);
        return values
            .Select(value => new
            {
                Value = value,
                Score = GetMeaningfulTokens(Normalize(getName(value))).Count(questionTokens.Contains)
            })
            .Where(match => match.Score > 0)
            .OrderByDescending(match => match.Score)
            .ThenByDescending(match => getName(match.Value).Length)
            .Select(match => match.Value)
            .FirstOrDefault();
    }

    private static HashSet<string> GetMeaningfulTokens(string text)
    {
        string[] ignoredTokens =
        {
            "phim", "rap", "cinema", "cinemas", "hcm", "co", "cua", "cho", "toi",
            "suat", "chieu", "luc", "may", "gio", "ngay", "hom", "nay", "mai", "khong",
            "thong", "tin", "chi", "nhanh", "nam", "o", "dau"
        };

        return text.Split(new[] { ' ', '-', '_', ':', ',', '.', '?', '!', '/', '\\' },
                StringSplitOptions.RemoveEmptyEntries)
            .Where(token => token.Length >= 3 && !ignoredTokens.Contains(token))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static DateTime? ExtractRequestedDate(string question)
    {
        var normalized = Normalize(question);
        if (normalized.Contains("ngay mai")) return DateTime.Today.AddDays(1);
        if (normalized.Contains("hom nay")) return DateTime.Today;

        var match = System.Text.RegularExpressions.Regex.Match(question, @"\b(?<day>\d{1,2})[/-](?<month>\d{1,2})(?:[/-](?<year>\d{4}))?\b");
        if (!match.Success) return null;

        var day = int.Parse(match.Groups["day"].Value);
        var month = int.Parse(match.Groups["month"].Value);
        var year = match.Groups["year"].Success ? int.Parse(match.Groups["year"].Value) : DateTime.Today.Year;
        return DateTime.TryParseExact($"{day:00}/{month:00}/{year}", "dd/MM/yyyy", CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var result) ? result : null;
    }
}
