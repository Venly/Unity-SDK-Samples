using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Venly;
using Venly.Core;
using Venly.Models.Shared;
using Venly.Models.Wallet;

internal abstract class BackendHandlerBase
{
    //Constants
    public readonly eVyChain DefaultChain = eVyChain.Matic;
    public readonly string ContractAddress = "0x22c705d1aab680037f7e0565b20342b0307255c6";
    public readonly int ContractId = 38173;
    public readonly string DefaultWalletIdentifier = "VENLYDASH";

    public bool IsDemo { get; protected set; }

    //Init
    public void Initialize()
    {
        //Base Initialize
        if (!IsDemo)
        {
            if (!VenlyAPI.IsInitialized)
                VenlyUnity.Initialize();
        }
    }

    //Default Behaviour [NONE] >> Backend Specific
    public virtual VyTask SignIn(string email, string password)
    {
        return VyTask.Failed(new NotImplementedException("[BackendHandlerBase::SignIn] SignIn not implemented"));
    }
    
    //Default Behaviour [NONE] >> Backend Specific
    public virtual VyTask SignUp(string email, string password, string pincode, string nickname = null)
    {
        return VyTask.Failed(new NotImplementedException("[BackendHandlerBase::SignUp] SignUp not implemented"));
    }

    public virtual VyTask<VyWalletDto> RetrieveWallet(string walletId = null, bool includeBalance = true)
    {
        var taskNotifier = VyTask<VyWalletDto>.Create();

        taskNotifier.Scope(async () =>
        {
            var query = VyQuery_GetWallet.Create().IncludeBalance(includeBalance);
            var wallet = await VenlyAPI.Wallet.GetWallet(walletId, query).AwaitResult();

            taskNotifier.NotifySuccess(wallet);
        });

        return taskNotifier.Task;
    }

    //Default Behaviour [VenlyAPI] >> ProviderExtension::Invoke("claim_token")
    public virtual VyTask<ClaimTokenResponse> ClaimToken(ClaimTokenRequest claimRequest, int cost = -1)
    {
        var taskNotifier = VyTask<ClaimTokenResponse>.Create();

        taskNotifier.Scope(async () =>
        {
            //Send Request to Backend
            var requestData = VyExtensionRequestData.Create("claim_token")
                .AddJsonContent(claimRequest);

            //Request new token
            var claimedToken = await VenlyAPI.ProviderExtensions.Invoke<ClaimTokenResponse>(requestData).AwaitResult();

            //Notify Success
            taskNotifier.NotifySuccess(claimedToken);
        });

        return taskNotifier.Task;
    }

    //Default Behaviour [VenlyAPI] >> Retrieve NFTs from UserWallet
    public virtual VyTask<VyMultiTokenDto[]> RetrieveNFTs()
    {
        var taskNotifier = VyTask<VyMultiTokenDto[]>.Create();

        taskNotifier.Scope(async () =>
        {
            //Retrieve Tokens in UserWallet
            var query = VyQuery_GetMultiTokenBalances.Create()
                .ContractAddresses(new[] {ContractAddress});
            var filteredTokens = await VenlyAPI.Wallet.GetMultiTokenBalances(PlayerDataWeb3.instance.UserWallet.Id, query).AwaitResult();
            //var filteredTokens = allTokens.Where(t =>
            //    t.Contract.Address.Equals(Web3Controller.ContractAddress, StringComparison.OrdinalIgnoreCase)).ToArray();

            //Notify Success
            taskNotifier.NotifySuccess(filteredTokens);
        });

        return taskNotifier.Task;
    }

    //Default Behaviour [MOCK] >> Mock Balance
    public virtual VyTask<int> RetrieveCoinBalance()
    {
        return VyTask<int>.Succeeded(Web3MockData.InitialCoins);
    }

    //Default Behaviour [MOCK] >> Update Mock Balance
    public virtual VyTask<int> UpdateCoinBalance(int coinBalance)
    {
        if (Web3MockData.UseDelay)
        {
            var taskNotifier = VyTask<int>.Create();
            taskNotifier.Scope(async () =>
            {
                await Web3MockData.Delay();
                taskNotifier.NotifySuccess(coinBalance);
            });

            return taskNotifier.Task;
        }

        return VyTask<int>.Succeeded(coinBalance);
    }

    //Default Behaviour [MOCK] >> Retrieves Mock Leaderboard
    public virtual VyTask<List<HighscoreEntry>> RetrieveLeaderboard(bool aroundPlayer, int maxEntries = 10)
    {
        return VyTask<List<HighscoreEntry>>.Succeeded(PlayerDataWeb3.instance.highscores);
    }

    //Default Behaviour [MOCK] >> Update Mock Leaderboard
    public virtual VyTask UpdateLeaderboard(int score)
    {
        PlayerDataWeb3.instance.InsertScore(score, "NO_NAME");
        return VyTask.Succeeded();
    }

    //Default Behaviour [VenlyAPI] >> Transfer Token to Destination Addr
    public virtual VyTask<VyTransactionResultDto> TransferToken(VyMultiTokenDto token, string destinationAddress,
        string pincode, bool isMetaTransaction = false)
    {
        var transferParams = new VyTransactionMultiTokenTransferRequest()
        {
            Chain = DefaultChain,
            WalletId = PlayerDataWeb3.instance.UserWallet.Id,
            ToAddress = destinationAddress,
            TokenId = int.Parse(token.Id),
            TokenAddress = token.Contract.Address
        };

        return VenlyAPI.Wallet.ExecuteMultiTokenTransfer(pincode, transferParams);
    }
}