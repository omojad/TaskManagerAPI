using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace showlab_api.Services
{
    public class DBService
    {
        private readonly ILogger<DBService> _logger;
        private readonly IConfiguration _configuration;

        public DBService(ILogger<DBService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        private string DBConstr => _configuration.GetConnectionString("dbConnection");

        public async Task<string> ExecSprocWithJsonAsync(string sprocName, string json)
        {
            return await ExecSprocAsync(sprocName, new Dictionary<string, object> { { "json", json } });
        }

        public async Task<string> ExecSprocWithIdAsync(string sprocName, string userid)
        {
            return await ExecSprocAsync(sprocName, new Dictionary<string, object> { { "userId", userid } });
        }

        public async Task<string> ExecSprocWithIdAndIntAsync(string sprocName, int? id, string result)
        {
            return await ExecSprocAsync(sprocName, new Dictionary<string, object> { { "id", id },{ "result", result } });
        }

        public async Task<string> ExecSprocAsync(string sprocName, Dictionary<string, object> parms = null)
        {
            try
            {
                using (var conn = new SqlConnection(DBConstr))
                {
                    await conn.OpenAsync();
                    using (var cmd = new SqlCommand(sprocName, conn) { CommandType = System.Data.CommandType.StoredProcedure })
                    {
                        // Set command timeout to zero (wait indefinitely)
                        cmd.CommandTimeout = 0;
                        if (parms != null)
                        {
                            foreach (var p in parms)
                            {
                                var parameterName = $"@{p.Key}";
                                switch (p.Value)
                                {
                                    case string strValue:
                                        if (p.Key.Equals("json", StringComparison.OrdinalIgnoreCase))
                                            // set
                                            cmd.Parameters.Add(parameterName, System.Data.SqlDbType.NVarChar, -1).Value = strValue;
                                        else
                                            cmd.Parameters.Add(parameterName, System.Data.SqlDbType.NVarChar).Value = strValue;
                                        break;
                                    case int intValue:
                                        cmd.Parameters.Add(parameterName, System.Data.SqlDbType.Int).Value = intValue;
                                        break;
                                    // ✅ FIXED: Added long (Int64) support for ConversationId, MessageId, UserId
                                    case long longValue:
                                        cmd.Parameters.Add(parameterName, System.Data.SqlDbType.BigInt).Value = longValue;
                                        break;
                                    case bool boolValue:
                                        cmd.Parameters.Add(parameterName, System.Data.SqlDbType.Bit).Value = boolValue;
                                        break;
                                    case DateTime dateTimeValue:
                                        cmd.Parameters.Add(parameterName, System.Data.SqlDbType.DateTime).Value = dateTimeValue;
                                        break;
                                    case decimal decimalValue:
                                        cmd.Parameters.Add(parameterName, System.Data.SqlDbType.Decimal).Value = decimalValue;
                                        break;
                                    case double doubleValue:
                                        cmd.Parameters.Add(parameterName, System.Data.SqlDbType.Float).Value = doubleValue;
                                        break;
                                    case DBNull _:
                                        cmd.Parameters.AddWithValue(parameterName, DBNull.Value);
                                        break;

                                    default:
                                        var typeName = p.Value?.GetType().Name ?? "null";
                                        _logger?.LogError($"Unsupported parameter type '{typeName}' for parameter '{p.Key}' with value '{p.Value}'");
                                        throw new ArgumentException($"Unsupported parameter type '{typeName}' for {p.Key}. Supported types: string, int, long, bool, DateTime, decimal, double");
                                }
                            }
                        }

                        var rdr = await cmd.ExecuteReaderAsync(System.Data.CommandBehavior.CloseConnection);

                        var sb = new StringBuilder();
                        if (rdr.HasRows)
                        {
                            while (await rdr.ReadAsync())
                            {
                                sb.Append(rdr.GetValue(0)?.ToString());
                               // sb.AppendLine();
                            }
                        }
                        return sb.ToString();
                    }
                }
            }
            catch (SqlException sqlex)
            {
                _logger?.LogError(sqlex, "An error occurred executing stored procedure {SprocName} with parameters {Parameters}", sprocName, GetDict(parms));
                throw;
            }
            catch (System.Exception ex)
            {
                _logger?.LogError(ex, "An unexpected error occurred");
                throw;
            }
        }

     

        private string GetDict(Dictionary<string, object> dict)
        {
            if (dict == null)
                return string.Empty;
            var sb = new StringBuilder();
            foreach (var key in dict.Keys)
                sb.AppendLine(key + "=" + dict[key]);
            return sb.ToString();
        }
    }
}
