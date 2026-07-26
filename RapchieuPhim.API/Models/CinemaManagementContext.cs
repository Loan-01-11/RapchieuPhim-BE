using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace RapchieuPhim.API.Models;

public partial class CinemaManagementContext : DbContext
{
    public CinemaManagementContext()
    {
    }

    public CinemaManagementContext(DbContextOptions<CinemaManagementContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Area> Areas { get; set; }

    public virtual DbSet<Booking> Bookings { get; set; }

    public virtual DbSet<Cinema> Cinemas { get; set; }

    public virtual DbSet<Combo> Combos { get; set; }

    public virtual DbSet<Combofoodmapping> Combofoodmappings { get; set; }

    public virtual DbSet<Discount> Discounts { get; set; }

    public virtual DbSet<Food> Foods { get; set; }

    public virtual DbSet<Movie> Movies { get; set; }

    public virtual DbSet<Moviecategory> Moviecategories { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<Orderitem> Orderitems { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Review> Reviews { get; set; }



    public virtual DbSet<Room> Rooms { get; set; }

    public virtual DbSet<Seat> Seats { get; set; }

    public virtual DbSet<Showtime> Showtimes { get; set; }

    public virtual DbSet<Staffreport> Staffreports { get; set; }

    public virtual DbSet<Staffshift> Staffshifts { get; set; }

    public virtual DbSet<Ticket> Tickets { get; set; }

    public virtual DbSet<Ticketpricing> Ticketpricings { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<Userdiscountusage> Userdiscountusages { get; set; }

    public virtual DbSet<VwAvailableSeat> VwAvailableSeats { get; set; }

    public virtual DbSet<VwBookingDetail> VwBookingDetails { get; set; }

    public virtual DbSet<VwRevenueByMovie> VwRevenueByMovies { get; set; }

    public virtual DbSet<VwShowtimeDetail> VwShowtimeDetails { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)

       => optionsBuilder.UseSqlServer(GetConnectionString());
    private string GetConnectionString()
    {
        IConfiguration config = new ConfigurationBuilder()
             .SetBasePath(AppContext.BaseDirectory)
                    .AddJsonFile("appsettings.json", true, true)
                    .Build();
        var strConn = config["ConnectionStrings:DefaultConnection"];

        return strConn;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseCollation("Vietnamese_CI_AS");

        modelBuilder.Entity<Area>(entity =>
        {
            entity.ToTable("AREAS");

            entity.HasIndex(e => e.AreaName, "UQ_AREAS_NAME").IsUnique();

            entity.Property(e => e.AreaName).HasMaxLength(100);
        });

        modelBuilder.Entity<Booking>(entity =>
        {
            entity.ToTable("BOOKINGS");

            entity.HasIndex(e => e.ShowTimeId, "IDX_BK_SHOWTIME");

            entity.HasIndex(e => e.Status, "IDX_BK_STATUS");

            entity.HasIndex(e => e.UserId, "IDX_BK_USER");

            entity.HasIndex(e => new { e.ShowTimeId, e.SeatId }, "UQ_BOOKING_SEAT").IsUnique();

            entity.Property(e => e.BookingDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.BookingType)
                .HasMaxLength(20)
                .HasDefaultValue("Online");
            entity.Property(e => e.DiscountAmt).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .HasDefaultValue("Pending");
            entity.Property(e => e.TicketPrice).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(12, 2)");

            entity.HasOne(d => d.Discount).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.DiscountId)
                .HasConstraintName("FK_BK_DISCOUNT");

            entity.HasOne(d => d.Seat).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.SeatId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BK_SEAT");

            entity.HasOne(d => d.ShowTime).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.ShowTimeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BK_SHOWTIME");

            entity.HasOne(d => d.Staff).WithMany(p => p.BookingStaffs)
                .HasForeignKey(d => d.StaffId)
                .HasConstraintName("FK_BK_STAFF");

            entity.HasOne(d => d.User).WithMany(p => p.BookingUsers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BK_USER");
        });

        modelBuilder.Entity<Cinema>(entity =>
        {
            entity.ToTable("CINEMAS");

            entity.HasIndex(e => e.AreaId, "IDX_CINEMAS_AREA");

            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.CinemaName).HasMaxLength(150);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Phone).HasMaxLength(20);

            entity.HasOne(d => d.Area).WithMany(p => p.Cinemas)
                .HasForeignKey(d => d.AreaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CINEMAS_AREA");
        });

        modelBuilder.Entity<Combo>(entity =>
        {
            entity.ToTable("COMBOS");

            entity.Property(e => e.ComboName).HasMaxLength(150);
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.Property(e => e.IsAvailable).HasDefaultValue(true);
            entity.Property(e => e.Price).HasColumnType("decimal(12, 2)");
        });

        modelBuilder.Entity<Combofoodmapping>(entity =>
        {
            entity.HasKey(e => new { e.ComboId, e.FoodId });

            entity.ToTable("COMBOFOODMAPPING");

            entity.Property(e => e.Quantity).HasDefaultValue(1);

            entity.HasOne(d => d.Combo).WithMany(p => p.Combofoodmappings)
                .HasForeignKey(d => d.ComboId)
                .HasConstraintName("FK_CFM_COMBO");

            entity.HasOne(d => d.Food).WithMany(p => p.Combofoodmappings)
                .HasForeignKey(d => d.FoodId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CFM_FOOD");
        });

        modelBuilder.Entity<Discount>(entity =>
        {
            entity.ToTable("DISCOUNTS");

            entity.HasIndex(e => e.DiscountCode, "IDX_DC_CODE");

            entity.HasIndex(e => e.DiscountCode, "UQ_DISCOUNT_CODE").IsUnique();

            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.DiscountCode).HasMaxLength(50);
            entity.Property(e => e.DiscountType)
                .HasMaxLength(20)
                .HasDefaultValue("Percent");
            entity.Property(e => e.DiscountValue).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.EndDate).HasColumnType("datetime");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.MaxUsagePerUser).HasDefaultValue(1);
            entity.Property(e => e.MinOrderAmount).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.StartDate).HasColumnType("datetime");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Discounts)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DC_USER");
        });

