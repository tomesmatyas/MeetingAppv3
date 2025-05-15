using Microsoft.Maui.Devices;

public static class ApiConfig
{
    public static string BaseAddress =>
        DeviceInfo.Platform == DevicePlatform.Android
            ? "http://10.0.2.2:5000"
            : "http://localhost:5000";
}
