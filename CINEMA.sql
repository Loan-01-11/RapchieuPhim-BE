-- ============================================================
--   Cinema Management Database 
-- ============================================================

USE master;
GO

IF EXISTS (SELECT name FROM sys.databases WHERE name = 'CinemaManagement')
BEGIN
    ALTER DATABASE CinemaManagement SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE CinemaManagement;
END
GO

CREATE DATABASE CinemaManagement
    COLLATE Vietnamese_CI_AS;
GO

USE CinemaManagement;
GO

-- ============================================================
-- 1. ROLES  (UC-16 Management account - role-based access)
--    Values: Customer | Staff | Admin
-- ============================================================
CREATE TABLE ROLES (
    RoleId   INT          NOT NULL IDENTITY(1,1),
    RoleName NVARCHAR(50) NOT NULL,
    CONSTRAINT PK_ROLES      PRIMARY KEY (RoleId),
    CONSTRAINT UQ_ROLES_NAME UNIQUE (RoleName)
);
GO

-- ============================================================
-- 2. AREAS  (UC-03 Select area - filter cinemas by city/region)
-- ============================================================
CREATE TABLE AREAS (
    AreaId   INT           NOT NULL IDENTITY(1,1),
    AreaName NVARCHAR(100) NOT NULL,
    CONSTRAINT PK_AREAS      PRIMARY KEY (AreaId),
    CONSTRAINT UQ_AREAS_NAME UNIQUE (AreaName)
);
GO

-- ============================================================
-- 3. USERS  (UC-01 Login | UC-02 Register | UC-16 Management account)
-- ============================================================
CREATE TABLE USERS (
    UserId          INT           NOT NULL IDENTITY(1,1),
    FullName        NVARCHAR(100) NOT NULL,
    Email           NVARCHAR(150) NOT NULL,
    PasswordHash    NVARCHAR(255) NOT NULL,
    Phone           NVARCHAR(20)  NULL,
    AvatarUrl       NVARCHAR(500) NULL,
    DateOfBirth     DATE          NULL,
    Gender          NVARCHAR(10)  NULL,
    Address         NVARCHAR(255) NULL,
    RewardPoint     INT           NOT NULL DEFAULT 0,
    MembershipLevel NVARCHAR(50)  NULL,   -- Bronze | Silver | Gold | Platinum
    RoleId          INT           NOT NULL,
    IsActive        BIT           NOT NULL DEFAULT 1,
    CreatedAt       DATETIME      NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_USERS       PRIMARY KEY (UserId),
    CONSTRAINT UQ_USERS_EMAIL UNIQUE (Email),
    CONSTRAINT FK_USERS_ROLE  FOREIGN KEY (RoleId)
        REFERENCES ROLES (RoleId)
        ON UPDATE CASCADE
        ON DELETE NO ACTION
);
GO

-- ============================================================
-- 4. CINEMAS  (UC-15 Manage theaters)
--    AreaId added for UC-03 Select area filtering
-- ============================================================
CREATE TABLE CINEMAS (
    CinemaId   INT           NOT NULL IDENTITY(1,1),
    AreaId     INT           NOT NULL,
    CinemaName NVARCHAR(150) NOT NULL,
    Address    NVARCHAR(255) NOT NULL,
    Phone      NVARCHAR(20)  NULL,
    IsActive   BIT           NOT NULL DEFAULT 1,
    CONSTRAINT PK_CINEMAS      PRIMARY KEY (CinemaId),
    CONSTRAINT FK_CINEMAS_AREA FOREIGN KEY (AreaId)
        REFERENCES AREAS (AreaId)
        ON UPDATE CASCADE
        ON DELETE NO ACTION
);
GO

-- ============================================================
-- 5. ROOMS  (UC-17 Manage screening rooms | UC-20 Management seat)
-- ============================================================
CREATE TABLE ROOMS (
    RoomId     INT           NOT NULL IDENTITY(1,1),
    CinemaId   INT           NOT NULL,
    RoomName   NVARCHAR(100) NOT NULL,
    RoomType   NVARCHAR(50)  NULL,        -- 2D | 3D | IMAX | 4DX
    TotalSeats INT           NOT NULL DEFAULT 0,
    IsActive   BIT           NOT NULL DEFAULT 1,
    CONSTRAINT PK_ROOMS        PRIMARY KEY (RoomId),
    CONSTRAINT FK_ROOMS_CINEMA FOREIGN KEY (CinemaId)
        REFERENCES CINEMAS (CinemaId)
        ON UPDATE CASCADE
        ON DELETE CASCADE
);
GO

-- ============================================================
-- 6. SEATS  (UC-20 Management seat)
-- ============================================================
CREATE TABLE SEATS (
    SeatId     INT          NOT NULL IDENTITY(1,1),
    RoomId     INT          NOT NULL,
    SeatRow    NCHAR(2)     NOT NULL,     -- A, B, C ...
    SeatNumber NVARCHAR(10) NOT NULL,     -- A1, B2 ...
    SeatType   NVARCHAR(30) NULL,         -- Standard | VIP | Couple
    IsActive   BIT          NOT NULL DEFAULT 1,
    CONSTRAINT PK_SEATS      PRIMARY KEY (SeatId),
    CONSTRAINT UQ_SEATS      UNIQUE (RoomId, SeatNumber),
    CONSTRAINT FK_SEATS_ROOM FOREIGN KEY (RoomId)
        REFERENCES ROOMS (RoomId)
        ON UPDATE CASCADE
        ON DELETE CASCADE
);
GO

