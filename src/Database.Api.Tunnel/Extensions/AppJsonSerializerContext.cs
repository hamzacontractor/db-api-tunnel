using System.Text.Json.Serialization;
using Database.Api.Tunnel.Models.Requests;
using Database.Api.Tunnel.Models.Responses;
using Database.Api.Tunnel.Models.Schema;

namespace Database.Api.Tunnel.Extensions;

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
[JsonSerializable(typeof(SchemaSummary))]
[JsonSerializable(typeof(DatabaseSchemaInfo))]
[JsonSerializable(typeof(CosmosDatabaseSchema))]
[JsonSerializable(typeof(CosmosContainerSchema))]
[JsonSerializable(typeof(CosmosPropertySchema))]
[JsonSerializable(typeof(List<CosmosContainerSchema>))]
[JsonSerializable(typeof(List<CosmosPropertySchema>))]
[JsonSerializable(typeof(TableSchema))]
[JsonSerializable(typeof(ColumnSchema))]
[JsonSerializable(typeof(ForeignKeySchemaResponse))]
[JsonSerializable(typeof(IndexSchemaResponse))]
[JsonSerializable(typeof(ConstraintSchemaResponse))]
[JsonSerializable(typeof(AnalyticalDetails))]
[JsonSerializable(typeof(CosmosAnalyticalDetails))]
[JsonSerializable(typeof(ColumnMetadata))]
[JsonSerializable(typeof(GenericQueryResponse))]
[JsonSerializable(typeof(QueryData))]
[JsonSerializable(typeof(Column))]
[JsonSerializable(typeof(Meta))]
[JsonSerializable(typeof(List<Dictionary<string, object>>))]
[JsonSerializable(typeof(Dictionary<string, object>))]
[JsonSerializable(typeof(HealthResponse))]
[JsonSerializable(typeof(DateTime))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(string))]
public partial class AppJsonSerializerContext : JsonSerializerContext
{
}