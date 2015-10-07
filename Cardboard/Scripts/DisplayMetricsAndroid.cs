using UnityEngine;

/// <summary>
/// http://answers.unity3d.com/questions/161281/is-there-a-way-to-android-physical-screen-size.html
/// </summary>
public class DisplayMetricsAndroid
{
    // The logical density of the display
    public static float Density
    {
        get;
        protected set;
    }

    // The screen density expressed as dots-per-inch
    public static int DensityDPI
    {
        get;
        protected set;
    }

    // The absolute height of the display in pixels
    public static int HeightPixels
    {
        get;
        protected set;
    }

    // The absolute width of the display in pixels
    public static int WidthPixels
    {
        get;
        protected set;
    }

    // A scaling factor for fonts displayed on the display
    public static float ScaledDensity
    {
        get;
        protected set;
    }

    // The exact physical pixels per inch of the screen in the X dimension
    public static float XDPI
    {
        get;
        protected set;
    }

    // The exact physical pixels per inch of the screen in the Y dimension
    public static float YDPI
    {
        get;
        protected set;
    }

    static DisplayMetricsAndroid()
    {
#if UNITY_ANDROID

        // The following is equivalent to this Java code:
        //
        // metricsInstance = new DisplayMetrics();
        // UnityPlayer.currentActivity.getWindowManager().getDefaultDisplay().getMetrics(metricsInstance);
        //
        // ... which is pretty much equivalent to the code on this page:
        // http://developer.android.com/reference/android/util/DisplayMetrics.html

        using (
            AndroidJavaClass unityPlayerClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer"),
            metricsClass = new AndroidJavaClass("android.util.DisplayMetrics")
        )
        {
            using (
                AndroidJavaObject metricsInstance = new AndroidJavaObject("android.util.DisplayMetrics"),
                activityInstance = unityPlayerClass.GetStatic<AndroidJavaObject>("currentActivity"),
                windowManagerInstance = activityInstance.Call<AndroidJavaObject>("getWindowManager"),
                displayInstance = windowManagerInstance.Call<AndroidJavaObject>("getDefaultDisplay")
            )
            {
                displayInstance.Call("getMetrics", metricsInstance);
                Density = metricsInstance.Get<float>("density");
                DensityDPI = metricsInstance.Get<int>("densityDpi");
                HeightPixels = metricsInstance.Get<int>("heightPixels");
                WidthPixels = metricsInstance.Get<int>("widthPixels");
                ScaledDensity = metricsInstance.Get<float>("scaledDensity");
                XDPI = metricsInstance.Get<float>("xdpi");
                YDPI = metricsInstance.Get<float>("ydpi");
            }
        }
#endif
    }
}