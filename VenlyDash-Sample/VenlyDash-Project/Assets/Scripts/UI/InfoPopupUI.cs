using PlayFab.ClientModels;
using UnityEngine;
using UnityEngine.UI;

public class InfoPopupUI : MonoBehaviour
{
    public static InfoPopupUI Instance;
    public Text Text;
    public Button Button;

    private bool _isActive = false;
    public static bool IsActive => Instance?._isActive ?? false;


    void Start()
    {
        Button.onClick.AddListener(Hide);

        Instance = this;
        Hide();
    }

    public static void Show(string message = "")
    {
        if (Instance)
        {
            Instance.Text.text = message;
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
