/* =========================================================
   OrderManagementDemoDemo - DB Setup + Seed
   ========================================================= */

-- 0) (Optional) Drop existing DB (nur wenn du wirklich neu starten willst)
IF DB_ID('OrderManagementDemo') IS NOT NULL
BEGIN
    ALTER DATABASE OrderManagementDemo SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE OrderManagementDemo;
END
GO

-- 1) Create DB
CREATE DATABASE OrderManagementDemo;
GO

USE OrderManagementDemo;
GO

/* =========================================================
   2) Domain "Enums" als CHECK Constraints (int)
   =========================================================
   Customer.Status: 0=Active, 1=Inactive
   Article.Status:  0=Active, 1=Inactive
   ArticleGroup.Status: 0=Active, 1=Inactive
   Order.Status:    0=Draft, 1=Created, 2=Paid, 3=Shipped, 4=Cancelled
   Order.PaymentMethod: 0=Invoice, 1=Card, 2=Twint, 3=Cash
*/

-- 3) Tables

-- Customers
CREATE TABLE dbo.Customers
(
    CustomerID      INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Customers PRIMARY KEY,
    CustomerNr      NVARCHAR(7)  NOT NULL,
    LastName        NVARCHAR(100) NOT NULL,
    SurName         NVARCHAR(100) NOT NULL,
    [E-Mail]        NVARCHAR(255) NOT NULL,
    Website         NVARCHAR(255) NULL,
    PhoneNumber     NVARCHAR(30)  NULL,
    Status          INT NOT NULL CONSTRAINT DF_Customers_Status DEFAULT(0),
    PasswordHash    NVARCHAR(255) NULL,

    CONSTRAINT UQ_Customers_CustomerNr UNIQUE (CustomerNr),
    CONSTRAINT UQ_Customers_Email UNIQUE ([E-Mail]),
    CONSTRAINT CK_Customers_Status CHECK (Status IN (0,1))
);
GO

-- CustomerAddresses (historisiert, mehrere pro Customer)
CREATE TABLE dbo.CustomerAddresses
(
    CustomerAddressID   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CustomerAddresses PRIMARY KEY,
    CustomerID          INT NOT NULL,
    ValidFrom           DATE NOT NULL,
    ValidTo             DATE NULL,                 -- NULL = aktuell gültig
    Street              NVARCHAR(200) NOT NULL,
    HouseNumber         NVARCHAR(20)  NOT NULL,
    PostalCode          NVARCHAR(20)  NOT NULL,
    City                NVARCHAR(100) NOT NULL,
    CountryCode         NCHAR(2)       NOT NULL,   -- ISO (CH, DE, AT, ...)

    CONSTRAINT FK_CustomerAddresses_Customers
        FOREIGN KEY (CustomerID) REFERENCES dbo.Customers(CustomerID),

    CONSTRAINT CK_CustomerAddresses_ValidRange
        CHECK (ValidTo IS NULL OR ValidTo >= ValidFrom)
);
GO

-- ArticleGroups (Tree via ParentGroupID)
CREATE TABLE dbo.ArticleGroups
(
    ArticleGroupID   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ArticleGroups PRIMARY KEY,
    [Name]           NVARCHAR(150) NOT NULL,
    [Description]    NTEXT NULL,
    ParentGroupID    INT NULL,
    Status           INT NOT NULL CONSTRAINT DF_ArticleGroups_Status DEFAULT(0),

    CONSTRAINT FK_ArticleGroups_Parent
        FOREIGN KEY (ParentGroupID) REFERENCES dbo.ArticleGroups(ArticleGroupID),

    CONSTRAINT CK_ArticleGroups_Status CHECK (Status IN (0,1))
);
GO

