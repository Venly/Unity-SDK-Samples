using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Assertions;
using Venly;
using Venly.Core;
using Venly.Models.Market;
using Venly.Models.Nft;
using Venly.Models.Shared;
using Venly.Models.Wallet;

#region DTOs
public class ClaimTokenRequest
{
    [JsonProperty("random")] public bool RandomDrop { get; set; } = false;
    [JsonProperty("itemType")] public string ItemType { get; set; }
    [JsonProperty("itemName")] public string ItemName { get; set; }
}

public class ClaimTokenResponse
{
    public ClaimTokenResponse(){}
    public ClaimTokenResponse(VyMultiTokenDto token, int coinBalance)
    {
        Token = token;
        CoinBalance = coinBalance;
    }

    [JsonProperty("token")] public VyMultiTokenDto Token { get; private set; }
    [JsonProperty("coinBalance")] public int CoinBalance { get; private set; }
}
#endregion

public static class Web3Controller
{
    public static bool IsDemo => _backendHandler?.IsDemo ?? false;

    private static bool _isInitialized = false;
    private static BackendHandlerBase _backendHandler;

    #region Init

    public static void SetBackendHandler(bool isDemo = false)
    {
        _backendHandler = null;

        if (isDemo)
        {
            _backendHandler = new BackendHandler_Demo();
            return;
        }

#if ENABLE_VENLY_DEV_MODE || ENABLE_VENLY_DEVMODE
        _backendHandler = new BackendHandler_DevMode();
#elif ENABLE_VENLY_PLAYFAB
        _backendHandler = new BackendHandler_PlayFab();
#elif ENABLE_VENLY_BEAMABLE
        _backendHandler = new BackendHandler_Beamable();
#endif

        Assert.IsNotNull(_backendHandler);
        _backendHandler.Initialize();
    }

    public static VyTask Init(bool loadMockData = false)
    {
        if (_isInitialized)
            return VyTask.Succeeded();

        //Init PlayerDataWeb3
        PlayerDataWeb3.instance.Init();

        //Load MockData (if required)
        if (loadMockData)
        {
            var taskNotifier = VyTask.Create();

            taskNotifier.Scope(async () =>
            {
#if ENABLE_VENLY_PLAYFAB || ENABLE_VENLY_DEVMODE || ENABLE_VENLY_BEAMABLE
                await SignIn(null, null);
#else
                await SignIn_Demo();
#endif

                taskNotifier.NotifySuccess();
            });

            return taskNotifier.Task;
        }

        _isInitialized = true;

        return VyTask.Succeeded();
    }

    #endregion

    #region Authentication

    //Demo uses MockData
    public static VyTask SignIn_Demo()
    {
        SetBackendHandler(true);
        return _backendHandler.SignIn(null, null);
    }

    //Either DevMode or Dedicated Backend (with Identity Service)
    public static VyTask SignIn(string email, string password)
    {
        SetBackendHandler();
        return _backendHandler.SignIn(email, password);
    }

    public static VyTask SignUp(string email, string password, string pincode, string nickname = null)
    {
        if (string.IsNullOrEmpty(email))
            return VyTask.Failed("Email cannot be empty");

        if (string.IsNullOrEmpty(password))
            return VyTask.Failed("Password cannot be empty");

        if (string.IsNullOrEmpty(nickname))
            return VyTask.Failed("Nickname cannot be empty");

        if (string.IsNullOrEmpty(pincode))
            return VyTask.Failed("Pincode cannot be empty");

        SetBackendHandler();
        return _backendHandler.SignUp(email, password, pincode, nickname);
    }

    #endregion

    #region Retrieve Data

    public static VyTask<VyWalletDto> RetrieveWallet(string walletId = null, bool includeBalance = true)
    {
        return _backendHandler.RetrieveWallet(walletId, includeBalance);
    }

    public static VyTask<VyMultiTokenDto[]> RetrieveNFTs()
    {
        return _backendHandler.RetrieveNFTs();
    }

    public static VyTask<int> RetrieveCoinBalance()
    {
        return _backendHandler.RetrieveCoinBalance();
    }

    public static VyTask<int> UpdateCoinBalance(int coinBalance)
    {
        return _backendHandler.UpdateCoinBalance(coinBalance);
    }

    #endregion

    #region Claim Items

    public static VyTask<VyMultiTokenDto> ClaimCharacter(string characterName, int cost)
    {
        Debug.Log($"[WEB3_CONTROLLER] Claim Character (name={characterName} | cost={cost})");
        return ClaimToken(new ClaimTokenRequest
        {
            ItemType = "character",
            ItemName = characterName,
            RandomDrop = false
        }, cost);
    }

    public static VyTask<VyMultiTokenDto> ClaimAccessory(string accessoryName, int cost)
    {
        Debug.Log($"[WEB3_CONTROLLER] Claim Accessory (name={accessoryName} | cost={cost})");
        return ClaimToken(new ClaimTokenRequest
        {
            ItemType = "accessory",
            ItemName = accessoryName,
            RandomDrop = false
        }, cost);
    }

