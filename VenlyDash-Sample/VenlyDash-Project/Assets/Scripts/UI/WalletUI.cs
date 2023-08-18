using System.Globalization;
using Assets.Scripts.Web3;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Venly.Models.Shared;

public class WalletUI : MonoBehaviour
{
    public TransferUI TransferUI;

    [Header("UI")]
    public Text CoinText;
    public Text NativeText;
    public Text NativeSymbol;
    //public Text WalletAddressText;
    public InputField WalletAddressInput;
    public Button TransferButton;
    public Button CopyButton;
    public Button RefreshButton;

    [Header("NFT UI")]
    public Text SelectedNFTText;
    public RectTransform NFTSelector;
    public Text NoNFTsText;
    public Text NFTMintNumberDisplay;
    public Text NFTTypeDisplay;
    public Image NFTIconDisplay;
    public Image NFTMintNumberPanel;
    public Button NextNFTButton;
    public Button PreviousNFTButton;

    private int SelectedNFT = 0;
    private bool _refreshPending = false;
    private bool _refreshUserData = false;

    void Start()
    {
        TransferUI.Close();

        Web3Controller.Init(true);
        CoroutineHandler.StartStaticCoroutine(CharacterDatabase.LoadDatabase());
        CoroutineHandler.StartStaticCoroutine(ThemeDatabase.LoadDatabase());
        UpdateNFT();

        TransferButton.onClick.AddListener(TransferSelectedNFT);
        NextNFTButton.onClick.AddListener(SelectNextNFT);
        PreviousNFTButton.onClick.AddListener(SelectPreviousNFT);
        CopyButton.onClick.AddListener(() =>
        {
            GUIUtility.systemCopyBuffer = PlayerDataWeb3.instance.UserWallet.Address;
            InfoPopupUI.Show("Wallet Address copied to Clipboard!");
        });
        RefreshButton.onClick.AddListener(() =>
        {
            Refresh(true); //Full Wallet Refresh
        });

        WalletAddressInput.readOnly = true;

//#if UNITY_WEBGL
//        CopyButton.gameObject.SetActive(false);
//#else
//        CopyButton.gameObject.SetActive(true);
//#endif
    }

    private void OnDestroy()
    {
        TransferButton.onClick.RemoveListener(TransferSelectedNFT);
        CopyButton.onClick.RemoveAllListeners();
        NextNFTButton.onClick.RemoveListener(SelectNextNFT);
        PreviousNFTButton.onClick.RemoveListener(SelectPreviousNFT);
        RefreshButton.onClick.RemoveAllListeners();
    }

    void Update()
    {
        if (TransferUI.IsActive ||
            InfoPopupUI.IsActive ||
            LoadingUI.IsActive) return;

        //Execute Scheduled Refresh
        if (_refreshPending)
        {
            Refresh(_refreshUserData);
            return;
        }

        //Update Wallet Flow

        bool hasNFTs = PlayerDataWeb3.instance.NFTs != null && PlayerDataWeb3.instance.NFTs.Length > 0;

        CoinText.text = PlayerDataWeb3.instance.User.Coins.ToString();
        NativeText.text = PlayerDataWeb3.instance.UserWallet.Balance.Balance.ToString(CultureInfo.InvariantCulture);
        NativeSymbol.text = PlayerDataWeb3.instance.UserWallet.Balance.Symbol;
        WalletAddressInput.text = PlayerDataWeb3.instance.UserWallet.Address;

        if (hasNFTs)
        {
            SelectedNFTText.text = $"{SelectedNFT + 1}/{PlayerDataWeb3.instance.NFTs.Length}";
        }
        //else
        //{
        //    NFTNameDisplay.text = "";
        //    NFTNameDisplay.text = "No NFT here";
        //    NFTMintNumberDisplay.text = "";
        //}

        if (TransferButton)
            TransferButton.gameObject.SetActive(hasNFTs);

        if (NextNFTButton)
            NextNFTButton.gameObject.SetActive(hasNFTs);

        if (PreviousNFTButton)
            PreviousNFTButton.gameObject.SetActive(hasNFTs);

        if (NFTIconDisplay) 
            NFTIconDisplay.gameObject.SetActive(hasNFTs);
        
        if(SelectedNFTText)
            SelectedNFTText.gameObject.SetActive(hasNFTs);

        if(NFTMintNumberPanel)
            NFTMintNumberPanel.gameObject.SetActive(hasNFTs);

        if(NoNFTsText)
            NoNFTsText.gameObject.SetActive(!hasNFTs);
    }

    public void UpdateNFT()
    {
        VyMultiTokenDto[] nfts = PlayerDataWeb3.instance.NFTs;
        if (nfts == null)
            return;

        NFTSelector.gameObject.SetActive(nfts.Length > 1);

        if (nfts.Length > 0 && nfts.Length > SelectedNFT)
        {
            var nft = nfts[SelectedNFT];

            NFTTypeDisplay.text = nft.GetAttributeValue("itemType", "?Type?").ToUpper();

            if (nft.HasAttribute("mintNumber"))
                NFTMintNumberDisplay.text = $"# {nft.GetAttributeValue<string>("mintNumber")}";
            else if (!string.IsNullOrEmpty(nft.Id))
                NFTMintNumberDisplay.text = $"id {nft.Id}";
            else NFTMintNumberDisplay.text = "processing";

            Web3Resources.GetTokenSprite(nft)
                .OnSuccess(sprite =>
                {
                    if (NFTIconDisplay.TryGetComponent(out AspectRatioFitter fitter))
                    {
                        fitter.aspectRatio = sprite.rect.width / sprite.rect.height;
                    }
                    
                    NFTIconDisplay.sprite = sprite;
                })
                .OnFail(Web3Controller.ShowException);
        }
    }

    public void SelectNextNFT()
    {
        if (PlayerDataWeb3.instance.NFTs == null)
            return;

        SelectedNFT++;

        if (SelectedNFT >= PlayerDataWeb3.instance.NFTs.Length)
        {
            SelectedNFT = 0;
        }
        UpdateNFT();
    }

    public void SelectPreviousNFT()
    {
        if (PlayerDataWeb3.instance.NFTs == null)
            return;

        SelectedNFT--;

        if (SelectedNFT < 0)
        {
            SelectedNFT = PlayerDataWeb3.instance.NFTs.Length - 1;
        }
        UpdateNFT();
    }


    private void TransferSelectedNFT()
    {
        TransferUI.Open(this, PlayerDataWeb3.instance.NFTs[SelectedNFT], NFTIconDisplay.sprite);
    }

    public void CloseScene()
    {
        SceneManager.UnloadSceneAsync("wallet");
        LoadoutState loadoutState = GameManager.instance.topState as LoadoutState;
        if (loadoutState != null)
        {
            loadoutState.Refresh();
        }
    }

    public void Refresh(bool refreshUserData = false)
    {
        _refreshPending = false;
        
        Web3Controller.ShowLoader(refreshUserData? "Refreshing Wallet" : "Updating Wallet");
        PlayerDataWeb3.instance.RefreshWallet(refreshUserData)
            .OnFail(Web3Controller.ShowException)
            .Finally(() =>
            {
                if (SelectedNFT >= PlayerDataWeb3.instance.NFTs.Length)
                    SelectedNFT = PlayerDataWeb3.instance.NFTs.Length - 1;

                UpdateNFT();

                Web3Controller.HideLoader();
            });

    }

    public void ScheduleRefresh(bool refreshUserData)
    {
        _refreshPending = true;
        _refreshUserData = refreshUserData;
    }
}