-- Articles
CREATE TABLE dbo.Articles
(
    ArticleID        INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Articles PRIMARY KEY,
    ArticleNr        NVARCHAR(20)  NOT NULL,
    [Name]           NVARCHAR(200) NOT NULL,
    ArticleGroupID   INT NOT NULL,

    PriceAmount      DECIMAL(18,2) NOT NULL,
    PriceCurrency    NCHAR(3) NOT NULL,          -- ISO 4217 (CHF, EUR, ...)
    VatRate          DECIMAL(5,2) NOT NULL,      -- z.B. 7.70
    Stock            INT NOT NULL,
    [Description]    NTEXT NULL,
    Status           INT NOT NULL CONSTRAINT DF_Articles_Status DEFAULT(0),

    CONSTRAINT UQ_Articles_ArticleNr UNIQUE (ArticleNr),
    CONSTRAINT FK_Articles_ArticleGroups
        FOREIGN KEY (ArticleGroupID) REFERENCES dbo.ArticleGroups(ArticleGroupID),

    CONSTRAINT CK_Articles_Status CHECK (Status IN (0,1)),
    CONSTRAINT CK_Articles_Currency CHECK (PriceCurrency LIKE '[A-Z][A-Z][A-Z]'),
    CONSTRAINT CK_Articles_Stock CHECK (Stock >= 0),
    CONSTRAINT CK_Articles_VatRate CHECK (VatRate >= 0 AND VatRate <= 100)
);
GO

-- Orders
CREATE TABLE dbo.Orders
(
    OrderID             INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Orders PRIMARY KEY,
    OrderNr             NVARCHAR(20) NOT NULL,
    CustomerID          INT NOT NULL,
    OrderDate           DATETIME2 NOT NULL CONSTRAINT DF_Orders_OrderDate DEFAULT(SYSDATETIME()),

    Status              INT NOT NULL CONSTRAINT DF_Orders_Status DEFAULT(1),
    PaymentMethod       INT NOT NULL CONSTRAINT DF_Orders_PaymentMethod DEFAULT(0),

    TotalAmount         DECIMAL(18,2) NOT NULL CONSTRAINT DF_Orders_TotalAmount DEFAULT(0),
    TotalCurrency       NCHAR(3) NOT NULL CONSTRAINT DF_Orders_TotalCurrency DEFAULT('CHF'),

    DeliveryStreet      NVARCHAR(200) NOT NULL,
    DeliveryHouseNumber NVARCHAR(20)  NOT NULL,
    DeliveryPostalCode  NVARCHAR(20)  NOT NULL,
    DeliveryCity        NVARCHAR(100) NOT NULL,
    DeliveryCountryCode NCHAR(2)      NOT NULL,

    CONSTRAINT UQ_Orders_OrderNr UNIQUE (OrderNr),
    CONSTRAINT FK_Orders_Customers
        FOREIGN KEY (CustomerID) REFERENCES dbo.Customers(CustomerID),

    CONSTRAINT CK_Orders_Status CHECK (Status IN (0,1,2,3,4)),
    CONSTRAINT CK_Orders_PaymentMethod CHECK (PaymentMethod IN (0,1,2,3)),
    CONSTRAINT CK_Orders_Currency CHECK (TotalCurrency LIKE '[A-Z][A-Z][A-Z]')
);
GO

-- OrderLines
CREATE TABLE dbo.OrderLines
(
    OrderLineID      INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_OrderLines PRIMARY KEY,
    OrderID          INT NOT NULL,
    LineNr           INT NOT NULL,
    ArticleID        INT NOT NULL,

    ArticleName      NVARCHAR(200) NOT NULL,      -- Snapshot
    UnitPriceAmount  DECIMAL(18,2) NOT NULL,
    UnitPriceCurrency NCHAR(3) NOT NULL,
    Quantity         INT NOT NULL,
    LineTotalAmount  DECIMAL(18,2) NOT NULL,
    LineTotalCurrency NCHAR(3) NOT NULL,

    CONSTRAINT FK_OrderLines_Orders
        FOREIGN KEY (OrderID) REFERENCES dbo.Orders(OrderID) ON DELETE CASCADE,

    CONSTRAINT FK_OrderLines_Articles
        FOREIGN KEY (ArticleID) REFERENCES dbo.Articles(ArticleID),

    CONSTRAINT UQ_OrderLines_Order_LineNr UNIQUE (OrderID, LineNr),
    CONSTRAINT CK_OrderLines_Qty CHECK (Quantity > 0),
    CONSTRAINT CK_OrderLines_Currency1 CHECK (UnitPriceCurrency LIKE '[A-Z][A-Z][A-Z]'),
    CONSTRAINT CK_OrderLines_Currency2 CHECK (LineTotalCurrency LIKE '[A-Z][A-Z][A-Z]')
);
GO

