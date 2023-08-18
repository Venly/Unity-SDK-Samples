using System;
using System.Collections.Generic;
using Venly.Core;
using Venly.Models.Shared;
using Venly.Models.Wallet;

internal class BackendHandler_Demo : BackendHandlerBase
{
    public BackendHandler_Demo()
    {
        IsDemo = true;
    }

    //email & pass are ignored for Demo, MockData is used
    public override VyTask SignIn(string email, string password)
    {
        var taskNotifier = VyTask.Create();

        //Use Mock Wallet (with artificial Delay)
        taskNotifier.Scope(async () =>
        {
            await Web3MockData.Delay();

            //Create Demo User
            PlayerDataWeb3.instance.User = new PlayerDataWeb3.UserData();

            //Retrieve Mock Wallet
            var wallet = Web3MockData.CreateMockWallet();

            //Load UserData
            await PlayerDataWeb3.instance.Refresh(wallet);

            //Notify Success
            taskNotifier.NotifySuccess();
        });

        return taskNotifier.Task;
    }

    public override VyTask<ClaimTokenResponse> ClaimToken(ClaimTokenRequest claimRequest, int cost = -1)
    {
        //todo: add (optional) artificial delay
        var coinBalance = PlayerDataWeb3.instance.User.Coins - cost;
        var token = Web3MockData.GetToken(claimRequest);

        return token == null 
            ? VyTask<ClaimTokenResponse>.Failed(new NotSupportedException($"Unable to Retrieve Mock Token (\'{claimRequest.ItemType}\', \'{claimRequest.ItemName}\', \'{claimRequest.RandomDrop}\')")) 
            : VyTask<ClaimTokenResponse>.Succeeded(new ClaimTokenResponse(token, coinBalance));
    }

    public override VyTask<VyMultiTokenDto[]> RetrieveNFTs()
    {
        return VyTask<VyMultiTokenDto[]>.Succeeded(Web3MockData.GetMockMultiTokens());
    }

    public override VyTask<List<HighscoreEntry>> RetrieveLeaderboard(bool aroundPlayer, int maxEntries = 10)
    {
        //todo: update logic
        if (aroundPlayer)
        {
            var local = new List<HighscoreEntry>
            {
                new HighscoreEntry
                {
                    name = "DEMO",
                    position = 0,
                    score = 2541
                }
            };

            return VyTask<List<HighscoreEntry>>.Succeeded(local);
        }

        var global = new List<HighscoreEntry>();
        var globalsize = global.Count;
        for (var i = 0; i < maxEntries; ++i)
        {
            global.Add(new HighscoreEntry
            {
                name = $"demo_{i}",
                position = i + globalsize,
                score = 150 - 10 * i
            });
        }

        return VyTask<List<HighscoreEntry>>.Succeeded(global);
    }

    public override VyTask<VyTransactionResultDto> TransferToken(VyMultiTokenDto token, string destinationAddress, string pincode, bool isMetaTransaction = false)
    {
        return VyTask<VyTransactionResultDto>.Failed(new NotSupportedException("Not Supported in Demo."));
    }

    public override VyTask<VyWalletDto> RetrieveWallet(string walletId = null, bool includeBalance = false)
    {
        return VyTask<VyWalletDto>.Succeeded(Web3MockData.CreateMockWallet());
    }
}