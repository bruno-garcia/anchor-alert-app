using Microsoft.Extensions.Logging;
using Microsoft.Maui.Maps;

namespace AnchorAlertApp;

public partial class MainPage : ContentPage
{
    private readonly ILogger<MainPage> _logger;

    private CancellationTokenSource? _currentLocationCancelTokenSource;
    private bool _isCheckingLocation;

    public MainPage(ILogger<MainPage> logger)
	{
        _logger = logger;
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        // Triggers the permission request:
        // map.IsShowingUser = true;

        Geolocation.LocationChanged += (_, args) =>
        {
            // var location = await GetCurrentLocation();
            var location = args.Location;

            _logger.LogDebug($"Latitude: {location.Latitude}, Longitude: {location.Longitude}, Accuracy: {location.Accuracy}");

            var mapSpan = MapSpan.FromCenterAndRadius(location, Distance.FromKilometers(0.444));
            map.MoveToRegion(mapSpan);
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
        double latLongDegrees = 360 / Math.Pow(2, zoomLevel);
        if (map.VisibleRegion is not null)
        {
            map.MoveToRegion(new MapSpan(map.VisibleRegion.Center, latLongDegrees, latLongDegrees));
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