-- ============================================================
-- 7. MOVIECATEGORIES  (UC-19 Manage movies - genre tagging)
-- ============================================================
CREATE TABLE MOVIECATEGORIES (
    CategoryId   INT           NOT NULL IDENTITY(1,1),
    CategoryName NVARCHAR(100) NOT NULL,
    CONSTRAINT PK_MOVIECATEGORIES PRIMARY KEY (CategoryId),
    CONSTRAINT UQ_CATEGORY_NAME   UNIQUE (CategoryName)
);
GO

-- ============================================================
-- 8. MOVIES  (UC-04 Movie list | UC-19 Manage movies)
-- ============================================================
CREATE TABLE MOVIES (
    MovieId     INT           NOT NULL IDENTITY(1,1),
    Title       NVARCHAR(200) NOT NULL,
    Description NVARCHAR(MAX) NULL,
    Duration    INT           NOT NULL,   -- minutes
    Director    NVARCHAR(200) NULL,
    Actors      NVARCHAR(MAX) NULL,
    Language    NVARCHAR(50)  NULL,
    Subtitles   NVARCHAR(50)  NULL,
    AgeRating   NVARCHAR(10)  NULL,       -- P | C13 | C16 | C18
    ReleaseDate DATE          NULL,
    EndDate     DATE          NULL,
    PosterUrl   NVARCHAR(500) NULL,
    TrailerUrl  NVARCHAR(500) NULL,
    Status      NVARCHAR(20)  NOT NULL DEFAULT 'Active',  -- Active | Inactive | Coming Soon
    CreatedBy   INT           NOT NULL,
    CreatedAt   DATETIME      NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_MOVIES      PRIMARY KEY (MovieId),
    CONSTRAINT FK_MOVIES_USER FOREIGN KEY (CreatedBy)
        REFERENCES USERS (UserId)
        ON UPDATE NO ACTION
        ON DELETE NO ACTION
);
GO

-- ============================================================
-- 9. MOVIECATEGORYMAPPING  (many-to-many: MOVIES <-> MOVIECATEGORIES)
-- ============================================================
CREATE TABLE MOVIECATEGORYMAPPING (
    MovieId    INT NOT NULL,
    CategoryId INT NOT NULL,
    CONSTRAINT PK_MOVIECATEGORYMAPPING PRIMARY KEY (MovieId, CategoryId),
    CONSTRAINT FK_MCM_MOVIE    FOREIGN KEY (MovieId)
        REFERENCES MOVIES (MovieId)
        ON UPDATE CASCADE
        ON DELETE CASCADE,
    CONSTRAINT FK_MCM_CATEGORY FOREIGN KEY (CategoryId)
        REFERENCES MOVIECATEGORIES (CategoryId)
        ON UPDATE CASCADE
        ON DELETE CASCADE
);
GO

-- ============================================================
-- 10. SHOWTIMES  (UC-05 View movie schedule | UC-21 Schedule movie screenings)
-- ============================================================
CREATE TABLE SHOWTIMES (
    ShowTimeId INT            NOT NULL IDENTITY(1,1),
    MovieId    INT            NOT NULL,
    RoomId     INT            NOT NULL,
    StartTime  DATETIME       NOT NULL,
    EndTime    DATETIME       NOT NULL,
    BasePrice  DECIMAL(12, 2) NOT NULL DEFAULT 0,
    Status     NVARCHAR(30)   NOT NULL DEFAULT 'Active',  -- Active | Cancelled | Completed
    CONSTRAINT PK_SHOWTIMES PRIMARY KEY (ShowTimeId),
    CONSTRAINT FK_ST_MOVIE  FOREIGN KEY (MovieId)
        REFERENCES MOVIES (MovieId)
        ON UPDATE NO ACTION
        ON DELETE NO ACTION,
    CONSTRAINT FK_ST_ROOM   FOREIGN KEY (RoomId)
        REFERENCES ROOMS (RoomId)
        ON UPDATE NO ACTION
        ON DELETE NO ACTION
);
GO

-- ============================================================
-- 11. TICKETPRICING  (UC-22 Manage ticket - price configuration by day/seat/room type)
-- ============================================================
CREATE TABLE TICKETPRICING (
    PricingId  INT            NOT NULL IDENTITY(1,1),
    RoomType   NVARCHAR(50)   NULL,       -- 2D | 3D | IMAX | 4DX ; NULL = all
    SeatType   NVARCHAR(30)   NULL,       -- Standard | VIP | Couple ; NULL = all
    DayType    NVARCHAR(20)   NULL,       -- Weekday | Weekend | Holiday ; NULL = all
    Price      DECIMAL(12, 2) NOT NULL DEFAULT 0,
    EffectFrom DATE           NOT NULL,
    EffectTo   DATE           NULL,       -- NULL = indefinite
    IsActive   BIT            NOT NULL DEFAULT 1,
    CreatedBy  INT            NOT NULL,
    CONSTRAINT PK_TICKETPRICING PRIMARY KEY (PricingId),
    CONSTRAINT FK_TP_USER       FOREIGN KEY (CreatedBy)
        REFERENCES USERS (UserId)
        ON UPDATE NO ACTION
        ON DELETE NO ACTION
);
GO

