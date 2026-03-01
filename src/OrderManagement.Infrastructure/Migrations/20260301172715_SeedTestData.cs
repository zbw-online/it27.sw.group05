using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedTestData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ============================================================
            // 1. ARTICLE GROUPS (Hierarchie: Root -> Children)
            // ============================================================
            _ = migrationBuilder.Sql(@"
                -- Temporal Table temporär deaktivieren
                ALTER TABLE ArticleGroups SET (SYSTEM_VERSIONING = OFF);

                -- Root Groups (KEIN IDENTITY_INSERT nötig!)
                INSERT INTO ArticleGroups (ArticleGroupId, Name, ParentGroupId, Description, Status)
                VALUES 
                    (1, 'Elektronik', NULL, 'Elektronische Geräte und Zubehör', 1),
                    (2, 'Bürobedarf', NULL, 'Büromaterial und Schreibwaren', 1),
                    (3, 'Möbel', NULL, 'Büromöbel und Einrichtung', 1);

                -- Child Groups (Elektronik)
                INSERT INTO ArticleGroups (ArticleGroupId, Name, ParentGroupId, Description, Status)
                VALUES 
                    (4, 'Computer', 1, 'Desktop und Laptops', 1),
                    (5, 'Peripherie', 1, 'Mäuse, Tastaturen, Monitore', 1),
                    (6, 'Netzwerk', 1, 'Router, Switches, Kabel', 1);

                -- Child Groups (Bürobedarf)
                INSERT INTO ArticleGroups (ArticleGroupId, Name, ParentGroupId, Description, Status)
                VALUES 
                    (7, 'Schreibwaren', 2, 'Stifte, Blöcke, Papier', 1),
                    (8, 'Ordnungssysteme', 2, 'Ordner, Mappen, Register', 1);

                -- Child Groups (Möbel)
                INSERT INTO ArticleGroups (ArticleGroupId, Name, ParentGroupId, Description, Status)
                VALUES 
                    (9, 'Tische', 3, 'Schreibtische und Besprechungstische', 1),
                    (10, 'Stühle', 3, 'Bürostühle und Besucherstühle', 1);
                
                -- Temporal Table wieder aktivieren
                ALTER TABLE ArticleGroups SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.ArticleGroupsHistory));
            ");

            // ============================================================
            // 2. ARTICLES (mit realistischen Preisen und Lagerbestand)
            // ============================================================
            _ = migrationBuilder.Sql(@"
                -- Temporal Table temporär deaktivieren
                ALTER TABLE Articles SET (SYSTEM_VERSIONING = OFF);
                
                -- Elektronik > Computer
                INSERT INTO Articles (ArticleId, ArticleNumber, Name, PriceAmount, PriceCurrency, ArticleGroupId, Stock, VatRate, Description, Status)
                VALUES 
                    (1, 'ART-001', 'Dell Latitude 5540 Laptop', 1299.00, 'CHF', 4, 25, 7.70, 'Intel Core i7, 16GB RAM, 512GB SSD', 1),
                    (2, 'ART-002', 'HP EliteDesk 800 G9 Desktop', 899.00, 'CHF', 4, 15, 7.70, 'Intel Core i5, 8GB RAM, 256GB SSD', 1),
                    (3, 'ART-003', 'Lenovo ThinkPad X1 Carbon', 1899.00, 'CHF', 4, 10, 7.70, 'Intel Core i7, 32GB RAM, 1TB SSD', 1);

                -- Elektronik > Peripherie
                INSERT INTO Articles (ArticleId, ArticleNumber, Name, PriceAmount, PriceCurrency, ArticleGroupId, Stock, VatRate, Description, Status)
                VALUES 
                    (4, 'ART-004', 'Logitech MX Master 3S Maus', 109.00, 'CHF', 5, 50, 7.70, 'Kabellose Maus mit Präzisionssensor', 1),
                    (5, 'ART-005', 'Logitech MX Keys Tastatur', 129.00, 'CHF', 5, 40, 7.70, 'Kabellose beleuchtete Tastatur', 1),
                    (6, 'ART-006', 'Dell UltraSharp U2723DE Monitor', 599.00, 'CHF', 5, 20, 7.70, '27 Zoll QHD IPS Monitor', 1);

                -- Elektronik > Netzwerk
                INSERT INTO Articles (ArticleId, ArticleNumber, Name, PriceAmount, PriceCurrency, ArticleGroupId, Stock, VatRate, Description, Status)
                VALUES 
                    (7, 'ART-007', 'Cisco Catalyst 1000 Switch', 450.00, 'CHF', 6, 12, 7.70, '24-Port Gigabit Ethernet Switch', 1),
                    (8, 'ART-008', 'UniFi Dream Machine Pro', 499.00, 'CHF', 6, 8, 7.70, 'Enterprise Gateway Router', 1);

                -- Bürobedarf > Schreibwaren
                INSERT INTO Articles (ArticleId, ArticleNumber, Name, PriceAmount, PriceCurrency, ArticleGroupId, Stock, VatRate, Description, Status)
                VALUES 
                    (9, 'ART-009', 'Pelikan Souverän M800 Füller', 389.00, 'CHF', 7, 5, 7.70, 'Hochwertiger Füllfederhalter', 1),
                    (10, 'ART-010', 'Moleskine Classic Notebook A4', 29.90, 'CHF', 7, 100, 7.70, 'Liniertes Notizbuch, 192 Seiten', 1),
                    (11, 'ART-011', 'Stabilo BOSS Original 10er Set', 15.90, 'CHF', 7, 200, 7.70, 'Textmarker in 10 Farben', 1);

                -- Bürobedarf > Ordnungssysteme
                INSERT INTO Articles (ArticleId, ArticleNumber, Name, PriceAmount, PriceCurrency, ArticleGroupId, Stock, VatRate, Description, Status)
                VALUES 
                    (12, 'ART-012', 'Leitz Ordner 1080 10er Pack', 49.90, 'CHF', 8, 150, 7.70, 'A4 Ordner 80mm Rücken', 1),
                    (13, 'ART-013', 'Durable Hängeregistratur Set', 79.00, 'CHF', 8, 30, 7.70, 'Komplett-Set für Schublade', 1);

                -- Möbel > Tische
                INSERT INTO Articles (ArticleId, ArticleNumber, Name, PriceAmount, PriceCurrency, ArticleGroupId, Stock, VatRate, Description, Status)
                VALUES 
                    (14, 'ART-014', 'IKEA BEKANT Schreibtisch', 349.00, 'CHF', 9, 20, 7.70, '160x80cm höhenverstellbar', 1),
                    (15, 'ART-015', 'USM Haller Tisch 175x75', 2890.00, 'CHF', 9, 5, 7.70, 'Designer Schreibtisch in Chrom', 1);

                -- Möbel > Stühle
                INSERT INTO Articles (ArticleId, ArticleNumber, Name, PriceAmount, PriceCurrency, ArticleGroupId, Stock, VatRate, Description, Status)
                VALUES 
                    (16, 'ART-016', 'Herman Miller Aeron Chair', 1499.00, 'CHF', 10, 12, 7.70, 'Ergonomischer Bürostuhl Größe B', 1),
                    (17, 'ART-017', 'IKEA JÄRVFJÄLLET Bürostuhl', 249.00, 'CHF', 10, 40, 7.70, 'Mit Armlehnen, verstellbar', 1),
                    (18, 'ART-018', 'Vitra Physix Konferenzstuhl', 899.00, 'CHF', 10, 25, 7.70, 'Besucherstuhl mit Netzrücken', 1);
                
                -- Temporal Table wieder aktivieren
                ALTER TABLE Articles SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.ArticlesHistory));
            ");

            // ============================================================
            // 3. CUSTOMERS (diverse Testfälle mit Adressen)
            // ============================================================
            _ = migrationBuilder.Sql(@"
                -- Temporal Table temporär deaktivieren
                ALTER TABLE Customers SET (SYSTEM_VERSIONING = OFF);
                
                -- Customer 1: Standard Geschäftskunde mit aktueller Adresse
                INSERT INTO Customers (CustomerId, CustomerNumber, LastName, SurName, Email, Website, PasswordHash)
                VALUES (1, 'C-00001', 'Meier', 'Hans', 'hans.meier@musterag.ch', 'https://www.musterag.ch', 
                        '$2a$11$XjZHQzNkYmQ5ZjE4ZjE4Z.hashed_password_example1');

                -- Customer 2: Kunde mit Adresswechsel (alte + neue Adresse)
                INSERT INTO Customers (CustomerId, CustomerNumber, LastName, SurName, Email, Website, PasswordHash)
                VALUES (2, 'C-00002', 'Schmidt', 'Anna', 'anna.schmidt@techgmbh.ch', 'https://www.techgmbh.ch', 
                        '$2a$11$XjZHQzNkYmQ5ZjE4ZjE4Z.hashed_password_example2');

                -- Customer 3: Privatkunde ohne Website
                INSERT INTO Customers (CustomerId, CustomerNumber, LastName, SurName, Email, Website, PasswordHash)
                VALUES (3, 'C-00003', 'Keller', 'Beat', 'beat.keller@gmail.com', NULL, 
                        '$2a$11$XjZHQzNkYmQ5ZjE4ZjE4Z.hashed_password_example3');

                -- Customer 4: Internationaler Kunde (Deutschland)
                INSERT INTO Customers (CustomerId, CustomerNumber, LastName, SurName, Email, Website, PasswordHash)
                VALUES (4, 'C-00004', 'Weber', 'Claudia', 'c.weber@example.de', 'https://www.weber-consulting.de', 
                        '$2a$11$XjZHQzNkYmQ5ZjE4ZjE4Z.hashed_password_example4');

                -- Customer 5: Kunde mit langem Namen (Boundary Test)
                INSERT INTO Customers (CustomerId, CustomerNumber, LastName, SurName, Email, Website, PasswordHash)
                VALUES (5, 'C-00005', 'Müller-Lüdenscheidt', 'Karl-Theodor Maria Nikolaus Johann Jacob Philipp Wilhelm Franz Joseph Sylvester', 
                        'kt.mueller-luedenscheidt@example.ch', NULL, 
                        '$2a$11$XjZHQzNkYmQ5ZjE4ZjE4Z.hashed_password_example5');
                
                -- Temporal Table wieder aktivieren
                ALTER TABLE Customers SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.CustomersHistory));
            ");

            _ = migrationBuilder.Sql(@"
                -- Temporal Table für CustomerAddresses temporär deaktivieren
                ALTER TABLE CustomerAddresses SET (SYSTEM_VERSIONING = OFF);
                
                INSERT INTO CustomerAddresses (CustomerId, ValidFrom, ValidTo, Street, HouseNumber, PostalCode, City, CountryCode)
                VALUES (1, '2023-01-01', NULL, 'Bahnhofstrasse', '15', '8001', 'Zürich', 'CH');

                INSERT INTO CustomerAddresses (CustomerId, ValidFrom, ValidTo, Street, HouseNumber, PostalCode, City, CountryCode)
                VALUES 
                    (2, '2022-06-01', '2024-12-31', 'Seestrasse', '88', '8002', 'Zürich', 'CH'),
                    (2, '2025-01-01', NULL, 'Limmatquai', '12', '8001', 'Zürich', 'CH');

                INSERT INTO CustomerAddresses (CustomerId, ValidFrom, ValidTo, Street, HouseNumber, PostalCode, City, CountryCode)
                VALUES (3, '2024-03-15', NULL, 'Riedstrasse', '7a', '8953', 'Dietikon', 'CH');

                INSERT INTO CustomerAddresses (CustomerId, ValidFrom, ValidTo, Street, HouseNumber, PostalCode, City, CountryCode)
                VALUES (4, '2023-09-01', NULL, 'Hauptstrasse', '42', '80331', 'München', 'DE');

                INSERT INTO CustomerAddresses (CustomerId, ValidFrom, ValidTo, Street, HouseNumber, PostalCode, City, CountryCode)
                VALUES (5, '2024-06-01', NULL, 'Langstrasse', '123', '8004', 'Zürich', 'CH');
                
                -- Temporal Table wieder aktivieren
                ALTER TABLE CustomerAddresses SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.CustomerAddressesHistory));
            ");

            // ============================================================
            // 4. ORDERS (realistische Bestellungen mit OrderLines)
            // ============================================================
            _ = migrationBuilder.Sql(@"
                -- Temporal Table temporär deaktivieren
                ALTER TABLE Orders SET (SYSTEM_VERSIONING = OFF);
                
                -- Order 1: Einfache Bestellung (Customer 1, 2 Artikel)
                INSERT INTO Orders (OrderNumber, OrderDate, CustomerId, 
                                    DeliveryStreet, DeliveryHouseNumber, DeliveryPostalCode, DeliveryCity, DeliveryCountryCode,
                                    TotalAmount, TotalCurrency)
                VALUES ('ORD-2025-001', '2025-02-15 10:30:00', 1,
                        'Bahnhofstrasse', '15', '8001', 'Zürich', 'CH',
                        1408.00, 'CHF');

                -- Order 2: Größere Bestellung (Customer 2, mehrere Positionen)
                INSERT INTO Orders (OrderNumber, OrderDate, CustomerId, 
                                    DeliveryStreet, DeliveryHouseNumber, DeliveryPostalCode, DeliveryCity, DeliveryCountryCode,
                                    TotalAmount, TotalCurrency)
                VALUES ('ORD-2025-002', '2025-02-20 14:15:00', 2,
                        'Limmatquai', '12', '8001', 'Zürich', 'CH',
                        4355.00, 'CHF');

                -- Order 3: Büromöbel-Bestellung (Customer 3)
                INSERT INTO Orders (OrderNumber, OrderDate, CustomerId, 
                                    DeliveryStreet, DeliveryHouseNumber, DeliveryPostalCode, DeliveryCity, DeliveryCountryCode,
                                    TotalAmount, TotalCurrency)
                VALUES ('ORD-2025-003', '2025-02-22 09:00:00', 3,
                        'Riedstrasse', '7a', '8953', 'Dietikon', 'CH',
                        1997.00, 'CHF');

                -- Order 4: Komplette Büroausstattung (Customer 1)
                INSERT INTO Orders (OrderNumber, OrderDate, CustomerId, 
                                    DeliveryStreet, DeliveryHouseNumber, DeliveryPostalCode, DeliveryCity, DeliveryCountryCode,
                                    TotalAmount, TotalCurrency)
                VALUES ('ORD-2025-004', '2025-02-25 16:45:00', 1,
                        'Bahnhofstrasse', '15', '8001', 'Zürich', 'CH',
                        5192.00, 'CHF');

                -- Order 5: Kleine Schreibwaren-Bestellung (Customer 4)
                INSERT INTO Orders (OrderNumber, OrderDate, CustomerId, 
                                    DeliveryStreet, DeliveryHouseNumber, DeliveryPostalCode, DeliveryCity, DeliveryCountryCode,
                                    TotalAmount, TotalCurrency)
                VALUES ('ORD-2025-005', '2025-02-28 11:20:00', 4,
                        'Hauptstrasse', '42', '80331', 'München', 'DE',
                        278.90, 'CHF');
                
                -- Temporal Table wieder aktivieren
                ALTER TABLE Orders SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.OrdersHistory));
            ");

            _ = migrationBuilder.Sql(@"
                -- Temporal Table temporär deaktivieren
                ALTER TABLE OrderLines SET (SYSTEM_VERSIONING = OFF);
                
                -- OrderLines für Order 1
                INSERT INTO OrderLines (OrderId, LineNumber, ArticleId, ArticleName, Quantity, 
                                        UnitPriceAmount, UnitPriceCurrency, LineTotalAmount, LineTotalCurrency)
                SELECT OrderId, 1, 1, 'Dell Latitude 5540 Laptop', 1, 1299.00, 'CHF', 1299.00, 'CHF'
                FROM Orders WHERE OrderNumber = 'ORD-2025-001';
                
                INSERT INTO OrderLines (OrderId, LineNumber, ArticleId, ArticleName, Quantity, 
                                        UnitPriceAmount, UnitPriceCurrency, LineTotalAmount, LineTotalCurrency)
                SELECT OrderId, 2, 4, 'Logitech MX Master 3S Maus', 1, 109.00, 'CHF', 109.00, 'CHF'
                FROM Orders WHERE OrderNumber = 'ORD-2025-001';

                -- OrderLines für Order 2
                INSERT INTO OrderLines (OrderId, LineNumber, ArticleId, ArticleName, Quantity, 
                                        UnitPriceAmount, UnitPriceCurrency, LineTotalAmount, LineTotalCurrency)
                SELECT OrderId, 1, 3, 'Lenovo ThinkPad X1 Carbon', 2, 1899.00, 'CHF', 3798.00, 'CHF'
                FROM Orders WHERE OrderNumber = 'ORD-2025-002';
                
                INSERT INTO OrderLines (OrderId, LineNumber, ArticleId, ArticleName, Quantity, 
                                        UnitPriceAmount, UnitPriceCurrency, LineTotalAmount, LineTotalCurrency)
                SELECT OrderId, 2, 5, 'Logitech MX Keys Tastatur', 2, 129.00, 'CHF', 258.00, 'CHF'
                FROM Orders WHERE OrderNumber = 'ORD-2025-002';
                
                INSERT INTO OrderLines (OrderId, LineNumber, ArticleId, ArticleName, Quantity, 
                                        UnitPriceAmount, UnitPriceCurrency, LineTotalAmount, LineTotalCurrency)
                SELECT OrderId, 3, 10, 'Moleskine Classic Notebook A4', 10, 29.90, 'CHF', 299.00, 'CHF'
                FROM Orders WHERE OrderNumber = 'ORD-2025-002';

                -- OrderLines für Order 3
                INSERT INTO OrderLines (OrderId, LineNumber, ArticleId, ArticleName, Quantity, 
                                        UnitPriceAmount, UnitPriceCurrency, LineTotalAmount, LineTotalCurrency)
                SELECT OrderId, 1, 16, 'Herman Miller Aeron Chair', 1, 1499.00, 'CHF', 1499.00, 'CHF'
                FROM Orders WHERE OrderNumber = 'ORD-2025-003';
                
                INSERT INTO OrderLines (OrderId, LineNumber, ArticleId, ArticleName, Quantity, 
                                        UnitPriceAmount, UnitPriceCurrency, LineTotalAmount, LineTotalCurrency)
                SELECT OrderId, 2, 17, 'IKEA JÄRVFJÄLLET Bürostuhl', 2, 249.00, 'CHF', 498.00, 'CHF'
                FROM Orders WHERE OrderNumber = 'ORD-2025-003';

                -- OrderLines für Order 4
                INSERT INTO OrderLines (OrderId, LineNumber, ArticleId, ArticleName, Quantity, 
                                        UnitPriceAmount, UnitPriceCurrency, LineTotalAmount, LineTotalCurrency)
                SELECT OrderId, 1, 2, 'HP EliteDesk 800 G9 Desktop', 3, 899.00, 'CHF', 2697.00, 'CHF'
                FROM Orders WHERE OrderNumber = 'ORD-2025-004';
                
                INSERT INTO OrderLines (OrderId, LineNumber, ArticleId, ArticleName, Quantity, 
                                        UnitPriceAmount, UnitPriceCurrency, LineTotalAmount, LineTotalCurrency)
                SELECT OrderId, 2, 6, 'Dell UltraSharp U2723DE Monitor', 3, 599.00, 'CHF', 1797.00, 'CHF'
                FROM Orders WHERE OrderNumber = 'ORD-2025-004';
                
                INSERT INTO OrderLines (OrderId, LineNumber, ArticleId, ArticleName, Quantity, 
                                        UnitPriceAmount, UnitPriceCurrency, LineTotalAmount, LineTotalCurrency)
                SELECT OrderId, 3, 14, 'IKEA BEKANT Schreibtisch', 2, 349.00, 'CHF', 698.00, 'CHF'
                FROM Orders WHERE OrderNumber = 'ORD-2025-004';

                -- OrderLines für Order 5
                INSERT INTO OrderLines (OrderId, LineNumber, ArticleId, ArticleName, Quantity, 
                                        UnitPriceAmount, UnitPriceCurrency, LineTotalAmount, LineTotalCurrency)
                SELECT OrderId, 1, 11, 'Stabilo BOSS Original 10er Set', 5, 15.90, 'CHF', 79.50, 'CHF'
                FROM Orders WHERE OrderNumber = 'ORD-2025-005';
                
                INSERT INTO OrderLines (OrderId, LineNumber, ArticleId, ArticleName, Quantity, 
                                        UnitPriceAmount, UnitPriceCurrency, LineTotalAmount, LineTotalCurrency)
                SELECT OrderId, 2, 12, 'Leitz Ordner 1080 10er Pack', 1, 49.90, 'CHF', 49.90, 'CHF'
                FROM Orders WHERE OrderNumber = 'ORD-2025-005';
                
                INSERT INTO OrderLines (OrderId, LineNumber, ArticleId, ArticleName, Quantity, 
                                        UnitPriceAmount, UnitPriceCurrency, LineTotalAmount, LineTotalCurrency)
                SELECT OrderId, 3, 10, 'Moleskine Classic Notebook A4', 5, 29.90, 'CHF', 149.50, 'CHF'
                FROM Orders WHERE OrderNumber = 'ORD-2025-005';
                
                -- Temporal Table wieder aktivieren
                ALTER TABLE OrderLines SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.OrderLinesHistory));
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Temporal Tables temporär deaktivieren vor dem Löschen
            _ = migrationBuilder.Sql(@"
                ALTER TABLE OrderLines SET (SYSTEM_VERSIONING = OFF);
                ALTER TABLE Orders SET (SYSTEM_VERSIONING = OFF);
                ALTER TABLE CustomerAddresses SET (SYSTEM_VERSIONING = OFF);
                ALTER TABLE Customers SET (SYSTEM_VERSIONING = OFF);
                ALTER TABLE Articles SET (SYSTEM_VERSIONING = OFF);
                ALTER TABLE ArticleGroups SET (SYSTEM_VERSIONING = OFF);
            ");

            // Testdaten in umgekehrter Reihenfolge löschen (Foreign Key Constraints beachten)
            _ = migrationBuilder.Sql("DELETE FROM OrderLines;");
            _ = migrationBuilder.Sql("DELETE FROM Orders;");
            _ = migrationBuilder.Sql("DELETE FROM CustomerAddresses;");
            _ = migrationBuilder.Sql("DELETE FROM Customers;");
            _ = migrationBuilder.Sql("DELETE FROM Articles;");
            _ = migrationBuilder.Sql("DELETE FROM ArticleGroups;");

            // Temporal Tables wieder aktivieren
            _ = migrationBuilder.Sql(@"
                ALTER TABLE OrderLines SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.OrderLinesHistory));
                ALTER TABLE Orders SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.OrdersHistory));
                ALTER TABLE CustomerAddresses SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.CustomerAddressesHistory));
                ALTER TABLE Customers SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.CustomersHistory));
                ALTER TABLE Articles SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.ArticlesHistory));
                ALTER TABLE ArticleGroups SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.ArticleGroupsHistory));
            ");
        }
    }
}
