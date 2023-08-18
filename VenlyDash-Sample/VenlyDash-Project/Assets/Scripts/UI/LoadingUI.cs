using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class LoadingUI : MonoBehaviour
{
    public static LoadingUI Instance;
    public Text Text;

    private bool _isActive = false;
    public static bool IsActive => Instance?._isActive ?? false;

    void Start()
    {
        Instance = this;
        Hide();
    }

    public static void Show(string msg = null)
    {
        if (Instance)
        {
            if(string.IsNullOrWhiteSpace(msg))
                Instance.Text.text = "";
            else
                Instance.Text.text = msg;

            Instance.gameObject.SetActive(true);
            Instance._isActive = true;
        }
    }

    public static void Hide()
    {
        if (Instance)
        {
            Instance.gameObject.SetActive(false);
            Instance._isActive = false;
        }
    }
}
