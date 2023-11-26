using CoreLocation;
using UIKit;

namespace AnchorAlertApp;

public class Program
{
	// This is the main entry point of the application.
    private static void Main(string[] args)
	{
		var lm = new CLLocationManager();
		// Require to continue to receive notifications for location without the need to request "Always" access/
		lm.ShowsBackgroundLocationIndicator = true;

		// if you want to use a different Application Delegate class from "AppDelegate"
		// you can specify it here.
		UIApplication.Main(args, null, typeof(AppDelegate));
	}
}
