-- =============================================
-- Kütüphane Yönetim Sistemi Veritabanı Kurulumu
-- =============================================

-- Veritabanını oluştur
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'kütüphaneuygulaması')
BEGIN
    CREATE DATABASE [kütüphaneuygulaması]
    COLLATE Turkish_CI_AS
END
GO

USE [kütüphaneuygulaması]
GO

-- =============================================
-- TABLOLAR
-- =============================================

-- Kullanıcı hesapları tablosu
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'usersaccounts')
BEGIN
    CREATE TABLE [usersaccounts] (
        [Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [name] nvarchar(100) NOT NULL UNIQUE,
        [pass] nvarchar(255) NOT NULL,
        [email] nvarchar(100) NOT NULL UNIQUE,
        [role] nvarchar(20) NOT NULL DEFAULT 'customer',
        [CreatedDate] datetime2 NOT NULL DEFAULT GETDATE(),
        [IsActive] bit NOT NULL DEFAULT 1
    )
END
GO

-- Kategoriler tablosu
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Category')
BEGIN
    CREATE TABLE [Category] (
        [Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [name] nvarchar(100) NOT NULL,
        [description] nvarchar(500) NULL,
        [CreatedDate] datetime2 NOT NULL DEFAULT GETDATE(),
        [IsActive] bit NOT NULL DEFAULT 1
    )
END
GO

-- Kitaplar tablosu
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Book')
BEGIN
    CREATE TABLE [Book] (
        [Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [title] nvarchar(200) NOT NULL,
        [author] nvarchar(100) NOT NULL,
        [description] nvarchar(1000) NULL,
        [price] decimal(10,2) NOT NULL,
        [stock] int NOT NULL DEFAULT 0,
        [imgfile] nvarchar(500) NULL,
        [CategoryId] int NOT NULL,
        [CreatedDate] datetime2 NOT NULL DEFAULT GETDATE(),
        [IsActive] bit NOT NULL DEFAULT 1,
        CONSTRAINT [FK_Book_Category] FOREIGN KEY ([CategoryId]) REFERENCES [Category] ([Id])
    )
END
GO

-- Sepet tablosu
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Cart')
BEGIN
    CREATE TABLE [Cart] (
        [Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [UserId] int NOT NULL,
        [BookId] int NOT NULL,
        [Quantity] int NOT NULL DEFAULT 1,
        [CreatedDate] datetime2 NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [FK_Cart_usersaccounts] FOREIGN KEY ([UserId]) REFERENCES [usersaccounts] ([Id]),
        CONSTRAINT [FK_Cart_Book] FOREIGN KEY ([BookId]) REFERENCES [Book] ([Id])
    )
END
GO

-- Siparişler tablosu
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'orders')
BEGIN
    CREATE TABLE [orders] (
        [Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [UserId] int NOT NULL,
        [BookId] int NOT NULL,
        [Quantity] int NOT NULL,
        [TotalPrice] decimal(10,2) NOT NULL,
        [OrderDate] datetime2 NOT NULL DEFAULT GETDATE(),
        [Status] nvarchar(50) NOT NULL DEFAULT 'Pending',
        [ShippingAddress] nvarchar(500) NULL,
        [PhoneNumber] nvarchar(20) NULL,
        CONSTRAINT [FK_orders_usersaccounts] FOREIGN KEY ([UserId]) REFERENCES [usersaccounts] ([Id]),
        CONSTRAINT [FK_orders_Book] FOREIGN KEY ([BookId]) REFERENCES [Book] ([Id])
    )
END
GO

-- Favoriler tablosu
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Favorite')
BEGIN
    CREATE TABLE [Favorite] (
        [Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [UserId] int NOT NULL,
        [BookId] int NOT NULL,
        [CreatedDate] datetime2 NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [FK_Favorite_usersaccounts] FOREIGN KEY ([UserId]) REFERENCES [usersaccounts] ([Id]),
        CONSTRAINT [FK_Favorite_Book] FOREIGN KEY ([BookId]) REFERENCES [Book] ([Id]),
        CONSTRAINT [UQ_Favorite_UserBook] UNIQUE ([UserId], [BookId])
    )
END
GO

-- JWT Refresh Token tablosu
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'JwtRefreshTokens')
BEGIN
    CREATE TABLE [JwtRefreshTokens] (
        [Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [UserId] int NOT NULL,
        [Token] nvarchar(500) NOT NULL,
        [ExpiryDate] datetime2 NOT NULL,
        [IsRevoked] bit NOT NULL DEFAULT 0,
        [CreatedDate] datetime2 NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [FK_JwtRefreshTokens_usersaccounts] FOREIGN KEY ([UserId]) REFERENCES [usersaccounts] ([Id])
    )
END
GO

-- =============================================
-- INDEXLER
-- =============================================

-- Kullanıcı adı indexi
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_usersaccounts_name')
BEGIN
    CREATE INDEX [IX_usersaccounts_name] ON [usersaccounts] ([name])
END
GO

-- Email indexi
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_usersaccounts_email')
BEGIN
    CREATE INDEX [IX_usersaccounts_email] ON [usersaccounts] ([email])
END
GO

-- Kitap başlık indexi
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Book_title')
BEGIN
    CREATE INDEX [IX_Book_title] ON [Book] ([title])
END
GO

-- Kitap yazar indexi
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Book_author')
BEGIN
    CREATE INDEX [IX_Book_author] ON [Book] ([author])
END
GO

-- Kategori indexi
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Book_CategoryId')
BEGIN
    CREATE INDEX [IX_Book_CategoryId] ON [Book] ([CategoryId])
END
GO

-- Sipariş tarih indexi
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_orders_OrderDate')
BEGIN
    CREATE INDEX [IX_orders_OrderDate] ON [orders] ([OrderDate])
END
GO

-- =============================================
-- VARSayılan VERİLER
-- =============================================

-- Varsayılan admin hesabı (BCrypt hash: Admin123!)
IF NOT EXISTS (SELECT * FROM usersaccounts WHERE name = 'admin')
BEGIN
    INSERT INTO [usersaccounts] ([name], [pass], [email], [role], [CreatedDate], [IsActive])
    VALUES ('admin', '$2a$11$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi', 'admin@admin.com', 'admin', GETDATE(), 1)
END
GO

-- Varsayılan kategoriler
IF NOT EXISTS (SELECT * FROM Category WHERE name = 'Roman')
BEGIN
    INSERT INTO [Category] ([name], [description], [CreatedDate], [IsActive])
    VALUES 
    ('Roman', 'Roman türündeki kitaplar', GETDATE(), 1),
    ('Bilim Kurgu', 'Bilim kurgu türündeki kitaplar', GETDATE(), 1),
    ('Tarih', 'Tarih kitapları', GETDATE(), 1),
    ('Bilim', 'Bilimsel kitaplar', GETDATE(), 1),
    ('Çocuk', 'Çocuk kitapları', GETDATE(), 1),
    ('Eğitim', 'Eğitim kitapları', GETDATE(), 1)
END
GO

-- Varsayılan kitaplar
IF NOT EXISTS (SELECT * FROM Book WHERE title = 'Suç ve Ceza')
BEGIN
    INSERT INTO [Book] ([title], [author], [description], [price], [stock], [imgfile], [CategoryId], [CreatedDate], [IsActive])
    VALUES 
    ('Suç ve Ceza', 'Fyodor Dostoyevski', 'Klasik Rus edebiyatının başyapıtlarından biri', 45.00, 10, 'https://cdn1.dokuzsoft.com/u/pelikankitabevi/img/b/u/m/umuttepe-objective-c-programlama-dili-mimar-aslan338fc5c49effd2447e1a7eca9326c04a.jpg', 1, GETDATE(), 1),
    ('1984', 'George Orwell', 'Distopik roman türünün önemli eserlerinden', 35.00, 15, 'https://avatars.mds.yandex.net/i?id=37a40c159f9460310e6c39675a51cf3d7a9cf7c6-5876454-images-thumbs&n=13', 2, GETDATE(), 1),
    ('Küçük Prens', 'Antoine de Saint-Exupéry', 'Çocuklar ve yetişkinler için felsefi masal', 25.00, 20, 'https://cdn.kitapsec.com/image/urun/2017/12/06/1512552355.jpg', 5, GETDATE(), 1),
    ('Atatürk', 'Andrew Mango', 'Mustafa Kemal Atatürk biyografisi', 55.00, 8, NULL, 3, GETDATE(), 1),
    ('Kozmos', 'Carl Sagan', 'Evrenin sırlarını anlatan bilim kitabı', 40.00, 12, NULL, 4, GETDATE(), 1)
END
GO

-- =============================================
-- STORED PROCEDURE'LER
-- =============================================

-- Kitap arama stored procedure'ü
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'SearchBooks')
    DROP PROCEDURE [SearchBooks]
GO

CREATE PROCEDURE [SearchBooks]
    @SearchTerm nvarchar(200) = NULL,
    @CategoryId int = NULL,
    @MinPrice decimal(10,2) = NULL,
    @MaxPrice decimal(10,2) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT b.*, c.name as CategoryName
    FROM Book b
    INNER JOIN Category c ON b.CategoryId = c.Id
    WHERE b.IsActive = 1
    AND (@SearchTerm IS NULL OR 
         b.title LIKE '%' + @SearchTerm + '%' OR 
         b.author LIKE '%' + @SearchTerm + '%' OR
         b.description LIKE '%' + @SearchTerm + '%')
    AND (@CategoryId IS NULL OR b.CategoryId = @CategoryId)
    AND (@MinPrice IS NULL OR b.price >= @MinPrice)
    AND (@MaxPrice IS NULL OR b.price <= @MaxPrice)
    ORDER BY b.title
END
GO

-- Kullanıcı sipariş geçmişi stored procedure'ü
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'GetUserOrderHistory')
    DROP PROCEDURE [GetUserOrderHistory]
GO

CREATE PROCEDURE [GetUserOrderHistory]
    @UserId int
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT o.*, b.title as BookTitle, b.author as BookAuthor, b.imgfile as BookImage
    FROM orders o
    INNER JOIN Book b ON o.BookId = b.Id
    WHERE o.UserId = @UserId
    ORDER BY o.OrderDate DESC
END
GO

-- =============================================
-- VIEW'LER
-- =============================================

-- Kitap detayları view'ü
IF EXISTS (SELECT * FROM sys.views WHERE name = 'BookDetailsView')
    DROP VIEW [BookDetailsView]
GO

CREATE VIEW [BookDetailsView] AS
SELECT 
    b.Id,
    b.title,
    b.author,
    b.description,
    b.price,
    b.stock,
    b.imgfile,
    b.CreatedDate,
    c.name as CategoryName,
    c.Id as CategoryId
FROM Book b
INNER JOIN Category c ON b.CategoryId = c.Id
WHERE b.IsActive = 1
GO

-- Sipariş özeti view'ü
IF EXISTS (SELECT * FROM sys.views WHERE name = 'OrderSummaryView')
    DROP VIEW [OrderSummaryView]
GO

CREATE VIEW [OrderSummaryView] AS
SELECT 
    o.Id,
    o.OrderDate,
    o.Status,
    o.TotalPrice,
    u.name as UserName,
    u.email as UserEmail,
    b.title as BookTitle,
    o.Quantity
FROM orders o
INNER JOIN usersaccounts u ON o.UserId = u.Id
INNER JOIN Book b ON o.BookId = b.Id
GO

-- =============================================
-- TRIGGER'LAR
-- =============================================

-- Kitap stok kontrolü trigger'ı
IF EXISTS (SELECT * FROM sys.triggers WHERE name = 'TR_Book_StockCheck')
    DROP TRIGGER [TR_Book_StockCheck]
GO

CREATE TRIGGER [TR_Book_StockCheck]
ON [orders]
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;
    
    UPDATE b
    SET b.stock = b.stock - i.Quantity
    FROM Book b
    INNER JOIN inserted i ON b.Id = i.BookId
    WHERE b.stock >= i.Quantity
    
    -- Stok yetersizse hata fırlat
    IF EXISTS (
        SELECT 1 FROM inserted i
        INNER JOIN Book b ON i.BookId = b.Id
        WHERE b.stock < i.Quantity
    )
    BEGIN
        RAISERROR ('Yetersiz stok!', 16, 1)
        ROLLBACK TRANSACTION
    END
END
GO

-- =============================================
-- VERİTABANI KURULUMU TAMAMLANDI
-- =============================================

PRINT 'Kütüphane Yönetim Sistemi veritabanı başarıyla kuruldu!'
PRINT 'Varsayılan admin hesabı: admin / Admin123!'
PRINT 'Veritabanı adı: kütüphaneuygulaması'
PRINT 'Toplam tablo sayısı: 7'
PRINT 'Toplam stored procedure sayısı: 2'
PRINT 'Toplam view sayısı: 2'
PRINT 'Toplam trigger sayısı: 1' 