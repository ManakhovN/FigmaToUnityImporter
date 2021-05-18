using System;
using UnityEditor;
using UnityEngine;

public static class Screenshoter
{
    [MenuItem("Screenshoter/Take Screenshot")]
    public static void TakeScreenshot()
    {
        
        ScreenCapture.CaptureScreenshot(DateTime.Now.Ticks + ".jpg");
    }
}