-- ============================================================
-- 12. DISCOUNTS  (UC-07 Apply Discount Code)
-- ============================================================
CREATE TABLE DISCOUNTS (
    DiscountId      INT            NOT NULL IDENTITY(1,1),
    DiscountCode    NVARCHAR(50)   NOT NULL,
    Description     NVARCHAR(255)  NULL,
    DiscountType    NVARCHAR(20)   NOT NULL DEFAULT 'Percent',  -- Percent | Fixed
    DiscountValue   DECIMAL(12, 2) NOT NULL DEFAULT 0,
    MinOrderAmount  DECIMAL(12, 2) NOT NULL DEFAULT 0,
    MaxUsageTotal   INT            NULL,   -- NULL = unlimited
    MaxUsagePerUser INT            NOT NULL DEFAULT 1,
    UsedCount       INT            NOT NULL DEFAULT 0,
    StartDate       DATETIME       NOT NULL,
    EndDate         DATETIME       NULL,
    IsActive        BIT            NOT NULL DEFAULT 1,
    CreatedBy       INT            NOT NULL,
    CONSTRAINT PK_DISCOUNTS     PRIMARY KEY (DiscountId),
    CONSTRAINT UQ_DISCOUNT_CODE UNIQUE (DiscountCode),
    CONSTRAINT FK_DC_USER       FOREIGN KEY (CreatedBy)
        REFERENCES USERS (UserId)
        ON UPDATE NO ACTION
        ON DELETE NO ACTION
);
GO

-- ============================================================
-- 13. REVIEWS  (UC-11 Review movie)
-- ============================================================
CREATE TABLE REVIEWS (
    ReviewId   INT           NOT NULL IDENTITY(1,1),
    UserId     INT           NOT NULL,
    MovieId    INT           NOT NULL,
    Rating     INT           NOT NULL,    -- 1-10
    Comment    NVARCHAR(MAX) NULL,
    ReviewDate DATETIME      NOT NULL DEFAULT GETDATE(),
    IsApproved BIT           NOT NULL DEFAULT 0,  -- admin moderation
    CONSTRAINT PK_REVIEWS           PRIMARY KEY (ReviewId),
    CONSTRAINT CK_RATING            CHECK (Rating BETWEEN 1 AND 10),
    CONSTRAINT UQ_USER_MOVIE_REVIEW UNIQUE (UserId, MovieId),
    CONSTRAINT FK_REV_USER          FOREIGN KEY (UserId)
        REFERENCES USERS (UserId)
        ON UPDATE CASCADE
        ON DELETE NO ACTION,
    CONSTRAINT FK_REV_MOVIE         FOREIGN KEY (MovieId)
        REFERENCES MOVIES (MovieId)
        ON UPDATE CASCADE
        ON DELETE NO ACTION
);
GO

-- ============================================================
-- 14. BOOKINGS  (UC-06 Book tickets | UC-13 Sell tickets at counter)
--     One row = one seat per showtime
-- ============================================================
CREATE TABLE BOOKINGS (
    BookingId   INT            NOT NULL IDENTITY(1,1),
    UserId      INT            NOT NULL,
    ShowTimeId  INT            NOT NULL,
    SeatId      INT            NOT NULL,
    DiscountId  INT            NULL,      -- UC-07 Apply Discount Code
    BookingDate DATETIME       NOT NULL DEFAULT GETDATE(),
    TicketPrice DECIMAL(12, 2) NOT NULL DEFAULT 0,  -- price before discount
    DiscountAmt DECIMAL(12, 2) NOT NULL DEFAULT 0,
    TotalAmount DECIMAL(12, 2) NOT NULL DEFAULT 0,  -- price after discount
    BookingType NVARCHAR(20)   NOT NULL DEFAULT 'Online',  -- Online | Counter
    StaffId     INT            NULL,      -- UC-13: staff who sold at counter
    Status      NVARCHAR(30)   NOT NULL DEFAULT 'Pending',  -- Pending | Confirmed | Cancelled
    CONSTRAINT PK_BOOKINGS     PRIMARY KEY (BookingId),
    CONSTRAINT UQ_BOOKING_SEAT UNIQUE (ShowTimeId, SeatId),
    CONSTRAINT FK_BK_USER      FOREIGN KEY (UserId)
        REFERENCES USERS (UserId)
        ON UPDATE NO ACTION
        ON DELETE NO ACTION,
    CONSTRAINT FK_BK_SHOWTIME  FOREIGN KEY (ShowTimeId)
        REFERENCES SHOWTIMES (ShowTimeId)
        ON UPDATE NO ACTION
        ON DELETE NO ACTION,
    CONSTRAINT FK_BK_SEAT      FOREIGN KEY (SeatId)
        REFERENCES SEATS (SeatId)
        ON UPDATE NO ACTION
        ON DELETE NO ACTION,
    CONSTRAINT FK_BK_DISCOUNT  FOREIGN KEY (DiscountId)
        REFERENCES DISCOUNTS (DiscountId)
        ON UPDATE NO ACTION
        ON DELETE NO ACTION,
    CONSTRAINT FK_BK_STAFF     FOREIGN KEY (StaffId)
        REFERENCES USERS (UserId)
        ON UPDATE NO ACTION
        ON DELETE NO ACTION
);
GO

