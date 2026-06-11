using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RapchieuPhim.API.Models;
using RapchieuPhim.API.Services;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add CORS – allow all origins, methods, and headers (for development)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

// Add controllers with JSON options to prevent circular reference errors
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"]!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = builder.Configuration["Jwt:Issuer"],
            ValidAudience            = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "Cinema Management API",
        Version     = "v1",
        Description = "RESTful API for Cinema Management System"
    });

    // Allow entering Bearer token in Swagger UI
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name         = "Authorization",
        Type         = SecuritySchemeType.Http,
        Scheme       = "bearer",
        BearerFormat = "JWT",
        In           = ParameterLocation.Header,
        Description  = "Enter your JWT token. Example: eyJhbGci..."
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddDbContext<RapchieuPhim.API.Models.CinemaManagementContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Memory Cache – lưu OTP quên mật khẩu
builder.Services.AddMemoryCache();

// Email Service
builder.Services.AddScoped<IEmailService, EmailService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Cinema Management API v1"));
}

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// ────────── KHỞI TẠO TÀI KHOẢN ADMIN MẶC ĐỊNH ──────────
using (var scope = app.Services.CreateScope()) // 1. Mở một không gian cô lập (Scope)
{
    var services = scope.ServiceProvider;
    try
    {
        // 2. Gọi tầng kết nối Database (DbContext) ra để dùng
        var context = services.GetRequiredService<RapchieuPhim.API.Models.CinemaManagementContext>();

        // 3. Quét database xem có ông nào đang giữ quyền "Admin" chưa
        var hasAdmin = await context.Users.AnyAsync(u => u.Role == "Admin");

        // 4. Nếu CHƯA CÓ tài khoản Admin nào, tiến hành tạo mới
        if (!hasAdmin)
        {
            var defaultAdmin = new User
            {
                FullName = "Hệ Thống Admin",
                Email = "admin@123.com", // 📧 Dùng tài khoản này đăng nhập
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"), // 🔑 Mật khẩu đã băm bảo mật
                Phone = "0123456789",
                DateOfBirth = new DateOnly(2000, 1, 1),
                Role = "Admin", // Chuỗi chữ gán quyền
                RewardPoint = 0,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            context.Users.Add(defaultAdmin); // Thêm vào giỏ
            await context.SaveChangesAsync(); // Chốt lưu xuống SQL Server

            // In một dòng chữ màu xanh ra màn hình đen (Console) để báo hiệu cho bạn biết
            Console.WriteLine("➔ [SEED DATA]: Khởi tạo thành công tài khoản Admin mặc định!");
        }
    }
    catch (Exception ex)
    {
        // Nếu database chưa được tạo hoặc bị lỗi kết nối, hệ thống sẽ ghi log lại chứ không làm sập App
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Lỗi cắm mồi dữ liệu Admin.");
    }
}

app.Run();
