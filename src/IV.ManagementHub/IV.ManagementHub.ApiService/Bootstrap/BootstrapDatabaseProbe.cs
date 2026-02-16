using System.Data.Common;
using Npgsql;

namespace IV.ManagementHub.ApiService.Bootstrap
{
    internal static class BootstrapDatabaseProbe
    {
        public static bool HasDxCoreSignature(BootstrapInstanceSettings instance)
        {
            if (instance is null ||
                string.IsNullOrWhiteSpace(instance.DatabaseType) ||
                string.IsNullOrWhiteSpace(instance.ConnectionString))
            {
                return false;
            }

            var type = instance.DatabaseType.Trim().ToLowerInvariant();
            if (type is not ("postgres" or "postgresql" or "npgsql"))
            {
                return false;
            }

            try
            {
                using (DbConnection connection = new NpgsqlConnection(instance.ConnectionString))
                {
                    connection.Open();

                    using var command = connection.CreateCommand();
                    command.CommandText = """
                        SELECT EXISTS (
                            SELECT 1
                            FROM pg_constraint
                            WHERE conname = 'UC_DXElementInUnitTypeEnum_Key'
                        );
                        """;

                    var scalar = command.ExecuteScalar();
                    return scalar is bool exists && exists;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
