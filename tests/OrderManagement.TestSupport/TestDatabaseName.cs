using Microsoft.Data.SqlClient;

namespace OrderManagement.TestSupport
{
    public static class TestDatabaseName
    {
        public static string Create(string prefix, string? discriminator = null)
        {
            string safeDiscriminator = discriminator is null
                ? string.Empty
                : new string([.. discriminator.Where(char.IsLetterOrDigit).Take(45)]);

            return safeDiscriminator.Length > 0
                ? $"{prefix}_{safeDiscriminator}_{Guid.NewGuid():N}"
                : $"{prefix}_{Guid.NewGuid():N}";
        }

        public static string BuildScopedConnectionString(string masterConnectionString, string databaseName)
        {
            var builder = new SqlConnectionStringBuilder(masterConnectionString)
            {
                InitialCatalog = databaseName,
                TrustServerCertificate = true,
                MultipleActiveResultSets = true
            };

            return builder.ConnectionString;
        }
    }
}
