using System.Diagnostics;
using System.Diagnostics.Metrics;
using LibrisMaleficarum.Domain.Interfaces.Services;

namespace LibrisMaleficarum.Infrastructure.Services;

/// <summary>
/// Default implementation of <see cref="ITelemetryService"/>.
/// Creates and manages counters and activities for application telemetry.
/// </summary>
public class TelemetryService : ITelemetryService
{
    private readonly Counter<int> _worldsCreated;
    private readonly Counter<int> _worldsDeleted;
    private readonly Counter<int> _entitiesCreated;
    private readonly Counter<int> _entitiesDeleted;
    private readonly ActivitySource _activitySource;

    /// <summary>
    /// Initializes a new instance of the <see cref="TelemetryService"/> class.
    /// </summary>
    /// <param name="meter">The meter used to create counters for telemetry.</param>
    /// <param name="activitySource">The activity source used to create activities for distributed tracing.</param>
    public TelemetryService(Meter meter, ActivitySource activitySource)
    {
        _worldsCreated = meter.CreateCounter<int>("worlds.created", description: "Counts the number of worlds created");
        _worldsDeleted = meter.CreateCounter<int>("worlds.deleted", description: "Counts the number of worlds deleted");
        _entitiesCreated = meter.CreateCounter<int>("entities.created", description: "Counts the number of entities created");
        _entitiesDeleted = meter.CreateCounter<int>("entities.deleted", description: "Counts the number of entities deleted");
        _activitySource = activitySource;
    }

    /// <inheritdoc />
    public void RecordWorldCreated(string worldName)
    {
        _worldsCreated.Add(1, new KeyValuePair<string, object?>("world.name", worldName));
    }

    /// <inheritdoc />
    public void RecordWorldDeleted(string worldName)
    {
        _worldsDeleted.Add(1, new KeyValuePair<string, object?>("world.name", worldName));
    }

    /// <inheritdoc />
    public void RecordEntityCreated(string entityType)
    {
        _entitiesCreated.Add(1, new KeyValuePair<string, object?>("entity.type", entityType));
    }

    /// <inheritdoc />
    public void RecordEntityDeleted(string entityType)
    {
        _entitiesDeleted.Add(1, new KeyValuePair<string, object?>("entity.type", entityType));
    }

    /// <inheritdoc />
    public Activity? StartActivity(string operationName, Dictionary<string, object>? tags = null)
    {
        var activity = _activitySource.StartActivity(operationName);
        if (activity is not null && tags is not null)
        {
            foreach (var (key, value) in tags)
            {
                activity.SetTag(key, value);
            }
        }

        return activity;
    }
}
