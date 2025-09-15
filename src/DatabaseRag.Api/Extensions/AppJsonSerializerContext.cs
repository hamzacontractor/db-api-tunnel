using System.Text.Json.Serialization;
using DatabaseRag.Api.Models.Requests;
using DatabaseRag.Api.Models.Responses;
using DatabaseRag.Api.Models.Schema;

namespace DatabaseRag.Api.Extensions;

/// <summary>
/// JSON serialization context for source generation
/// </summary>
[JsonSerializable(typeof(SqlQueryRequest))]
[JsonSerializable(typeof(CosmosQueryRequest))]
[JsonSerializable(typeof(CosmosSchemaRequest))]
[JsonSerializable(typeof(CosmosTestRequest))]
[JsonSerializable(typeof(SqlQueryResponse))]
[JsonSerializable(typeof(CosmosQueryResponse))]
[JsonSerializable(typeof(DatabaseSchemaResponse))]
[JsonSerializable(typeof(CosmosSchemaResponse))]
[JsonSerializable(typeof(DatabaseSchemaInfo))]
[JsonSerializable(typeof(CosmosDatabaseSchema))]
[JsonSerializable(typeof(CosmosContainerSchema))]
[JsonSerializable(typeof(CosmosPropertySchema))]
[JsonSerializable(typeof(TableSchema))]
[JsonSerializable(typeof(ColumnSchema))]
[JsonSerializable(typeof(ForeignKeySchemaResponse))]
[JsonSerializable(typeof(IndexSchemaResponse))]
[JsonSerializable(typeof(ConstraintSchemaResponse))]
[JsonSerializable(typeof(AnalyticalDetails))]
[JsonSerializable(typeof(CosmosAnalyticalDetails))]
[JsonSerializable(typeof(ColumnMetadata))]
[JsonSerializable(typeof(List<Dictionary<string, object>>))]
[JsonSerializable(typeof(Dictionary<string, object>))]
[JsonSerializable(typeof(HealthResponse))]
public partial class AppJsonSerializerContext : JsonSerializerContext
{
}