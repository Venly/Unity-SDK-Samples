using System;
using UnityEngine;
using UnityEngine.UI;
using Venly.Models.Shared;

public class TransferUI : MonoBehaviour
{
    private VyMultiTokenDto _nft;
    private WalletUI _walletUI;

    public Text NFTNameDisplay;
    public Image NFTIconDisplay;

    public InputField AdInputField;
    public InputField PinInputField;

    public Button TransferButton;
    public Button PasteButton;

    public bool IsActive { get; private set; }

    public void Open(WalletUI walletUI, VyMultiTokenDto nftToTransfer, Sprite nftSprite)
    {
        IsActive = true;


        if (PasteButton)
        {
#if UNITY_WEBGL
            PasteButton.gameObject.SetActive(true);
#else
            PasteButton.gameObject.SetActive(false);
#endif
        }


        _nft = nftToTransfer;
        _walletUI = walletUI;
        gameObject.SetActive(true);
        DisplayNFT(nftSprite);

        TransferButton.onClick.AddListener(() =>
        {
            Web3Controller.ShowLoader("Transferring...");
            Web3Controller.TransferToken(_nft, AdInputField.text, PinInputField.text)
                .OnSuccess((transferInfo) =>
                {
                    PlayerDataWeb3.instance.RemoveToken(nftToTransfer);
                    _walletUI.ScheduleRefresh(false);
                    Close();

                    InfoPopupUI.Show("Transfer Successful!");
                })
                .OnFail(ex =>
                {
                    if (ex.Message.Contains("transaction.insufficient-funds"))
                    {
                        Web3Controller.ShowException(new Exception($"Insufficient Funds ({PlayerDataWeb3.instance.UserWallet.Balance.Symbol}) to Execute the Transaction."));
                    }
                    else Web3Controller.ShowException(ex);
                })
                .Finally(Web3Controller.HideLoader);
        });
    }

    public void Close()
    {
        IsActive = false;
        PinInputField.text = string.Empty;

        TransferButton.onClick.RemoveAllListeners();
        gameObject.SetActive(false);
    }

    public void CopyFromClipboard()
    {
        if (!string.IsNullOrEmpty(GUIUtility.systemCopyBuffer))
        {
            AdInputField.text = GUIUtility.systemCopyBuffer;
        }
    }

    private void DisplayNFT(Sprite sprite)
    {
        //NFTNameDisplay.text = _nft.Name;

        if (NFTIconDisplay.TryGetComponent(out AspectRatioFitter fitter))
        {
            fitter.aspectRatio = sprite.rect.width / sprite.rect.height; ;
        }

        NFTIconDisplay.sprite = sprite;
    }

    void Start()
    {
        
    }

    void Update()
    {
        
    }
}