-- ============================================================
-- 15. TICKETS  (UC-22 Manage ticket - issued after confirmed booking)
-- ============================================================
CREATE TABLE TICKETS (
    TicketId   INT            NOT NULL IDENTITY(1,1),
    BookingId  INT            NOT NULL,
    TicketCode NVARCHAR(50)   NOT NULL,  -- unique printable/scannable code
    QrCodeUrl  NVARCHAR(500)  NULL,
    Price      DECIMAL(12, 2) NOT NULL DEFAULT 0,
    IssuedAt   DATETIME       NOT NULL DEFAULT GETDATE(),
    Status     NVARCHAR(30)   NOT NULL DEFAULT 'Active',  -- Active | Used | Cancelled
    CONSTRAINT PK_TICKETS     PRIMARY KEY (TicketId),
    CONSTRAINT UQ_TICKET_CODE UNIQUE (TicketCode),
    CONSTRAINT FK_TK_BOOKING  FOREIGN KEY (BookingId)
        REFERENCES BOOKINGS (BookingId)
        ON UPDATE CASCADE
        ON DELETE CASCADE
);
GO

-- ============================================================
-- 16. FOODS  (UC-08 Buy food, drink | UC-12 Sell F&B at Counter)
-- ============================================================
CREATE TABLE FOODS (
    FoodId      INT            NOT NULL IDENTITY(1,1),
    FoodName    NVARCHAR(150)  NOT NULL,
    Category    NVARCHAR(50)   NULL,      -- Popcorn | Drink | Snack | Meal
    Price       DECIMAL(12, 2) NOT NULL DEFAULT 0,
    Quantity    INT            NOT NULL DEFAULT 0,
    ImageUrl    NVARCHAR(500)  NULL,
    IsAvailable BIT            NOT NULL DEFAULT 1,
    CONSTRAINT PK_FOODS PRIMARY KEY (FoodId)
);
GO

-- ============================================================
-- 17. COMBOS  (UC-08 Buy food, drink - combo packages)
-- ============================================================
CREATE TABLE COMBOS (
    ComboId     INT            NOT NULL IDENTITY(1,1),
    ComboName   NVARCHAR(150)  NOT NULL,
    Price       DECIMAL(12, 2) NOT NULL DEFAULT 0,
    Description NVARCHAR(MAX)  NULL,
    ImageUrl    NVARCHAR(500)  NULL,
    Quantity    INT            NOT NULL DEFAULT 0,
    IsAvailable BIT            NOT NULL DEFAULT 1,
    CONSTRAINT PK_COMBOS PRIMARY KEY (ComboId)
);
GO

-- ============================================================
-- 18. COMBOFOODMAPPING  (many-to-many: COMBOS <-> FOODS)
-- ============================================================
CREATE TABLE COMBOFOODMAPPING (
    ComboId  INT NOT NULL,
    FoodId   INT NOT NULL,
    Quantity INT NOT NULL DEFAULT 1,
    CONSTRAINT PK_COMBOFOODMAPPING PRIMARY KEY (ComboId, FoodId),
    CONSTRAINT FK_CFM_COMBO FOREIGN KEY (ComboId)
        REFERENCES COMBOS (ComboId)
        ON UPDATE CASCADE
        ON DELETE CASCADE,
    CONSTRAINT FK_CFM_FOOD  FOREIGN KEY (FoodId)
        REFERENCES FOODS (FoodId)
        ON UPDATE NO ACTION
        ON DELETE NO ACTION
);
GO

-- ============================================================
-- 19. ORDERS  (UC-08 Buy food, drink | UC-12 Sell F&B at Counter)
-- ============================================================
CREATE TABLE ORDERS (
    OrderId     INT            NOT NULL IDENTITY(1,1),
    UserId      INT            NOT NULL,
    BookingId   INT            NULL,      -- nullable: counter F&B orders may have no booking
    StaffId     INT            NULL,      -- UC-12: counter staff
    DiscountId  INT            NULL,
    OrderDate   DATETIME       NOT NULL DEFAULT GETDATE(),
    TotalAmount DECIMAL(12, 2) NOT NULL DEFAULT 0,
    OrderType   NVARCHAR(20)   NOT NULL DEFAULT 'Online',  -- Online | Counter
    Status      NVARCHAR(30)   NOT NULL DEFAULT 'Pending', -- Pending | Confirmed | Cancelled
    CONSTRAINT PK_ORDERS       PRIMARY KEY (OrderId),
    CONSTRAINT FK_ORD_USER     FOREIGN KEY (UserId)
        REFERENCES USERS (UserId)
        ON UPDATE NO ACTION
        ON DELETE NO ACTION,
    CONSTRAINT FK_ORD_BOOKING  FOREIGN KEY (BookingId)
        REFERENCES BOOKINGS (BookingId)
        ON UPDATE NO ACTION
        ON DELETE NO ACTION,
    CONSTRAINT FK_ORD_STAFF    FOREIGN KEY (StaffId)
        REFERENCES USERS (UserId)
        ON UPDATE NO ACTION
        ON DELETE NO ACTION,
    CONSTRAINT FK_ORD_DISCOUNT FOREIGN KEY (DiscountId)
        REFERENCES DISCOUNTS (DiscountId)
        ON UPDATE NO ACTION
        ON DELETE NO ACTION
);
GO

-- ============================================================
-- 20. ORDERITEMS  (line items for each order)
-- ============================================================
CREATE TABLE ORDERITEMS (
    OrderItemId INT            NOT NULL IDENTITY(1,1),
    OrderId     INT            NOT NULL,
    FoodId      INT            NULL,
    ComboId     INT            NULL,
    Quantity    INT            NOT NULL DEFAULT 1,
    UnitPrice   DECIMAL(12, 2) NOT NULL DEFAULT 0,
    Subtotal    DECIMAL(12, 2) NOT NULL DEFAULT 0,
    CONSTRAINT PK_ORDERITEMS PRIMARY KEY (OrderItemId),
    CONSTRAINT CK_OI_ITEM    CHECK (FoodId IS NOT NULL OR ComboId IS NOT NULL),
    CONSTRAINT FK_OI_ORDER   FOREIGN KEY (OrderId)
        REFERENCES ORDERS (OrderId)
        ON UPDATE CASCADE
        ON DELETE CASCADE,
    CONSTRAINT FK_OI_FOOD    FOREIGN KEY (FoodId)
        REFERENCES FOODS (FoodId)
        ON UPDATE NO ACTION
        ON DELETE NO ACTION,
    CONSTRAINT FK_OI_COMBO   FOREIGN KEY (ComboId)
        REFERENCES COMBOS (ComboId)
        ON UPDATE NO ACTION
        ON DELETE NO ACTION
);
GO

