using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;

namespace AnchorAlertApp;

public partial class MainPage : ContentPage
{
    private readonly ILogger<MainPage> _logger;

    private CancellationTokenSource? _currentLocationCancelTokenSource;
    private bool _isCheckingLocation;
    private double _latLongDegrees;
    private readonly Distance _safeAreaRadius = Distance.FromMeters(250);
    private Location? _anchorLocation = null;

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

            if (map.VisibleRegion is not null && _anchorLocation is null)
            {
                _anchorLocation = location;
                Console.WriteLine(map.VisibleRegion.Radius.Meters);
                var mapSpan = new MapSpan(location, _latLongDegrees, _latLongDegrees);
                map.MoveToRegion(mapSpan);
            }

            map.IsShowingUser = true;
            if (map.MapElements.Count == 0)
            {
                map.MapElements.Add(new Circle
                {
                    StrokeColor = Color.FromArgb("#88FFF900"),
                    StrokeWidth = 8,
                    FillColor = Color.FromArgb("#88EDFFAC"),
                    Radius = _safeAreaRadius,
                    Center = _anchorLocation,
                });
            }
            else if (map.MapElements[0] is Circle safeArea)
            {
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

