using System.Data;
using System.Diagnostics;
using Database.Api.Tunnel.Models.Requests;
using Database.Api.Tunnel.Models.Responses;
using Database.Api.Tunnel.Models.Schema;
using Database.Api.Tunnel.Services.Abstractions;
using Microsoft.Data.SqlClient;

namespace Database.Api.Tunnel.Services;

/// <summary>
/// Service implementation for SQL Server operations
/// </summary>
public class SqlService : ISqlService
{
    /// <summary>
    /// Retrieves the schema information for a SQL Server database
    /// </summary>
    /// <param name="connectionString">The SQL Server connection string</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Database schema response</returns>
    public async Task<DatabaseSchemaResponse> GetSchemaAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        try
        {
            var tables = new List<TableSchema>();
            var foreignKeys = new List<ForeignKeySchemaResponse>();
            string databaseName = "";
            string serverVersion = "";

            // Step 1: Get database info
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync(cancellationToken);

                // Get database name and version
                using var dbInfoCommand = new SqlCommand("SELECT DB_NAME() as DatabaseName, @@VERSION as ServerVersion", connection);
                using var dbInfoReader = await dbInfoCommand.ExecuteReaderAsync(cancellationToken);

                if (await dbInfoReader.ReadAsync(cancellationToken))
                {
                    databaseName = dbInfoReader["DatabaseName"]?.ToString() ?? "";
                    serverVersion = dbInfoReader["ServerVersion"]?.ToString() ?? "";
                }
            }

            // Step 2: Get all tables with their columns
            var tableDict = new Dictionary<string, TableSchema>();
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync(cancellationToken);

                // Get all tables with their columns and additional metadata
                var tablesQuery = @"
                    SELECT 
                        t.TABLE_SCHEMA,
                        t.TABLE_NAME,
                        c.COLUMN_NAME,
                        c.DATA_TYPE,
                        c.IS_NULLABLE,
                        c.CHARACTER_MAXIMUM_LENGTH,
                        c.NUMERIC_PRECISION,
                        c.NUMERIC_SCALE,
                        c.COLUMN_DEFAULT,
                        c.ORDINAL_POSITION,
                        CASE WHEN pk.COLUMN_NAME IS NOT NULL THEN 1 ELSE 0 END as IS_PRIMARY_KEY,
                        CASE WHEN ic.is_identity = 1 THEN 1 ELSE 0 END as IS_IDENTITY,
                        CASE WHEN cc.is_computed = 1 THEN 1 ELSE 0 END as IS_COMPUTED
                    FROM INFORMATION_SCHEMA.TABLES t
                    INNER JOIN INFORMATION_SCHEMA.COLUMNS c ON t.TABLE_NAME = c.TABLE_NAME AND t.TABLE_SCHEMA = c.TABLE_SCHEMA
                    LEFT JOIN (
                        SELECT ku.TABLE_SCHEMA, ku.TABLE_NAME, ku.COLUMN_NAME
                        FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
                        INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE ku ON tc.CONSTRAINT_NAME = ku.CONSTRAINT_NAME
                        WHERE tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
                    ) pk ON c.TABLE_SCHEMA = pk.TABLE_SCHEMA AND c.TABLE_NAME = pk.TABLE_NAME AND c.COLUMN_NAME = pk.COLUMN_NAME
                    LEFT JOIN sys.tables st ON st.name = c.TABLE_NAME
                    LEFT JOIN sys.schemas ss ON ss.schema_id = st.schema_id AND ss.name = c.TABLE_SCHEMA
                    LEFT JOIN sys.columns sc ON sc.object_id = st.object_id AND sc.name = c.COLUMN_NAME
                    LEFT JOIN sys.identity_columns ic ON ic.object_id = sc.object_id AND ic.column_id = sc.column_id
                    LEFT JOIN sys.computed_columns cc ON cc.object_id = sc.object_id AND cc.column_id = sc.column_id
                    WHERE t.TABLE_TYPE = 'BASE TABLE'
                    ORDER BY t.TABLE_SCHEMA, t.TABLE_NAME, c.ORDINAL_POSITION";

                using var command = new SqlCommand(tablesQuery, connection);
                using var reader = await command.ExecuteReaderAsync(cancellationToken);

