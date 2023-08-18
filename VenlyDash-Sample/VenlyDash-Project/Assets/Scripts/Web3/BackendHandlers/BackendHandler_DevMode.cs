using System;
using System.Collections.Generic;
using System.Linq;
using Venly;
using Venly.Core;
using Venly.Models.Nft;
using Venly.Models.Shared;
using Venly.Models.Wallet;
using Random = UnityEngine.Random;

internal class BackendHandler_DevMode : BackendHandlerBase
{
    //Wallet used for DevMode Profile
    private readonly string _devWalletId = "74369cd6-493a-4132-8824-7bd94b952ed0";

    //Token Ids
    private static readonly Dictionary<string, int> _tokens = new ()
    {
        {"Rubbish Raccoon:Party Hat", 3},
        {"Rubbish Raccoon:Safety", 4},
        {"Rubbish Raccoon:Shirt Venly", 5},
        {"Rubbish Raccoon:Cap Venly", 20},
        {"Rubbish Raccoon:Shirt GDCVenly", 45},
        {"Trash Cat:Party Hat", 6},
        {"Trash Cat:Safety", 7},
        {"Trash Cat:Shirt Venly", 8},
        {"Trash Cat:Smart", 30},
        {"Trash Cat:Cap Venly", 21},
        {"Trash Cat:Shirt GDCVenly", 44},
        {"Rubbish Raccoon", 1}
    };

    //email & pass are ignored when using DevMode Profile
    public override VyTask SignIn(string email, string password)
    {
        var taskNotifier = VyTask.Create();

        taskNotifier.Scope(async () =>
        {
            //Retrieve Dev Wallet
            var wallet = await RetrieveWallet().AwaitResult();

            //Update PlayerData with Wallet Information
            await PlayerDataWeb3.instance.Refresh(wallet);

            //Notify Success
            taskNotifier.NotifySuccess();
        });

        return taskNotifier.Task;
    }

    //SignUp is equal to SignIn for DevMode Profile (Input are ignored)
    public override VyTask SignUp(string email, string password, string pincode, string nickname = null)
    {
        return SignIn(null, null);
    }

    public override VyTask<VyWalletDto> RetrieveWallet(string walletId = null, bool includeBalance = true)
    {
        return base.RetrieveWallet(_devWalletId, includeBalance);
    }

    public override VyTask<ClaimTokenResponse> ClaimToken(ClaimTokenRequest claimRequest, int cost = -1)
    {
        //Find Token ID
        var tokenId = -1;
        if (claimRequest.RandomDrop)
        {
            var tokenCount = _tokens.Count;
            var rngIndex = Random.Range(0, tokenCount);
            tokenId = _tokens.ElementAt(rngIndex).Value;
        }
        else
        {
            if(!_tokens.ContainsKey(claimRequest.ItemName))
                return VyTask<ClaimTokenResponse>.Failed($"Token for \'{claimRequest.ItemName}\' not found!");

            if (PlayerDataWeb3.instance.User.Coins < cost)
                return VyTask<ClaimTokenResponse>.Failed("Not enough coins to claim this token!");

            tokenId = _tokens[claimRequest.ItemName];
        }

        //Mint Token
        var taskNotifier = VyTask<ClaimTokenResponse>.Create();
        taskNotifier.Scope(async () =>
        {
            var mintParams = new VyMintTokensRequest()
            {
                Destinations = new []
                {
                    new VyTokenDestinationDto
                    {
                        Address = PlayerDataWeb3.instance.UserWallet.Address,
                        Amount = 1
                    }
                }
            };

            var mintedTokens = await VenlyAPI.Nft.MintTokens(ContractId, tokenId, mintParams).AwaitResult();
            var nft = mintedTokens[0].Metadata.ToObject<VyMultiTokenDto>();

            var coinBalance = PlayerDataWeb3.instance.User.Coins;
            if (!claimRequest.RandomDrop)
            {
                coinBalance -= cost;
            }

            taskNotifier.NotifySuccess(new ClaimTokenResponse(nft, coinBalance));
        });

        return taskNotifier.Task;
    }

    public override VyTask<int> RetrieveCoinBalance()
    {
        return VyTask<int>.Succeeded(10000);
    }
}