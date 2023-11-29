using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;

namespace AnchorAlertApp;

public partial class MainPage : ContentPage
{
    private readonly ILogger<MainPage> _logger;

    private CancellationTokenSource? _currentLocationCancelTokenSource;
    private bool _isCheckingLocation;
    private double _latLongDegrees = 0.005;
    // TODO: Calculated
    private readonly Distance _safeAreaRadius = Distance.FromMeters(100);
    private Location? _anchorLocation = null;
    private bool _isInSafeArea = true;

    public MainPage(ILogger<MainPage> logger)
    {
        _logger = logger;
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        Geolocation.LocationChanged += (_, args) =>
        {
            var location = args.Location;

            _logger.LogDebug($"Latitude: {location.Latitude}, Longitude: {location.Longitude}, Accuracy: {location.Accuracy}");

            // var distance = Distance.FromMeters(0); // zoom
            // var mapSpan = MapSpan.FromCenterAndRadius(location, distance);

            if (map.VisibleRegion is not null)
            {
                Console.WriteLine(map.VisibleRegion.Radius.Meters);
            }

            // TODO: Show the boat instead
            map.IsShowingUser = true;

            if (_anchorLocation is null)
            {
                _isInSafeArea = true;
                _anchorLocation = location;

                var mapSpan = new MapSpan(_anchorLocation, _latLongDegrees, _latLongDegrees);
                map.MoveToRegion(mapSpan);

                Debug.Assert(map.MapElements.Count == 0);
                map.MapElements.Add(new Circle
                {
                    StrokeColor = Colors.Aquamarine,
                    FillColor = Colors.Cyan,
                    StrokeWidth = 8,
                    Radius = _safeAreaRadius,
                    Center = _anchorLocation,
                });
            }
            else if (map.MapElements[0] is Circle safeArea)
            {
                var distanceOriginalAnchorLocationToBoat = Location.CalculateDistance(
                    _anchorLocation,
                    location,
                    DistanceUnits.Kilometers) * 1000;

                // TODO: Take precision into account
                // TODO: Take rode/chain length and depth into account
                if (distanceOriginalAnchorLocationToBoat - _safeAreaRadius.Meters > 1)
                {
                    safeArea.FillColor = Colors.Gold;
                    safeArea.StrokeColor = Colors.Coral;
                    _isInSafeArea = false;
                }
                else
                {
                    safeArea.StrokeColor = Colors.Aquamarine;
                    safeArea.FillColor = Colors.Cyan;
                    _isInSafeArea = true;
                }
                safeArea.Center = _anchorLocation;
            }
        };

        await Geolocation.StartListeningForegroundAsync(
            new GeolocationListeningRequest(
                // Best Accuracy means:
                // Platform Distance (in meters)
                // Android	  0 - 100
                // iOS	      ~0
                // Windows	  <= 10
                GeolocationAccuracy.Best,
                TimeSpan.FromSeconds(10)));

        base.OnAppearing();
    }

    private void OnSliderValueChanged(object sender, ValueChangedEventArgs e)
    {
        double zoomLevel = e.NewValue;
        _latLongDegrees = 360 / Math.Pow(2, zoomLevel);
        if (map.VisibleRegion is not null)
        {
            map.MoveToRegion(new MapSpan(map.VisibleRegion.Center, _latLongDegrees, _latLongDegrees));
        }
    }

    private async void OnButtonClicked(object sender, EventArgs e)
    {
        var button = sender as Button;
        map.MapType = button?.Text switch
        {
            "Street" => MapType.Street,
            "Satellite" => MapType.Satellite,
            "Hybrid" => MapType.Hybrid,
            _ => map.MapType
        };
    }

    private async Task<Location?> GetCurrentLocation()
    {
        try
        {
            _isCheckingLocation = true;

            var request = new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(10));

            _currentLocationCancelTokenSource = new CancellationTokenSource();


            var location = await Geolocation.Default.GetLocationAsync(request, _currentLocationCancelTokenSource.Token);

            if (location is not null)
            {
                _logger.LogDebug($"Latitude: {location.Latitude}, Longitude: {location.Longitude}, Altitude: {location.Altitude}");
            }

            return location;
        }
        // Catch one of the following exceptions:
        //   FeatureNotSupportedException
        //   FeatureNotEnabledException
        //   PermissionException
        catch (Exception ex)
        {
            // Unable to get location
        }
        finally
        {
            _isCheckingLocation = false;
        }

        return null;
    }

    public void CancelRequest()
    {
        if (_isCheckingLocation && _currentLocationCancelTokenSource?.IsCancellationRequested == false)
        {
            _currentLocationCancelTokenSource.Cancel();
        }
    }
}

