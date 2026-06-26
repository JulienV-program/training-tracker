using System.Net;
using MyTracker.Domain.Configurations;
using MyTracker.Infrastructure.External;
using MyTracker.Infrastructure.Tests.TestUtilities;
using Xunit;

namespace MyTracker.Infrastructure.Tests;

public class StravaProviderTests
{
    private static StravaProvider CreateProvider(HttpStatusCode statusCode, string responseContent, out FakeHttpMessageHandler handler)
    {
        handler = new FakeHttpMessageHandler(statusCode, responseContent);
        var httpClient = new HttpClient(handler);
        return new StravaProvider(httpClient, new StravaSettings());
    }

    [Fact]
    public async Task GetActivitiesAsync_MapsStravaListJsonToActivities()
    {
        const string json = """
        [
          {
            "id": 111, "name": "Morning Run", "start_date": "2026-01-01T07:00:00Z", "type": "Run",
            "distance": 5000.0, "moving_time": 1800, "total_elevation_gain": 40.0,
            "average_heartrate": 150.0, "max_heartrate": 175.0,
            "average_watts": null, "max_watts": null,
            "average_cadence": 85.0, "calories": 320.0, "suffer_score": 55.0
          },
          {
            "id": 222, "name": "Evening Ride", "start_date": "2026-01-02T18:00:00Z", "type": "Ride",
            "distance": 20000.0, "moving_time": 3600, "total_elevation_gain": 120.0,
            "average_heartrate": 130.0, "max_heartrate": 160.0,
            "average_watts": 180.0, "max_watts": 400.0,
            "average_cadence": 90.0, "calories": 600.0, "suffer_score": 70.0
          }
        ]
        """;

        var provider = CreateProvider(HttpStatusCode.OK, json, out _);
        var activities = (await provider.GetActivitiesAsync("token")).ToList();

        Assert.Equal(2, activities.Count);
        Assert.Equal("111", activities[0].Id);
        Assert.Equal("Morning Run", activities[0].Name);
        Assert.Equal("Run", activities[0].Type);
        Assert.Equal(5000.0, activities[0].Distance);
        Assert.Equal(150.0, activities[0].AverageHeartRate);
        Assert.Null(activities[0].AverageWatts);
        Assert.Equal("222", activities[1].Id);
        Assert.Equal(180.0, activities[1].AverageWatts);
    }

    [Fact]
    public async Task GetActivityStreamsAsync_ParsesScalarLatLngAndMovingStreams()
    {
        const string json = """
        {
          "time": { "data": [0, 1] },
          "heartrate": { "data": [120, 125] },
          "distance": { "data": [0.0, 3.5] },
          "latlng": { "data": [[45.8, 3.16], [45.801, 3.161]] },
          "moving": { "data": [false, true] },
          "temp": { "data": [18, 18] }
        }
        """;

        var provider = CreateProvider(HttpStatusCode.OK, json, out _);
        var points = (await provider.GetActivityStreamsAsync("123", "token")).ToList();

        Assert.Equal(2, points.Count);

        Assert.Equal(0, points[0].TimeOffset);
        Assert.Equal(120.0, points[0].HeartRate);
        Assert.Equal(45.8, points[0].Lat);
        Assert.Equal(3.16, points[0].Lng);
        Assert.False(points[0].Moving);
        Assert.Equal(18.0, points[0].Temperature);

        Assert.Equal(1, points[1].TimeOffset);
        Assert.Equal(45.801, points[1].Lat);
        Assert.Equal(3.161, points[1].Lng);
        Assert.True(points[1].Moving);
    }

    [Fact]
    public async Task GetActivityStreamsAsync_ReturnsEmptyList_WhenHttpCallFails()
    {
        var provider = CreateProvider(HttpStatusCode.Unauthorized, "{}", out _);
        var points = await provider.GetActivityStreamsAsync("123", "token");

        Assert.Empty(points);
    }

    [Fact]
    public async Task GetActivityDetailAsync_MapsLapsSplitsAndLatLng()
    {
        const string json = """
        {
          "id": 123,
          "average_speed": 3.2,
          "max_speed": 5.1,
          "start_latlng": [45.8, 3.16],
          "end_latlng": [45.81, 3.17],
          "map": { "id": "m1", "summary_polyline": "abc123" },
          "laps": [
            {
              "id": 1, "name": "Lap 1", "lap_index": 1, "elapsed_time": 600, "moving_time": 590,
              "start_date": "2026-01-01T07:00:00Z", "distance": 1000.0, "average_speed": 1.7,
              "max_speed": 2.0, "average_heartrate": 140.0, "max_heartrate": 150.0, "total_elevation_gain": 10.0
            }
          ],
          "splits_metric": [
            {
              "distance": 1000.0, "elapsed_time": 590, "elevation_difference": 5.0, "moving_time": 580,
              "split": 1, "average_speed": 1.7, "average_heartrate": 140.0, "pace_zone": 2
            }
          ]
        }
        """;

        var provider = CreateProvider(HttpStatusCode.OK, json, out _);
        var detail = await provider.GetActivityDetailAsync("123", "token");

        Assert.Equal("123", detail.ActivityId);
        Assert.Equal(3.2, detail.AverageSpeed);
        Assert.Equal(45.8, detail.StartLat);
        Assert.Equal(3.16, detail.StartLng);
        Assert.Equal(45.81, detail.EndLat);
        Assert.Equal(3.17, detail.EndLng);
        Assert.Equal("abc123", detail.MapPolyline);

        Assert.Single(detail.Laps);
        Assert.Equal("Lap 1", detail.Laps[0].Name);
        Assert.Equal(1, detail.Laps[0].LapIndex);

        Assert.Single(detail.Splits);
        Assert.Equal(1, detail.Splits[0].SplitIndex);
        Assert.Equal(2, detail.Splits[0].PaceZone);
    }

    [Fact]
    public async Task GetActivityDetailAsync_HandlesNullLatLngAndMap_ForIndoorActivities()
    {
        const string json = """
        {
          "id": 123,
          "average_speed": 2.0,
          "max_speed": 2.5,
          "start_latlng": null,
          "end_latlng": null,
          "map": null,
          "laps": [],
          "splits_metric": []
        }
        """;

        var provider = CreateProvider(HttpStatusCode.OK, json, out _);
        var detail = await provider.GetActivityDetailAsync("123", "token");

        Assert.Null(detail.StartLat);
        Assert.Null(detail.StartLng);
        Assert.Null(detail.EndLat);
        Assert.Null(detail.EndLng);
        Assert.Null(detail.MapPolyline);
        Assert.Empty(detail.Laps);
        Assert.Empty(detail.Splits);
    }
}