        modelBuilder.Entity<Food>(entity =>
        {
            entity.ToTable("FOODS");

            entity.Property(e => e.Category).HasMaxLength(50);
            entity.Property(e => e.FoodName).HasMaxLength(150);
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.Property(e => e.IsAvailable).HasDefaultValue(true);
            entity.Property(e => e.Price).HasColumnType("decimal(12, 2)");
        });

        modelBuilder.Entity<Movie>(entity =>
        {
            entity.ToTable("MOVIES");

            entity.HasIndex(e => e.ReleaseDate, "IDX_MOVIES_RELEASE");

            entity.HasIndex(e => e.Status, "IDX_MOVIES_STATUS");

            entity.Property(e => e.AgeRating).HasMaxLength(10);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Director).HasMaxLength(200);
            entity.Property(e => e.Language).HasMaxLength(50);
            entity.Property(e => e.PosterUrl).HasMaxLength(500);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Active");
            entity.Property(e => e.Subtitles).HasMaxLength(50);
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.TrailerUrl).HasMaxLength(500);

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Movies)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MOVIES_USER");

            entity.HasMany(d => d.Categories).WithMany(p => p.Movies)
                .UsingEntity<Dictionary<string, object>>(
                    "Moviecategorymapping",
                    r => r.HasOne<Moviecategory>().WithMany()
                        .HasForeignKey("CategoryId")
                        .HasConstraintName("FK_MCM_CATEGORY"),
                    l => l.HasOne<Movie>().WithMany()
                        .HasForeignKey("MovieId")
                        .HasConstraintName("FK_MCM_MOVIE"),
                    j =>
                    {
                        j.HasKey("MovieId", "CategoryId");
                        j.ToTable("MOVIECATEGORYMAPPING");
                    });
        });

        modelBuilder.Entity<Moviecategory>(entity =>
        {
            entity.HasKey(e => e.CategoryId);

            entity.ToTable("MOVIECATEGORIES");

            entity.HasIndex(e => e.CategoryName, "UQ_CATEGORY_NAME").IsUnique();

            entity.Property(e => e.CategoryName).HasMaxLength(100);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("ORDERS");

            entity.HasIndex(e => e.BookingId, "IDX_ORD_BOOKING");

            entity.HasIndex(e => e.UserId, "IDX_ORD_USER");

            entity.Property(e => e.OrderDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.OrderType)
                .HasMaxLength(20)
                .HasDefaultValue("Online");
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .HasDefaultValue("Pending");
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(12, 2)");

            entity.HasOne(d => d.Booking).WithMany(p => p.Orders)
                .HasForeignKey(d => d.BookingId)
                .HasConstraintName("FK_ORD_BOOKING");

            entity.HasOne(d => d.Discount).WithMany(p => p.Orders)
                .HasForeignKey(d => d.DiscountId)
                .HasConstraintName("FK_ORD_DISCOUNT");

            entity.HasOne(d => d.Staff).WithMany(p => p.OrderStaffs)
                .HasForeignKey(d => d.StaffId)
                .HasConstraintName("FK_ORD_STAFF");

            entity.HasOne(d => d.User).WithMany(p => p.OrderUsers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ORD_USER");
        });

        modelBuilder.Entity<Orderitem>(entity =>
        {
            entity.ToTable("ORDERITEMS");

            entity.HasIndex(e => e.OrderId, "IDX_OI_ORDER");

            entity.Property(e => e.Quantity).HasDefaultValue(1);
            entity.Property(e => e.Subtotal).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(12, 2)");

            entity.HasOne(d => d.Combo).WithMany(p => p.Orderitems)
                .HasForeignKey(d => d.ComboId)
                .HasConstraintName("FK_OI_COMBO");

            entity.HasOne(d => d.Food).WithMany(p => p.Orderitems)
                .HasForeignKey(d => d.FoodId)
                .HasConstraintName("FK_OI_FOOD");

            entity.HasOne(d => d.Order).WithMany(p => p.Orderitems)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK_OI_ORDER");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.ToTable("PAYMENTS");

            entity.HasIndex(e => e.BookingId, "IDX_PAY_BOOKING");

            entity.HasIndex(e => e.OrderId, "IDX_PAY_ORDER");

            entity.HasIndex(e => e.PaymentStatus, "IDX_PAY_STATUS");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.DiscountAmt).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.PaidAt).HasColumnType("datetime");
            entity.Property(e => e.PaymentMethod).HasMaxLength(50);
            entity.Property(e => e.PaymentStatus)
                .HasMaxLength(30)
                .HasDefaultValue("Pending");
            entity.Property(e => e.SubTotal).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.TransactionId).HasMaxLength(100);

            entity.HasOne(d => d.Booking).WithMany(p => p.Payments)
                .HasForeignKey(d => d.BookingId)
                .HasConstraintName("FK_PAY_BOOKING");

            entity.HasOne(d => d.Order).WithMany(p => p.Payments)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK_PAY_ORDER");

            entity.HasOne(d => d.Staff).WithMany(p => p.PaymentStaffs)
                .HasForeignKey(d => d.StaffId)
                .HasConstraintName("FK_PAY_STAFF");

            entity.HasOne(d => d.User).WithMany(p => p.PaymentUsers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PAY_USER");
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.ToTable("REVIEWS");

            entity.HasIndex(e => e.MovieId, "IDX_REV_MOVIE");

            entity.HasIndex(e => new { e.UserId, e.MovieId }, "UQ_USER_MOVIE_REVIEW").IsUnique();

            entity.Property(e => e.ReviewDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Movie).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.MovieId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_REV_MOVIE");

            entity.HasOne(d => d.User).WithMany(p => p.Reviews)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_REV_USER");
        });



        modelBuilder.Entity<Room>(entity =>
        {
            entity.ToTable("ROOMS");

            entity.HasIndex(e => e.CinemaId, "IDX_ROOMS_CINEMA");

            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.RoomName).HasMaxLength(100);
            entity.Property(e => e.RoomType).HasMaxLength(50);

            entity.HasOne(d => d.Cinema).WithMany(p => p.Rooms)
                .HasForeignKey(d => d.CinemaId)
                .HasConstraintName("FK_ROOMS_CINEMA");
        });

        modelBuilder.Entity<Seat>(entity =>
        {
            entity.ToTable("SEATS");

            entity.HasIndex(e => e.RoomId, "IDX_SEATS_ROOM");

            entity.HasIndex(e => new { e.RoomId, e.SeatNumber }, "UQ_SEATS").IsUnique();

            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.SeatNumber).HasMaxLength(10);
            entity.Property(e => e.SeatRow)
                .HasMaxLength(2)
                .IsFixedLength();
            entity.Property(e => e.SeatType).HasMaxLength(30);

            entity.HasOne(d => d.Room).WithMany(p => p.Seats)
                .HasForeignKey(d => d.RoomId)
                .HasConstraintName("FK_SEATS_ROOM");
        });

        modelBuilder.Entity<Showtime>(entity =>
        {
            entity.ToTable("SHOWTIMES");

            entity.HasIndex(e => e.MovieId, "IDX_ST_MOVIE");

            entity.HasIndex(e => e.RoomId, "IDX_ST_ROOM");

            entity.HasIndex(e => e.StartTime, "IDX_ST_START");

            entity.Property(e => e.BasePrice).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.EndTime).HasColumnType("datetime");
            entity.Property(e => e.StartTime).HasColumnType("datetime");
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .HasDefaultValue("Active");

            entity.HasOne(d => d.Movie).WithMany(p => p.Showtimes)
                .HasForeignKey(d => d.MovieId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ST_MOVIE");

            entity.HasOne(d => d.Room).WithMany(p => p.Showtimes)
                .HasForeignKey(d => d.RoomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ST_ROOM");
        });

        modelBuilder.Entity<Staffreport>(entity =>
        {
            entity.HasKey(e => e.ReportId);

            entity.ToTable("STAFFREPORTS");

            entity.Property(e => e.GeneratedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Summary).HasMaxLength(1000);
            entity.Property(e => e.TotalRevenue).HasColumnType("decimal(15, 2)");

            entity.HasOne(d => d.Cinema).WithMany(p => p.Staffreports)
                .HasForeignKey(d => d.CinemaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SR_CINEMA");

            entity.HasOne(d => d.Staff).WithMany(p => p.Staffreports)
                .HasForeignKey(d => d.StaffId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SR_STAFF");
        });

        modelBuilder.Entity<Staffshift>(entity =>
        {
            entity.HasKey(e => e.ShiftId);

            entity.ToTable("STAFFSHIFTS");

            entity.HasIndex(e => new { e.StaffId, e.ShiftDate }, "IDX_SS_STAFF_DATE");

            entity.Property(e => e.ShiftEnd).HasColumnType("datetime");
            entity.Property(e => e.ShiftStart).HasColumnType("datetime");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Open");
            entity.Property(e => e.Summary).HasMaxLength(1000);
            entity.Property(e => e.TotalRevenue).HasColumnType("decimal(15, 2)");

            entity.HasOne(d => d.Cinema).WithMany(p => p.Staffshifts)
                .HasForeignKey(d => d.CinemaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SS_CINEMA");

            entity.HasOne(d => d.Staff).WithMany(p => p.Staffshifts)
                .HasForeignKey(d => d.StaffId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SS_STAFF");
        });

        modelBuilder.Entity<Ticket>(entity =>
        {
            entity.ToTable("TICKETS");

            entity.HasIndex(e => e.BookingId, "IDX_TK_BOOKING");

            entity.HasIndex(e => e.TicketCode, "IDX_TK_CODE");

            entity.HasIndex(e => e.TicketCode, "UQ_TICKET_CODE").IsUnique();

            entity.Property(e => e.IssuedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Price).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.QrCodeUrl).HasMaxLength(500);
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .HasDefaultValue("Active");
            entity.Property(e => e.TicketCode).HasMaxLength(50);

            entity.HasOne(d => d.Booking).WithMany(p => p.Tickets)
                .HasForeignKey(d => d.BookingId)
                .HasConstraintName("FK_TK_BOOKING");
        });

        modelBuilder.Entity<Ticketpricing>(entity =>
        {
            entity.HasKey(e => e.PricingId);

            entity.ToTable("TICKETPRICING");

            entity.Property(e => e.DayType).HasMaxLength(20);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Price).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.RoomType).HasMaxLength(50);
            entity.Property(e => e.SeatType).HasMaxLength(30);

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Ticketpricings)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TP_USER");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("USERS");

            entity.HasIndex(e => e.Role, "IDX_USERS_ROLE");

            entity.HasIndex(e => e.Email, "UQ_USERS_EMAIL").IsUnique();

            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.AvatarUrl).HasColumnType("nvarchar(max)");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Email).HasMaxLength(150);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.Gender).HasMaxLength(10);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.MembershipLevel).HasMaxLength(50);
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Role)
                .HasMaxLength(50)
                .HasDefaultValue("Customer");

            entity.Property(e => e.CinemaId).HasColumnName("CinemaId");
            entity.HasOne(d => d.Cinema).WithMany()
                .HasForeignKey(d => d.CinemaId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_USERS_CINEMAS");
        });

        modelBuilder.Entity<Userdiscountusage>(entity =>
        {
            entity.HasKey(e => e.UsageId);

            entity.ToTable("USERDISCOUNTUSAGE");

            entity.HasIndex(e => new { e.UserId, e.DiscountId }, "UQ_USER_DISCOUNT").IsUnique();

            entity.Property(e => e.LastUsedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.UsedCount).HasDefaultValue(1);

            entity.HasOne(d => d.Discount).WithMany(p => p.Userdiscountusages)
                .HasForeignKey(d => d.DiscountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UDU_DISCOUNT");

            entity.HasOne(d => d.User).WithMany(p => p.Userdiscountusages)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UDU_USER");
        });

        modelBuilder.Entity<VwAvailableSeat>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("VW_AVAILABLE_SEATS");

            entity.Property(e => e.MovieTitle).HasMaxLength(200);
            entity.Property(e => e.RoomName).HasMaxLength(100);
            entity.Property(e => e.SeatNumber).HasMaxLength(10);
            entity.Property(e => e.SeatType).HasMaxLength(30);
            entity.Property(e => e.StartTime).HasColumnType("datetime");
        });

        modelBuilder.Entity<VwBookingDetail>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("VW_BOOKING_DETAIL");

            entity.Property(e => e.AreaName).HasMaxLength(100);
            entity.Property(e => e.BookingDate).HasColumnType("datetime");
            entity.Property(e => e.BookingType).HasMaxLength(20);
            entity.Property(e => e.CinemaName).HasMaxLength(150);
            entity.Property(e => e.CustomerName).HasMaxLength(100);
            entity.Property(e => e.DiscountAmt).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.Email).HasMaxLength(150);
            entity.Property(e => e.MovieTitle).HasMaxLength(200);
            entity.Property(e => e.RoomName).HasMaxLength(100);
            entity.Property(e => e.RoomType).HasMaxLength(50);
            entity.Property(e => e.SeatNumber).HasMaxLength(10);
            entity.Property(e => e.SeatType).HasMaxLength(30);
            entity.Property(e => e.StartTime).HasColumnType("datetime");
            entity.Property(e => e.Status).HasMaxLength(30);
            entity.Property(e => e.TicketPrice).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(12, 2)");
        });

        modelBuilder.Entity<VwRevenueByMovie>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("VW_REVENUE_BY_MOVIE");

            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.TotalRevenue).HasColumnType("decimal(38, 2)");
        });

        modelBuilder.Entity<VwShowtimeDetail>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("VW_SHOWTIME_DETAIL");

            entity.Property(e => e.AgeRating).HasMaxLength(10);
            entity.Property(e => e.AreaName).HasMaxLength(100);
            entity.Property(e => e.BasePrice).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.CinemaName).HasMaxLength(150);
            entity.Property(e => e.EndTime).HasColumnType("datetime");
            entity.Property(e => e.MovieTitle).HasMaxLength(200);
            entity.Property(e => e.PosterUrl).HasMaxLength(500);
            entity.Property(e => e.RoomName).HasMaxLength(100);
            entity.Property(e => e.RoomType).HasMaxLength(50);
            entity.Property(e => e.StartTime).HasColumnType("datetime");
            entity.Property(e => e.Status).HasMaxLength(30);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