-- ============================================================
-- 21. PAYMENTS  (UC-09 Payments | UC-18 Management invoice)
-- ============================================================
CREATE TABLE PAYMENTS (
    PaymentId     INT            NOT NULL IDENTITY(1,1),
    BookingId     INT            NULL,
    OrderId       INT            NULL,
    UserId        INT            NOT NULL,
    StaffId       INT            NULL,    -- NULL for online payments
    PaymentMethod NVARCHAR(50)   NOT NULL,  -- Cash | Card | Momo | ZaloPay | VNPay
    SubTotal      DECIMAL(12, 2) NOT NULL DEFAULT 0,
    DiscountAmt   DECIMAL(12, 2) NOT NULL DEFAULT 0,
    TotalAmount   DECIMAL(12, 2) NOT NULL DEFAULT 0,
    TransactionId NVARCHAR(100)  NULL,    -- reference from payment gateway
    CreatedAt     DATETIME       NOT NULL DEFAULT GETDATE(),
    PaidAt        DATETIME       NULL,
    PaymentStatus NVARCHAR(30)   NOT NULL DEFAULT 'Pending',  -- Pending | Success | Failed | Refunded
    Notes         NVARCHAR(500)  NULL,
    CONSTRAINT PK_PAYMENTS    PRIMARY KEY (PaymentId),
    CONSTRAINT CK_PAY_REF     CHECK (BookingId IS NOT NULL OR OrderId IS NOT NULL),
    CONSTRAINT FK_PAY_BOOKING FOREIGN KEY (BookingId)
        REFERENCES BOOKINGS (BookingId)
        ON UPDATE NO ACTION
        ON DELETE NO ACTION,
    CONSTRAINT FK_PAY_ORDER   FOREIGN KEY (OrderId)
        REFERENCES ORDERS (OrderId)
        ON UPDATE NO ACTION
        ON DELETE NO ACTION,
    CONSTRAINT FK_PAY_USER    FOREIGN KEY (UserId)
        REFERENCES USERS (UserId)
        ON UPDATE NO ACTION
        ON DELETE NO ACTION,
    CONSTRAINT FK_PAY_STAFF   FOREIGN KEY (StaffId)
        REFERENCES USERS (UserId)
        ON UPDATE NO ACTION
        ON DELETE NO ACTION
);
GO

-- ============================================================
-- 22. USERDISCOUNTUSAGE  (track per-user discount code usage limit)
-- ============================================================
CREATE TABLE USERDISCOUNTUSAGE (
    UsageId    INT      NOT NULL IDENTITY(1,1),
    UserId     INT      NOT NULL,
    DiscountId INT      NOT NULL,
    UsedCount  INT      NOT NULL DEFAULT 1,
    LastUsedAt DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_USERDISCOUNTUSAGE PRIMARY KEY (UsageId),
    CONSTRAINT UQ_USER_DISCOUNT     UNIQUE (UserId, DiscountId),
    CONSTRAINT FK_UDU_USER          FOREIGN KEY (UserId)
        REFERENCES USERS (UserId)
        ON UPDATE CASCADE
        ON DELETE NO ACTION,
    CONSTRAINT FK_UDU_DISCOUNT      FOREIGN KEY (DiscountId)
        REFERENCES DISCOUNTS (DiscountId)
        ON UPDATE CASCADE
        ON DELETE NO ACTION
);
GO

-- ============================================================
-- 23. STAFFSHIFTS  (UC-14 Shift summary - track each staff shift)
-- ============================================================
CREATE TABLE STAFFSHIFTS (
    ShiftId       INT            NOT NULL IDENTITY(1,1),
    StaffId       INT            NOT NULL,
    CinemaId      INT            NOT NULL,
    ShiftDate     DATE           NOT NULL,
    ShiftStart    DATETIME       NOT NULL,
    ShiftEnd      DATETIME       NULL,
    TotalBookings INT            NOT NULL DEFAULT 0,
    TotalOrders   INT            NOT NULL DEFAULT 0,
    TotalRevenue  DECIMAL(15, 2) NOT NULL DEFAULT 0,
    Summary       NVARCHAR(1000) NULL,
    Status        NVARCHAR(20)   NOT NULL DEFAULT 'Open',  -- Open | Closed
    CONSTRAINT PK_STAFFSHIFTS PRIMARY KEY (ShiftId),
    CONSTRAINT FK_SS_STAFF    FOREIGN KEY (StaffId)
        REFERENCES USERS (UserId)
        ON UPDATE NO ACTION
        ON DELETE NO ACTION,
    CONSTRAINT FK_SS_CINEMA   FOREIGN KEY (CinemaId)
        REFERENCES CINEMAS (CinemaId)
        ON UPDATE NO ACTION
        ON DELETE NO ACTION
);
GO