    public static VyTask<VyMultiTokenDto> ClaimRandom()
    {
        Debug.Log("[WEB3_CONTROLLER] Claim Random");
        return ClaimToken(new ClaimTokenRequest
        {
            ItemType = null,
            ItemName = null,
            RandomDrop = true
        });
    }

    private static VyTask<VyMultiTokenDto> ClaimToken(ClaimTokenRequest claimTokenRequest, int cost = -1)
    {
        Debug.Log(
            $"[WEB3_CONTROLLER] Claim Token (type={claimTokenRequest.ItemType} | name={claimTokenRequest.ItemName})");
        var taskNotifier = VyTask<VyMultiTokenDto>.Create();

        _backendHandler.ClaimToken(claimTokenRequest, cost)
            .OnSuccess(claimedToken =>
            {
                //Add token to game + update new coin balance
                if (claimTokenRequest.RandomDrop) //Random = Only add token (ignore balance)
                {
                    PlayerDataWeb3.instance.AddToken(claimedToken.Token);
                }
                else //Not Random = Add Token and Update Balance
                {
                    PlayerDataWeb3.instance.AddToken(claimedToken.Token, claimedToken.CoinBalance);
                }

                //Notify Success
                taskNotifier.NotifySuccess(claimedToken.Token);

            })
            .OnFail(taskNotifier.NotifyFail);

        return taskNotifier.Task;
    }

    public static VyTask ClaimCoins(int amount)
    {
        Debug.Log($"[WEB3_CONTROLLER] Claim Coins (amount={amount})");
        return PlayerDataWeb3.instance.AddCoins(amount);
    }

    public static VyTask<VyMultiTokenDto> ClaimMission(MissionBase m)
    {
        var taskNotifier = VyTask<VyMultiTokenDto>.Create();

        taskNotifier.Scope(async () =>
        {
            //Remove Mission
            PlayerDataWeb3.instance.ClaimMission(m);

            //Generate Random NFT
            var token = await ClaimRandom().AwaitResult();
            ShowClaimedNFT(token);

            taskNotifier.NotifySuccess(token);
        });

        //todo
        return taskNotifier.Task;
    }

    #region Claim Theme/Consumable (not supported)

    public static VyTask<VyMultiTokenDto> ClaimTheme(string themeName, int cost)
    {
        return VyTask<VyMultiTokenDto>.Failed(new NotSupportedException("[WEB3_CONTROLLER] Claim Theme not supported"));

        //Debug.Log($" (TODO) [WEB3_CONTROLLER] Claim Theme (name={themeName} | cost={cost})");

        //PlayerDataWeb3.instance.RemoveCoins(cost);
        //PlayerDataWeb3.instance.AddTheme(themeName);
        //PlayerDataWeb3.instance.SaveLocalData();

        //return VyTask<VyMultiTokenDto>.Failed(new NotImplementedException());
    }

    public static VyTask<VyMultiTokenDto> ClaimConsumable(Consumable.ConsumableType type, int cost)
    {
        return VyTask<VyMultiTokenDto>.Failed(
            new NotSupportedException("[WEB3_CONTROLLER] Claim Consumable not supported"));

        //Debug.Log($" (TODO) [WEB3_CONTROLLER] Claim Consumable (type={type} | cost={cost})");

        //PlayerDataWeb3.instance.RemoveCoins(cost);
        //PlayerDataWeb3.instance.AddConsumable(type);
        //PlayerDataWeb3.instance.SaveLocalData();

        //return VyTask<VyMultiTokenDto>.Failed(new NotImplementedException());
    }

    #endregion

    #endregion

    #region Leaderboard

    public static VyTask<List<HighscoreEntry>> RetrieveLeaderboard(bool aroundPlayer)
    {
        return _backendHandler.RetrieveLeaderboard(aroundPlayer);
    }

    public static VyTask UpdateLeaderboard(int score)
    {
        return _backendHandler.UpdateLeaderboard(score);
    }

    #endregion

    #region Transfer Items

    public static VyTask<VyTransactionResultDto> TransferToken(VyMultiTokenDto token, string destinationAddress,
        string pincode)
    {
        return _backendHandler.TransferToken(token, destinationAddress, pincode);
    }

    #endregion

    #region Popups

    public static void ShowClaimedNFT(VyMultiTokenDto token)
    {
        Debug.Log($" (TODO) [WEB3_CONTROLLER] Show Claimed NFT (name={token.Name})");
        NFTPopupUI.Show(token);
    }

    public static void ShowException(Exception ex)
    {
        ExceptionPopupUI.Show(ex);
        Debug.LogException(ex);
        HideLoader();
    }

    public static void ShowLoader(string msg = null)
    {
        LoadingUI.Show(msg);
    }

    public static void HideLoader()
    {
        LoadingUI.Hide();
    }

    #endregion
}