                while (await reader.ReadAsync(cancellationToken))
                {
                    var tableKey = $"{reader["TABLE_SCHEMA"]}.{reader["TABLE_NAME"]}";

                    if (!tableDict.TryGetValue(tableKey, out var table))
                    {
                        table = new TableSchema
                        {
                            Schema = reader["TABLE_SCHEMA"].ToString() ?? string.Empty,
                            Name = reader["TABLE_NAME"].ToString() ?? string.Empty,
                            Columns = new List<ColumnSchema>(),
                            Indexes = new List<IndexSchemaResponse>(),
                            Constraints = new List<ConstraintSchemaResponse>()
                        };
                        tableDict[tableKey] = table;
                    }

                    var column = new ColumnSchema
                    {
                        Name = reader["COLUMN_NAME"].ToString() ?? string.Empty,
                        DataType = reader["DATA_TYPE"].ToString() ?? string.Empty,
                        IsNullable = reader["IS_NULLABLE"].ToString() == "YES",
                        MaxLength = reader["CHARACTER_MAXIMUM_LENGTH"] as int?,
                        NumericPrecision = reader["NUMERIC_PRECISION"] as byte?,
                        NumericScale = reader["NUMERIC_SCALE"] as int?,
                        DefaultValue = reader["COLUMN_DEFAULT"]?.ToString(),
                        OrdinalPosition = Convert.ToInt32(reader["ORDINAL_POSITION"]),
                        IsPrimaryKey = Convert.ToBoolean(reader["IS_PRIMARY_KEY"]),
                        IsIdentity = Convert.ToBoolean(reader["IS_IDENTITY"]),
                        IsComputed = Convert.ToBoolean(reader["IS_COMPUTED"])
                    };

                    ((List<ColumnSchema>)table.Columns).Add(column);
                }
            }

            // Step 3: Get foreign keys
            await GetForeignKeysAsync(connectionString, foreignKeys, cancellationToken);

            // Step 4: Get indexes for each table
            await GetIndexesAsync(connectionString, tableDict, cancellationToken);

            // Step 5: Get constraints for each table
            await GetConstraintsAsync(connectionString, tableDict, cancellationToken);

            tables.AddRange(tableDict.Values);

            var schema = new DatabaseSchemaResponse
            {
                Success = true,
                Schema = new DatabaseSchemaInfo
                {
                    Tables = tables,
                    ForeignKeys = foreignKeys,
                    DatabaseName = databaseName,
                    ServerVersion = serverVersion,
                    RetrievedAtUtc = DateTime.UtcNow
                }
            };

            return schema;
        }
        catch (Exception ex)
        {
            return new DatabaseSchemaResponse
            {
                Success = false,
                Error = ex.Message,
                Schema = null
            };
        }
    }

    /// <summary>
    /// Tests the connection to a SQL Server database
    /// </summary>
    /// <param name="connectionString">The SQL Server connection string</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Health response indicating connection status</returns>
    public async Task<HealthResponse> TestConnectionAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            return new HealthResponse("healthy", DateTime.UtcNow, "SQL Server Connection");
        }
        catch (Exception ex)
        {
            return new HealthResponse($"unhealthy: {ex.Message}", DateTime.UtcNow, "SQL Server Connection");
        }
    }

    /// <summary>
    /// Executes a SQL query against a SQL Server database
    /// </summary>
    /// <param name="request">The SQL query request</param>
    /// <param name="connectionString">The SQL Server connection string</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>SQL query response with results</returns>
    public async Task<SqlQueryResponse> ExecuteQueryAsync(SqlQueryRequest request, string connectionString, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            if (string.IsNullOrWhiteSpace(request.Query))
            {
                throw new ArgumentException("Query cannot be empty");
            }

            Console.WriteLine($"[DEBUG] SQL Query: {request.Query}");
            // [SECURITY] Connection string logging removed to avoid exposure of sensitive information.

            var results = new List<Dictionary<string, object>>();
            var columnInfo = new List<ColumnMetadata>();
            int rowCount = 0;

            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            Console.WriteLine($"[DEBUG] SQL Connection opened successfully");

            using var command = new SqlCommand(request.Query, connection);
            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            Console.WriteLine($"[DEBUG] SQL Query executed, reading results...");

            // Get column metadata
            var schemaTable = reader.GetSchemaTable();
            if (schemaTable != null)
            {
                foreach (DataRow row in schemaTable.Rows)
                {
                    columnInfo.Add(new ColumnMetadata
                    {
                        ColumnName = row["ColumnName"]?.ToString() ?? string.Empty,
                        DataType = row["DataType"]?.ToString() ?? string.Empty,
                        ColumnSize = row["ColumnSize"] as int? ?? 0,
                        AllowDBNull = row["AllowDBNull"] as bool? ?? true
                    });
                }
            }
            Console.WriteLine($"[DEBUG] Found {columnInfo.Count} columns");

            // Read data
            while (await reader.ReadAsync(cancellationToken))
            {
                var row = new Dictionary<string, object>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    row[reader.GetName(i)] = value ?? DBNull.Value;
                }
                results.Add(row);
                rowCount++;
            }

            Console.WriteLine($"[DEBUG] SQL Query completed: {rowCount} rows returned");
            if (results.Count > 0)
            {
                Console.WriteLine($"[DEBUG] Sample row keys: {string.Join(", ", results.First().Keys)}");
            }

            stopwatch.Stop();

            var response = new SqlQueryResponse
            {
                Success = true,
                Data = results,
                AnalyticalDetails = new AnalyticalDetails
                {
                    RowCount = rowCount,
                    ColumnCount = columnInfo.Count,
                    ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
                    Columns = columnInfo,
                    Query = request.Query,
                    Timestamp = DateTime.UtcNow
                }
            };

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            return new SqlQueryResponse
            {
                Success = false,
                Error = ex.Message,
                AnalyticalDetails = new AnalyticalDetails
                {
                    RowCount = 0,
                    ColumnCount = 0,
                    ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
                    Query = request.Query,
                    Timestamp = DateTime.UtcNow,
                    Columns = new List<ColumnMetadata>()
                }
            };
        }
    }

    private async Task GetForeignKeysAsync(string connectionString, List<ForeignKeySchemaResponse> foreignKeys, CancellationToken cancellationToken)
    {
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var foreignKeysQuery = @"
            SELECT 
                fk.name AS FK_NAME,
                sch1.name AS TABLE_SCHEMA,
                tab1.name AS TABLE_NAME,
                col1.name AS COLUMN_NAME,
                sch2.name AS REFERENCED_TABLE_SCHEMA,
                tab2.name AS REFERENCED_TABLE_NAME,
                col2.name AS REFERENCED_COLUMN_NAME,
                fk.delete_referential_action_desc AS DELETE_RULE,
                fk.update_referential_action_desc AS UPDATE_RULE
            FROM sys.foreign_keys fk
            INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
            INNER JOIN sys.tables tab1 ON tab1.object_id = fkc.parent_object_id
            INNER JOIN sys.schemas sch1 ON tab1.schema_id = sch1.schema_id
            INNER JOIN sys.columns col1 ON col1.column_id = fkc.parent_column_id AND col1.object_id = tab1.object_id
            INNER JOIN sys.tables tab2 ON tab2.object_id = fkc.referenced_object_id
            INNER JOIN sys.schemas sch2 ON tab2.schema_id = sch2.schema_id
            INNER JOIN sys.columns col2 ON col2.column_id = fkc.referenced_column_id AND col2.object_id = tab2.object_id
            ORDER BY fk.name";

        using var fkCommand = new SqlCommand(foreignKeysQuery, connection);
        using var fkReader = await fkCommand.ExecuteReaderAsync(cancellationToken);

        while (await fkReader.ReadAsync(cancellationToken))
        {
            foreignKeys.Add(new ForeignKeySchemaResponse
            {
                Name = fkReader["FK_NAME"].ToString() ?? string.Empty,
                TableSchema = fkReader["TABLE_SCHEMA"].ToString() ?? string.Empty,
                TableName = fkReader["TABLE_NAME"].ToString() ?? string.Empty,
                ColumnName = fkReader["COLUMN_NAME"].ToString() ?? string.Empty,
                ReferencedTableSchema = fkReader["REFERENCED_TABLE_SCHEMA"].ToString() ?? string.Empty,
                ReferencedTableName = fkReader["REFERENCED_TABLE_NAME"].ToString() ?? string.Empty,
                ReferencedColumnName = fkReader["REFERENCED_COLUMN_NAME"].ToString() ?? string.Empty,
                DeleteRule = fkReader["DELETE_RULE"].ToString() ?? string.Empty,
                UpdateRule = fkReader["UPDATE_RULE"].ToString() ?? string.Empty
            });
        }
    }

    private async Task GetIndexesAsync(string connectionString, Dictionary<string, TableSchema> tableDict, CancellationToken cancellationToken)
    {
        foreach (var table in tableDict.Values)
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            var indexQuery = @"
                SELECT 
                    i.name AS INDEX_NAME,
                    i.is_unique AS IS_UNIQUE,
                    i.is_primary_key AS IS_PRIMARY_KEY,
                    i.type_desc AS INDEX_TYPE,
                    i.filter_definition AS FILTER_DEFINITION,
                    STRING_AGG(c.name, ', ') WITHIN GROUP (ORDER BY ic.key_ordinal) AS COLUMN_NAMES
                FROM sys.indexes i
                INNER JOIN sys.tables t ON i.object_id = t.object_id
                INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
                INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
                INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
                WHERE s.name = @schema AND t.name = @tableName
                GROUP BY i.name, i.is_unique, i.is_primary_key, i.type_desc, i.filter_definition
                ORDER BY i.name";

            using var indexCommand = new SqlCommand(indexQuery, connection);
            indexCommand.Parameters.AddWithValue("@schema", table.Schema);
            indexCommand.Parameters.AddWithValue("@tableName", table.Name);
            using var indexReader = await indexCommand.ExecuteReaderAsync(cancellationToken);

            while (await indexReader.ReadAsync(cancellationToken))
            {
                var columnNames = indexReader["COLUMN_NAMES"]?.ToString()?.Split(", ") ?? Array.Empty<string>();
                ((List<IndexSchemaResponse>)table.Indexes).Add(new IndexSchemaResponse
                {
                    Name = indexReader["INDEX_NAME"].ToString() ?? string.Empty,
                    Columns = columnNames.ToList(),
                    IsUnique = Convert.ToBoolean(indexReader["IS_UNIQUE"]),
                    IsPrimaryKey = Convert.ToBoolean(indexReader["IS_PRIMARY_KEY"]),
                    IsClustered = indexReader["INDEX_TYPE"]?.ToString() == "CLUSTERED",
                    Type = indexReader["INDEX_TYPE"]?.ToString() ?? string.Empty,
                    FilterDefinition = indexReader["FILTER_DEFINITION"]?.ToString()
                });
            }
        }
    }

    private async Task GetConstraintsAsync(string connectionString, Dictionary<string, TableSchema> tableDict, CancellationToken cancellationToken)
    {
        foreach (var table in tableDict.Values)
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            var constraintQuery = @"
                SELECT 
                    tc.CONSTRAINT_NAME,
                    tc.CONSTRAINT_TYPE,
                    STRING_AGG(kcu.COLUMN_NAME, ', ') AS COLUMN_NAMES,
                    cc.CHECK_CLAUSE
                FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
                LEFT JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu ON tc.CONSTRAINT_NAME = kcu.CONSTRAINT_NAME
                LEFT JOIN INFORMATION_SCHEMA.CHECK_CONSTRAINTS cc ON tc.CONSTRAINT_NAME = cc.CONSTRAINT_NAME
                WHERE tc.TABLE_SCHEMA = @schema AND tc.TABLE_NAME = @tableName
                GROUP BY tc.CONSTRAINT_NAME, tc.CONSTRAINT_TYPE, cc.CHECK_CLAUSE
                ORDER BY tc.CONSTRAINT_TYPE, tc.CONSTRAINT_NAME";

            using var constraintCommand = new SqlCommand(constraintQuery, connection);
            constraintCommand.Parameters.AddWithValue("@schema", table.Schema);
            constraintCommand.Parameters.AddWithValue("@tableName", table.Name);
            using var constraintReader = await constraintCommand.ExecuteReaderAsync(cancellationToken);

            while (await constraintReader.ReadAsync(cancellationToken))
            {
                var columnNames = constraintReader["COLUMN_NAMES"]?.ToString()?.Split(", ") ?? Array.Empty<string>();
                ((List<ConstraintSchemaResponse>)table.Constraints).Add(new ConstraintSchemaResponse
                {
                    Name = constraintReader["CONSTRAINT_NAME"].ToString() ?? string.Empty,
                    Type = constraintReader["CONSTRAINT_TYPE"].ToString() ?? string.Empty,
                    Columns = columnNames.Where(c => !string.IsNullOrEmpty(c)).ToList(),
                    Definition = constraintReader["CHECK_CLAUSE"]?.ToString(),
                    IsEnabled = true // INFORMATION_SCHEMA doesn't provide this, assume enabled
                });
            }
        }
    }
}