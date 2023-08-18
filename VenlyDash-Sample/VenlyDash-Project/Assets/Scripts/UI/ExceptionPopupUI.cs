using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ExceptionPopupUI : MonoBehaviour
{
    public static ExceptionPopupUI Instance;
    public Text Text;
    public Button Button;


    void Start()
    {
        Button.onClick.AddListener(Hide);

        Instance = this;
        Hide();
    }

    public static void Show(Exception ex = null)
    {
        if (Instance)
        {
            Instance.Text.text = ex.Message;
            Instance.gameObject.SetActive(true);
        }
    }

    public static void Hide()
    {
        if (Instance)
        {
            Instance.gameObject.SetActive(false);
        }
    }
}