-- ============================================================
-- 24. STAFFREPORTS  (UC-14 / UC-18 - admin-level shift reports)
-- ============================================================
CREATE TABLE STAFFREPORTS (
    ReportId      INT            NOT NULL IDENTITY(1,1),
    StaffId       INT            NOT NULL,
    CinemaId      INT            NOT NULL,
    ReportDate    DATE           NOT NULL,
    Summary       NVARCHAR(1000) NULL,
    TotalBookings INT            NOT NULL DEFAULT 0,
    TotalOrders   INT            NOT NULL DEFAULT 0,
    TotalRevenue  DECIMAL(15, 2) NOT NULL DEFAULT 0,
    GeneratedAt   DATETIME       NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_STAFFREPORTS PRIMARY KEY (ReportId),
    CONSTRAINT FK_SR_STAFF     FOREIGN KEY (StaffId)
        REFERENCES USERS (UserId)
        ON UPDATE NO ACTION
        ON DELETE NO ACTION,
    CONSTRAINT FK_SR_CINEMA    FOREIGN KEY (CinemaId)
        REFERENCES CINEMAS (CinemaId)
        ON UPDATE NO ACTION
        ON DELETE NO ACTION
);
GO

-- ============================================================
-- INDEXES
-- ============================================================
CREATE INDEX IDX_USERS_ROLE    ON USERS       (RoleId);
CREATE INDEX IDX_CINEMAS_AREA  ON CINEMAS     (AreaId);
CREATE INDEX IDX_ROOMS_CINEMA  ON ROOMS       (CinemaId);
CREATE INDEX IDX_SEATS_ROOM    ON SEATS       (RoomId);
CREATE INDEX IDX_MOVIES_STATUS ON MOVIES      (Status);
CREATE INDEX IDX_MOVIES_RELEASE ON MOVIES     (ReleaseDate);
CREATE INDEX IDX_ST_MOVIE      ON SHOWTIMES   (MovieId);
CREATE INDEX IDX_ST_ROOM       ON SHOWTIMES   (RoomId);
CREATE INDEX IDX_ST_START      ON SHOWTIMES   (StartTime);
CREATE INDEX IDX_BK_USER       ON BOOKINGS    (UserId);
CREATE INDEX IDX_BK_SHOWTIME   ON BOOKINGS    (ShowTimeId);
CREATE INDEX IDX_BK_STATUS     ON BOOKINGS    (Status);
CREATE INDEX IDX_TK_BOOKING    ON TICKETS     (BookingId);
CREATE INDEX IDX_TK_CODE       ON TICKETS     (TicketCode);
CREATE INDEX IDX_REV_MOVIE     ON REVIEWS     (MovieId);
CREATE INDEX IDX_ORD_USER      ON ORDERS      (UserId);
CREATE INDEX IDX_ORD_BOOKING   ON ORDERS      (BookingId);
CREATE INDEX IDX_OI_ORDER      ON ORDERITEMS  (OrderId);
CREATE INDEX IDX_PAY_BOOKING   ON PAYMENTS    (BookingId);
CREATE INDEX IDX_PAY_ORDER     ON PAYMENTS    (OrderId);
CREATE INDEX IDX_PAY_STATUS    ON PAYMENTS    (PaymentStatus);
CREATE INDEX IDX_DC_CODE       ON DISCOUNTS   (DiscountCode);
CREATE INDEX IDX_SS_STAFF_DATE ON STAFFSHIFTS (StaffId, ShiftDate);
GO

-- ============================================================
-- VIEWS
-- ============================================================

-- UC-05 View movie schedule detail
CREATE OR ALTER VIEW VW_SHOWTIME_DETAIL AS
SELECT
    s.ShowTimeId,
    m.MovieId,
    m.Title      AS MovieTitle,
    m.Duration,
    m.AgeRating,
    m.PosterUrl,
    a.AreaName,
    c.CinemaId,
    c.CinemaName,
    r.RoomId,
    r.RoomName,
    r.RoomType,
    s.StartTime,
    s.EndTime,
    s.BasePrice,
    s.Status
FROM SHOWTIMES s
JOIN MOVIES  m ON s.MovieId  = m.MovieId
JOIN ROOMS   r ON s.RoomId   = r.RoomId
JOIN CINEMAS c ON r.CinemaId = c.CinemaId
JOIN AREAS   a ON c.AreaId   = a.AreaId;
GO

-- UC-10 View purchase history
CREATE OR ALTER VIEW VW_BOOKING_DETAIL AS
SELECT
    b.BookingId,
    u.FullName   AS CustomerName,
    u.Email,
    m.Title      AS MovieTitle,
    a.AreaName,
    c.CinemaName,
    r.RoomName,
    r.RoomType,
    se.SeatNumber,
    se.SeatType,
    sh.StartTime,
    b.TicketPrice,
    b.DiscountAmt,
    b.TotalAmount,
    b.BookingType,
    b.Status,
    b.BookingDate
FROM BOOKINGS  b
JOIN USERS     u  ON b.UserId     = u.UserId
JOIN SHOWTIMES sh ON b.ShowTimeId = sh.ShowTimeId
JOIN MOVIES    m  ON sh.MovieId   = m.MovieId
JOIN ROOMS     r  ON sh.RoomId    = r.RoomId
JOIN CINEMAS   c  ON r.CinemaId   = c.CinemaId
JOIN AREAS     a  ON c.AreaId     = a.AreaId
JOIN SEATS     se ON b.SeatId     = se.SeatId;
GO

