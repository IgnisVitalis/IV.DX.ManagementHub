using System.Data.Common;

namespace IV.ManagementHub.ApiService.Bootstrap
{
    internal static class BootstrapDatabaseIdentity
    {
        public static bool AreEquivalent(
            string? leftDatabaseType,
            string? leftConnectionString,
            string? rightDatabaseType,
            string? rightConnectionString)
        {
            var left = BuildKey(leftDatabaseType, leftConnectionString);
            var right = BuildKey(rightDatabaseType, rightConnectionString);

            if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
            {
                return false;
            }

            return string.Equals(left, right, StringComparison.Ordinal);
        }

        public static string BuildKey(string? databaseType, string? connectionString)
        {
            if (string.IsNullOrWhiteSpace(databaseType) || string.IsNullOrWhiteSpace(connectionString))
            {
                return string.Empty;
            }

            var normalizedType = databaseType.Trim().ToLowerInvariant();
            var normalizedConnection = connectionString.Trim();

            if (!TryParseConnectionString(normalizedConnection, out var values))
            {
                return $"{normalizedType}::{normalizedConnection}";
            }

            return normalizedType switch
            {
                "postgres" or "postgresql" or "npgsql" => BuildProviderKey(
                    normalizedType,
                    values,
                    defaultPort: "5432",
                    hostKeys: ["host", "server", "data source", "address", "addr", "network address"],
                    databaseKeys: ["database", "initial catalog"]),
                "mysql" => BuildProviderKey(
                    normalizedType,
                    values,
                    defaultPort: "3306",
                    hostKeys: ["server", "host", "data source", "address", "addr", "network address"],
                    databaseKeys: ["database", "initial catalog"]),
                _ => $"{normalizedType}::{normalizedConnection.ToLowerInvariant()}"
            };
        }

        private static string BuildProviderKey(
            string normalizedType,
            IReadOnlyDictionary<string, string> values,
            string defaultPort,
            string[] hostKeys,
            string[] databaseKeys)
        {
            var host = GetValue(values, hostKeys).ToLowerInvariant();
            var port = GetValue(values, ["port"]);
            if (string.IsNullOrWhiteSpace(port))
            {
                port = defaultPort;
            }

            var database = GetValue(values, databaseKeys).ToLowerInvariant();

            return $"{normalizedType}::{host}:{port}/{database}";
        }

        private static string GetValue(IReadOnlyDictionary<string, string> values, string[] keys)
        {
            foreach (var key in keys)
            {
                if (values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
                {
                    return value.Trim();
                }
            }

            return string.Empty;
        }

        private static bool TryParseConnectionString(string connectionString, out IReadOnlyDictionary<string, string> values)
        {
            try
            {
                var builder = new DbConnectionStringBuilder
                {
                    ConnectionString = connectionString
                };

                var dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (string key in builder.Keys)
                {
                    dictionary[key.Trim()] = builder[key]?.ToString()?.Trim() ?? string.Empty;
                }

                values = dictionary;
                return true;
            }
            catch
            {
                values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                return false;
            }
        }
    }
}