-- Helpful indexes
CREATE INDEX IX_CustomerAddresses_CustomerID ON dbo.CustomerAddresses(CustomerID);
CREATE INDEX IX_Articles_GroupID ON dbo.Articles(ArticleGroupID);
CREATE INDEX IX_Orders_CustomerID ON dbo.Orders(CustomerID);
CREATE INDEX IX_OrderLines_OrderID ON dbo.OrderLines(OrderID);
CREATE INDEX IX_ArticleGroups_Parent ON dbo.ArticleGroups(ParentGroupID);
GO

/* =========================================================
   4) Seed Data
   ========================================================= */

-- Customers
INSERT INTO dbo.Customers (CustomerNr, LastName, SurName, [E-Mail], Website, PhoneNumber, Status, PasswordHash)
VALUES
('C-00001','Müller','Edi','edi.mueller@example.com','https://example.com','+41 79 111 11 11',0,'hash-1'),
('C-00002','Meier','Nadine','nadine.meier@example.com',NULL,'+41 79 222 22 22',0,'hash-2'),
('C-00003','Schmid','Joël','joel.schmid@example.com',NULL,'+41 79 333 33 33',0,'hash-3'),
('C-00004','Keller','Amin','amin.keller@example.com','https://amin.dev',NULL,0,'hash-4'),
('C-00005','Weber','Lena','lena.weber@example.com',NULL,NULL,1,'hash-5');

-- CustomerAddresses (mehrere pro Customer, historisiert)
INSERT INTO dbo.CustomerAddresses (CustomerID, ValidFrom, ValidTo, Street, HouseNumber, PostalCode, City, CountryCode)
VALUES
(1,'2024-01-01','2024-12-31','Bahnhofstrasse','10','8001','Zürich','CH'),
(1,'2025-01-01',NULL,'Seestrasse','55a','8002','Zürich','CH'),
(2,'2025-02-01',NULL,'Hauptstrasse','5','3000','Bern','CH'),
(3,'2024-06-01','2025-05-31','Industriestrasse','21','4000','Basel','CH'),
(3,'2025-06-01',NULL,'Rheinweg','3','4051','Basel','CH'),
(4,'2025-03-01',NULL,'Marktgasse','7','9000','St. Gallen','CH'),
(5,'2023-01-01','2023-12-31','Alpenweg','1','6003','Luzern','CH'),
(5,'2024-01-01','2024-12-31','Pilatusstrasse','12','6003','Luzern','CH');

-- ArticleGroups (Tree)
-- Root
INSERT INTO dbo.ArticleGroups ([Name], [Description], ParentGroupID, Status)
VALUES
('Fenster','Root Gruppe Fenster',NULL,0),
('Zubehör','Root Gruppe Zubehör',NULL,0),
('Dienstleistungen','Root Gruppe Dienstleistungen',NULL,0);

-- Children of Fenster
INSERT INTO dbo.ArticleGroups ([Name], [Description], ParentGroupID, Status)
VALUES
('Holzfenster','Untergruppe',1,0),
('Kunststofffenster','Untergruppe',1,0),
('Alufenster','Untergruppe',1,0);

-- Children of Zubehör
INSERT INTO dbo.ArticleGroups ([Name], [Description], ParentGroupID, Status)
VALUES
('Beschläge','Untergruppe',2,0),
('Dichtungen','Untergruppe',2,0);

-- Child of Beschläge
INSERT INTO dbo.ArticleGroups ([Name], [Description], ParentGroupID, Status)
VALUES
('Scharniere','Untergruppe',7,0);

-- Articles (10+)
INSERT INTO dbo.Articles (ArticleNr, [Name], ArticleGroupID, PriceAmount, PriceCurrency, VatRate, Stock, [Description], Status)
VALUES
('A-1000','Holzfenster Standard 80x120',4, 450.00,'CHF',7.70, 25, 'Standard Holzfenster',0),
('A-1001','Holzfenster Premium 90x130',4, 620.00,'CHF',7.70, 10, 'Premium Holzfenster',0),
('A-2000','Kunststofffenster Basic 80x120',5, 320.00,'CHF',7.70, 40, 'Basic Kunststofffenster',0),
('A-2001','Kunststofffenster Comfort 90x130',5, 410.00,'CHF',7.70, 18, 'Comfort Kunststofffenster',0),
('A-3000','Alufenster Slim 80x120',6, 780.00,'CHF',7.70, 8, 'Alu Slim',0),
('A-4000','Beschlag Set Standard',7, 35.50,'CHF',7.70, 200,'Beschläge Set',0),
('A-4001','Dichtung EPDM 10m',8, 12.90,'CHF',7.70, 500,'Dichtung',0),
('A-4100','Scharnier HeavyDuty',9, 9.90,'CHF',7.70, 300,'Scharnier',0),
('S-9000','Montage vor Ort',3, 150.00,'CHF',7.70, 9999,'Dienstleistung Montage',0),
('S-9001','Wartungspaket 1 Jahr',3,  90.00,'CHF',7.70, 9999,'Dienstleistung Wartung',0);

