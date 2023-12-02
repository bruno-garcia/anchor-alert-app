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
        map.MapType = MapType.Hybrid;

        Geolocation.LocationChanged += OnGeolocationOnLocationChanged;

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

    private void OnGeolocationOnLocationChanged(object? _, GeolocationLocationChangedEventArgs args)
        => UpdateMap(args.Location);

    private void UpdateMap(Location location)
    {
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
            // TODO: Mocking the anchor drop here relative to where the simulator puts us
            // This can be set by user input when anchor is dropped.
            // _anchorLocation = location;
            _anchorLocation = new Location(42.688329, 17.939085);

            var mapSpan = new MapSpan(_anchorLocation, _latLongDegrees, _latLongDegrees);
            map.MoveToRegion(mapSpan);

            Debug.Assert(map.MapElements.Count == 0);
            map.MapElements.Add(new Circle
            {
                StrokeColor = Colors.Aquamarine,
                FillColor = Color.FromArgb("#80FFFFFF"),
                // StrokeWidth = 8,
                StrokeWidth = 1,
                Radius = _safeAreaRadius,
                Center = _anchorLocation,
            });
        }
        else if (map.MapElements[0] is Circle safeArea)
        {
            var distanceOriginalAnchorLocationToBoat = Location.CalculateDistance(_anchorLocation, location, DistanceUnits.Kilometers) * 1000;

            // TODO: Take precision into account
            // TODO: Take rode/chain length and depth into account
            if (distanceOriginalAnchorLocationToBoat - _safeAreaRadius.Meters > 1)
            {
                safeArea.StrokeColor = Colors.Coral;
                safeArea.StrokeWidth = 4;
                _isInSafeArea = false;
            }
            else
            {
                safeArea.StrokeColor = Colors.Aquamarine;
                safeArea.Center = _anchorLocation;
                safeArea.StrokeWidth = 1;
                _isInSafeArea = true;
            }
        }
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

