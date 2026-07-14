using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Interfaces;
using OmniFlow.Infrastructure.Contexts;

namespace OmniFlow.Infrastructure.Services;

public sealed class NearbyPlaceSearchService(ApplicationDbContext context) : INearbyPlaceSearchService
{
    public async Task<IReadOnlyList<NearbyPlaceCandidate>> SearchAsync(
        NearbyPlaceSearchCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        var connection = context.Database.GetDbConnection();
        var shouldCloseConnection = connection.State != ConnectionState.Open;
        if (shouldCloseConnection)
            await connection.OpenAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = BuildSql(criteria.CandidateLimit);
            AddParameter(command, "trip_id", criteria.TripId);
            AddParameter(command, "latitude", criteria.Latitude);
            AddParameter(command, "longitude", criteria.Longitude);
            AddParameter(command, "radius_meters", criteria.RadiusMeters);
            AddParameter(command, "categories", criteria.Categories.Select(category => category.ToString()).ToArray());

            var candidates = new List<NearbyPlaceCandidate>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                candidates.Add(new NearbyPlaceCandidate(reader.GetGuid(0), reader.GetInt32(1)));
            return candidates;
        }
        finally
        {
            if (shouldCloseConnection)
                await connection.CloseAsync();
        }
    }

    private static string BuildSql(int candidateLimit) => $$"""
        SELECT p.id,
               ROUND(ST_Distance(
                   ST_SetSRID(ST_MakePoint(p.longitude, p.latitude), 4326)::geography,
                   ST_SetSRID(ST_MakePoint(@longitude, @latitude), 4326)::geography))::integer AS distance_meters
        FROM places AS p
        WHERE p.is_active = TRUE
          AND p.category NOT IN ('Hotel', 'Transport')
          AND p.category = ANY(@categories)
          AND p.latitude BETWEEN -90 AND 90
          AND p.longitude BETWEEN -180 AND 180
          AND ST_DWithin(
              ST_SetSRID(ST_MakePoint(p.longitude, p.latitude), 4326)::geography,
              ST_SetSRID(ST_MakePoint(@longitude, @latitude), 4326)::geography,
              @radius_meters)
          AND NOT EXISTS (
              SELECT 1
              FROM timeline_entries AS entry
              WHERE entry.trip_id = @trip_id
                AND entry.place_id = p.id
                AND entry.deleted_at IS NULL)
        ORDER BY distance_meters, p.name, p.id
        LIMIT {{Math.Clamp(candidateLimit, 1, 100)}}
        """;

    private static void AddParameter(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