-- Orders (Lieferadresse = Snapshot; in der Praxis kommt sie aus "aktuell gültiger CustomerAddress")
INSERT INTO dbo.Orders
(OrderNr, CustomerID, OrderDate, Status, PaymentMethod, TotalAmount, TotalCurrency,
 DeliveryStreet, DeliveryHouseNumber, DeliveryPostalCode, DeliveryCity, DeliveryCountryCode)
VALUES
('O-2025-0001',1,'2025-08-01',1,0,0,'CHF','Seestrasse','55a','8002','Zürich','CH'),
('O-2025-0002',2,'2025-08-03',2,1,0,'CHF','Hauptstrasse','5','3000','Bern','CH'),
('O-2025-0003',3,'2025-08-05',1,2,0,'CHF','Rheinweg','3','4051','Basel','CH'),
('O-2025-0004',1,'2025-08-10',3,0,0,'CHF','Seestrasse','55a','8002','Zürich','CH'),
('O-2025-0005',4,'2025-08-12',1,3,0,'CHF','Marktgasse','7','9000','St. Gallen','CH'),
('O-2025-0006',3,'2025-08-20',4,0,0,'CHF','Rheinweg','3','4051','Basel','CH');

-- OrderLines (Snapshot Artikelname + Preis; line totals berechnen wir fix im Seed)
INSERT INTO dbo.OrderLines
(OrderID, LineNr, ArticleID, ArticleName, UnitPriceAmount, UnitPriceCurrency, Quantity, LineTotalAmount, LineTotalCurrency)
VALUES
(1,1,1,'Holzfenster Standard 80x120',450.00,'CHF',2, 900.00,'CHF'),
(1,2,6,'Beschlag Set Standard',35.50,'CHF',4, 142.00,'CHF'),
(1,3,9,'Montage vor Ort',150.00,'CHF',1, 150.00,'CHF'),

(2,1,3,'Kunststofffenster Basic 80x120',320.00,'CHF',3, 960.00,'CHF'),
(2,2,7,'Dichtung EPDM 10m',12.90,'CHF',5,  64.50,'CHF'),

(3,1,5,'Alufenster Slim 80x120',780.00,'CHF',1, 780.00,'CHF'),
(3,2,10,'Wartungspaket 1 Jahr',90.00,'CHF',1,  90.00,'CHF'),

(4,1,2,'Holzfenster Premium 90x130',620.00,'CHF',1, 620.00,'CHF'),
(4,2,6,'Beschlag Set Standard',35.50,'CHF',2,  71.00,'CHF'),
(4,3,9,'Montage vor Ort',150.00,'CHF',1, 150.00,'CHF'),

(5,1,4,'Kunststofffenster Comfort 90x130',410.00,'CHF',2, 820.00,'CHF'),
(5,2,8,'Scharnier HeavyDuty',9.90,'CHF',6,  59.40,'CHF'),

(6,1,1,'Holzfenster Standard 80x120',450.00,'CHF',1, 450.00,'CHF'),
(6,2,7,'Dichtung EPDM 10m',12.90,'CHF',2,  25.80,'CHF');

-- Order totals aus Lines berechnen (damit Daten konsistent sind)
UPDATE o
SET
    TotalAmount = x.SumLines,
    TotalCurrency = 'CHF'
FROM dbo.Orders o
JOIN (
    SELECT OrderID, SUM(LineTotalAmount) AS SumLines
    FROM dbo.OrderLines
    GROUP BY OrderID
) x ON x.OrderID = o.OrderID;

GO