-- UC-18 Management invoice - revenue by movie
CREATE OR ALTER VIEW VW_REVENUE_BY_MOVIE AS
SELECT
    m.MovieId,
    m.Title,
    COUNT(b.BookingId) AS TotalBookings,
    SUM(b.TotalAmount) AS TotalRevenue
FROM MOVIES m
LEFT JOIN SHOWTIMES sh ON sh.MovieId   = m.MovieId
LEFT JOIN BOOKINGS  b  ON b.ShowTimeId = sh.ShowTimeId
                      AND b.Status     = 'Confirmed'
GROUP BY m.MovieId, m.Title;
GO

-- UC-06 Available seats per showtime
CREATE OR ALTER VIEW VW_AVAILABLE_SEATS AS
SELECT
    sh.ShowTimeId,
    m.Title     AS MovieTitle,
    sh.StartTime,
    se.SeatId,
    se.SeatNumber,
    se.SeatType,
    r.RoomId,
    r.RoomName
FROM SHOWTIMES sh
JOIN MOVIES m  ON sh.MovieId = m.MovieId
JOIN ROOMS  r  ON sh.RoomId  = r.RoomId
JOIN SEATS  se ON se.RoomId  = r.RoomId
WHERE se.IsActive = 1
  AND se.SeatId NOT IN (
        SELECT SeatId FROM BOOKINGS
        WHERE ShowTimeId = sh.ShowTimeId
          AND Status    <> 'Cancelled'
  );
GO

-- ============================================================
-- STORED PROCEDURES
-- ============================================================

-- UC-06 Book ticket (online) / UC-13 Sell ticket at counter
CREATE OR ALTER PROCEDURE SP_BOOK_TICKET
    @UserId       INT,
    @ShowTimeId   INT,
    @SeatId       INT,
    @DiscountCode NVARCHAR(50) = NULL,
    @BookingType  NVARCHAR(20) = 'Online',
    @StaffId      INT          = NULL,
    @BookingId    INT          OUTPUT,
    @Message      NVARCHAR(200) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Exists       INT;
    DECLARE @BasePrice    DECIMAL(12,2);
    DECLARE @DiscountId   INT           = NULL;
    DECLARE @DiscountAmt  DECIMAL(12,2) = 0;
    DECLARE @DiscountType NVARCHAR(20);
    DECLARE @DiscountVal  DECIMAL(12,2);
    DECLARE @UsedCount    INT;
    DECLARE @MaxPerUser   INT;

    -- Check seat availability
    SELECT @Exists = COUNT(*)
    FROM BOOKINGS
    WHERE ShowTimeId = @ShowTimeId
      AND SeatId     = @SeatId
      AND Status    <> 'Cancelled';

    IF @Exists > 0
    BEGIN
        SET @BookingId = 0;
        SET @Message   = 'Seat is already booked. Please choose another seat.';
        RETURN;
    END

    SELECT @BasePrice = BasePrice FROM SHOWTIMES WHERE ShowTimeId = @ShowTimeId;

    -- Apply discount if code provided
    IF @DiscountCode IS NOT NULL AND @DiscountCode <> ''
    BEGIN
        SELECT
            @DiscountId   = DiscountId,
            @DiscountType = DiscountType,
            @DiscountVal  = DiscountValue,
            @MaxPerUser   = MaxUsagePerUser
        FROM DISCOUNTS
        WHERE DiscountCode = @DiscountCode
          AND IsActive     = 1
          AND (EndDate IS NULL OR EndDate >= GETDATE())
          AND StartDate   <= GETDATE();

        IF @DiscountId IS NULL
        BEGIN
            SET @BookingId = 0;
            SET @Message   = 'Discount code is invalid or expired.';
            RETURN;
        END

        -- Check per-user usage limit
        SELECT @UsedCount = ISNULL(UsedCount, 0)
        FROM USERDISCOUNTUSAGE
        WHERE UserId = @UserId AND DiscountId = @DiscountId;

        IF ISNULL(@UsedCount, 0) >= @MaxPerUser
        BEGIN
            SET @BookingId = 0;
            SET @Message   = 'You have reached the usage limit for this discount code.';
            RETURN;
        END

        IF @DiscountType = 'Percent'
            SET @DiscountAmt = @BasePrice * @DiscountVal / 100;
        ELSE
            SET @DiscountAmt = @DiscountVal;

        IF @DiscountAmt > @BasePrice SET @DiscountAmt = @BasePrice;
    END

    -- Insert booking
    INSERT INTO BOOKINGS (UserId, ShowTimeId, SeatId, DiscountId, TicketPrice,
                          DiscountAmt, TotalAmount, BookingType, StaffId, Status)
    VALUES (@UserId, @ShowTimeId, @SeatId, @DiscountId, @BasePrice,
            @DiscountAmt, @BasePrice - @DiscountAmt, @BookingType, @StaffId, 'Pending');

    SET @BookingId = SCOPE_IDENTITY();

    -- Update discount usage counters
    IF @DiscountId IS NOT NULL
    BEGIN
        IF EXISTS (SELECT 1 FROM USERDISCOUNTUSAGE WHERE UserId = @UserId AND DiscountId = @DiscountId)
            UPDATE USERDISCOUNTUSAGE
               SET UsedCount = UsedCount + 1, LastUsedAt = GETDATE()
             WHERE UserId = @UserId AND DiscountId = @DiscountId;
        ELSE
            INSERT INTO USERDISCOUNTUSAGE (UserId, DiscountId) VALUES (@UserId, @DiscountId);

        UPDATE DISCOUNTS SET UsedCount = UsedCount + 1 WHERE DiscountId = @DiscountId;
    END

    SET @Message = 'Booking created successfully.';
