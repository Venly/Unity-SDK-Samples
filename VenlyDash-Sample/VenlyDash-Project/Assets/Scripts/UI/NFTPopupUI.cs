using Assets.Scripts.Web3;
using UnityEngine;
using UnityEngine.UI;
using Venly.Models.Shared;

public class NFTPopupUI : MonoBehaviour
{
    public static NFTPopupUI Instance;
    public Text Text;
    public Image NFTImage;
    public Animator Animator;
    public RectTransform AnimationTarget;

    private bool _isActive;

    private void Awake()
    {
        if(!Animator)
            Animator = GetComponent<Animator>();
    }

    void Start()
    {
        Instance = this;
        Hide();
    }

    public static void SetText(string text)
    {
        if (Instance)
        {
            Instance.Text.text = text;
        }
    }
    public static void SetSprite(Sprite s)
    {
        if (Instance)
        {
            if (Instance.NFTImage.TryGetComponent(out AspectRatioFitter fitter))
            {
                fitter.aspectRatio = s.rect.width / s.rect.height;
            }

            Instance.NFTImage.sprite = s;
        }
    }
    public static void Show()
    {
        if (Instance)
        {
            Instance.AnimationTarget.localScale = Vector3.zero;
            Instance.gameObject.SetActive(true);
            Instance.Animator.SetTrigger("Show");

            Instance._isActive = true;
        }
    }
    public static void Show(VyMultiTokenDto token)
    {
        if (Instance)
        {
            Instance.Text.text = token.Name;

            Web3Resources.GetTokenSprite(token)
                .OnSuccess(s =>
                {
                    SetSprite(s);
                    Show();
                })
                .OnFail(Web3Controller.ShowException);
        }
    }

    public static void Hide()
    {
        if (Instance)
        {
            Instance._isActive = false;
            Instance.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (_isActive)
        {
            if (Animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1.0f)
            {
                Hide();
            }
        }
    }
}