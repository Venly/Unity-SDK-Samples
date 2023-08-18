#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public class TakeScreenshotInEditor : Editor
{
    public static string _fileName = "Editor Screenshot ";
    public static int _startNumber = 1;

    [MenuItem("Custom/Take Screenshot of Game View #s")]
    static void TakeScreenshot()
    {
        int number = _startNumber;
        string name = "" + number;
        string path = $"{Directory.GetParent($"{Application.dataPath}").FullName}/Screenshot/";
        string fullpath = $"{path}{_fileName}{name}.png";

        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        while (File.Exists(fullpath))
        {
            number++;
            name = "" + number;
            fullpath = $"{path}{_fileName}{name}.png";
        }

        _startNumber = number + 1;

        ScreenCapture.CaptureScreenshot(fullpath);
        Debug.Log($"Screenshot taken! {_fileName}{name}.png \n<color=cyan>{Path.GetFullPath(fullpath)}</color>");
    }
}
#endif