END
GO


-- UC-06 / UC-13 Cancel booking
CREATE OR ALTER PROCEDURE SP_CANCEL_BOOKING
    @BookingId INT,
    @Message   NVARCHAR(200) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Status NVARCHAR(30);
    SELECT @Status = Status FROM BOOKINGS WHERE BookingId = @BookingId;

    IF @Status IS NULL
        SET @Message = 'Booking not found.';
    ELSE IF @Status = 'Confirmed'
        SET @Message = 'Cannot cancel a confirmed booking.';
    ELSE IF @Status = 'Cancelled'
        SET @Message = 'Booking is already cancelled.';
    ELSE
    BEGIN
        UPDATE BOOKINGS SET Status = 'Cancelled' WHERE BookingId = @BookingId;
        UPDATE TICKETS  SET Status = 'Cancelled' WHERE BookingId = @BookingId;
        SET @Message = 'Booking cancelled successfully.';
    END
END
GO


-- UC-09 Process payment - confirms booking and auto-issues ticket
CREATE OR ALTER PROCEDURE SP_PROCESS_PAYMENT
    @BookingId     INT            = NULL,
    @OrderId       INT            = NULL,
    @UserId        INT,
    @StaffId       INT            = NULL,
    @PaymentMethod NVARCHAR(50),
    @TotalAmount   DECIMAL(12,2),
    @TransactionId NVARCHAR(100)  = NULL,
    @PaymentId     INT            OUTPUT,
    @Message       NVARCHAR(200)  OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF @BookingId IS NULL AND @OrderId IS NULL
    BEGIN
        SET @PaymentId = 0;
        SET @Message   = 'Payment must be linked to a booking or an order.';
        RETURN;
    END

    INSERT INTO PAYMENTS (BookingId, OrderId, UserId, StaffId, PaymentMethod,
                          TotalAmount, TransactionId, PaymentStatus, PaidAt)
    VALUES (@BookingId, @OrderId, @UserId, @StaffId, @PaymentMethod,
            @TotalAmount, @TransactionId, 'Success', GETDATE());

    SET @PaymentId = SCOPE_IDENTITY();

    -- Confirm booking and auto-issue ticket
    IF @BookingId IS NOT NULL
    BEGIN
        UPDATE BOOKINGS SET Status = 'Confirmed' WHERE BookingId = @BookingId;

        DECLARE @TicketCode  NVARCHAR(50)   = UPPER(CONVERT(NVARCHAR(50), NEWID()));
        DECLARE @TicketPrice DECIMAL(12,2);
        SELECT  @TicketPrice = TotalAmount FROM BOOKINGS WHERE BookingId = @BookingId;

        INSERT INTO TICKETS (BookingId, TicketCode, Price, Status)
        VALUES (@BookingId, @TicketCode, @TicketPrice, 'Active');
    END

    IF @OrderId IS NOT NULL
        UPDATE ORDERS SET Status = 'Confirmed' WHERE OrderId = @OrderId;

    SET @Message = 'Payment processed successfully.';
END
GO


-- UC-14 Close staff shift and generate summary report
CREATE OR ALTER PROCEDURE SP_CLOSE_SHIFT
    @ShiftId INT,
    @Summary NVARCHAR(1000) = NULL,
    @Message NVARCHAR(200)  OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @StaffId   INT;
    DECLARE @CinemaId  INT;
    DECLARE @ShiftDate DATE;

    SELECT @StaffId   = StaffId,
           @CinemaId  = CinemaId,
           @ShiftDate = ShiftDate
    FROM STAFFSHIFTS WHERE ShiftId = @ShiftId;

    IF @StaffId IS NULL
    BEGIN
        SET @Message = 'Shift not found.';
        RETURN;
    END

    DECLARE @TotalBookings INT;
    DECLARE @TotalOrders   INT;
    DECLARE @TotalRevenue  DECIMAL(15,2);

    SELECT @TotalBookings = COUNT(*)
    FROM BOOKINGS
    WHERE StaffId     = @StaffId
      AND BookingType = 'Counter'
      AND CAST(BookingDate AS DATE) = @ShiftDate
      AND Status      = 'Confirmed';

    SELECT @TotalOrders = COUNT(*)
    FROM ORDERS
    WHERE StaffId = @StaffId
      AND CAST(OrderDate AS DATE) = @ShiftDate
      AND Status  = 'Confirmed';

    SELECT @TotalRevenue = ISNULL(SUM(TotalAmount), 0)
    FROM PAYMENTS
    WHERE StaffId = @StaffId
      AND CAST(CreatedAt AS DATE) = @ShiftDate
      AND PaymentStatus = 'Success';

    UPDATE STAFFSHIFTS
    SET ShiftEnd      = GETDATE(),
        TotalBookings = @TotalBookings,
        TotalOrders   = @TotalOrders,
        TotalRevenue  = @TotalRevenue,
        Summary       = @Summary,
        Status        = 'Closed'
    WHERE ShiftId = @ShiftId;

    -- Write admin-level report (UC-18)
    INSERT INTO STAFFREPORTS (StaffId, CinemaId, ReportDate, TotalBookings,
                               TotalOrders, TotalRevenue, Summary)
    VALUES (@StaffId, @CinemaId, @ShiftDate, @TotalBookings,
            @TotalOrders, @TotalRevenue, @Summary);

    SET @Message = 'Shift closed and report generated.';
END
GO
