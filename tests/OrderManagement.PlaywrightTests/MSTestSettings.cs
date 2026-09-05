// Every test in this assembly shares one application process, one SQL Server database, and
// mutable seeded orders/stock (see PlaywrightAppFixture). Running them in parallel lets a
// stock-mutating test (e.g. OrderDeletionTests) race with tests reading the same rows, so
// execution is explicitly serialised rather than left to MSTest's default.
[assembly: DoNotParallelize]
