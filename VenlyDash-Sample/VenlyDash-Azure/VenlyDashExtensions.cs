using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.ServerModels;
using Venly;
using Venly.Backends;
using Venly.Companion;
using Venly.Core;
using Venly.Models.Nft;
using Venly.Models.Shared;

namespace VenlyDash_Azure
{
    public class ClaimTokenRequest
    {
        [JsonProperty("random")] public bool RandomDrop { get; private set; }
        [JsonProperty("itemType")] public string ItemType { get; private set; }
        [JsonProperty("itemName")] public string ItemName { get; private set; }
    }

    public class ClaimTokenResponse
    {
        [JsonProperty("token")] public VyMultiTokenDto Token { get; set; }
        [JsonProperty("coinBalance")] public int CoinBalance { get; set; }
    }

    static class VenlyDashExtensions
    {
        public static async Task<int> GetCoinBalance(PlayFabRequest pfRequest)
        {
            var userDataRequest = new GetUserDataRequest
            {
                PlayFabId = pfRequest.PlayFabId,
                Keys = new List<string> {"coins"}
            };

            var result = await PlayFabServerAPI.GetUserDataAsync(userDataRequest);
            var data = result.Result.Data;

            if (data.ContainsKey("coins"))
                return int.Parse(data["coins"].Value);

            return 0;
        }

        public static async Task UpdateCoinBalance(PlayFabRequest pfRequest, int coinBalance)
        {
            var userDataRequest = new UpdateUserDataRequest()
            {
                PlayFabId = pfRequest.PlayFabId,
                Data = new Dictionary<string, string>
                {
                    {"coins", coinBalance.ToString()}
                }
            };

            await PlayFabServerAPI.UpdateUserDataAsync(userDataRequest);
        }

        public static async Task UpdateLeaderboard(PlayFabRequest pfRequest, int score)
        {
            var updateStatRequest = new UpdatePlayerStatisticsRequest()
            {
                PlayFabId = pfRequest.PlayFabId,
                Statistics = new List<StatisticUpdate>
                {
                    new StatisticUpdate
                    {
                        StatisticName = "VenlyDashScore",
                        Value = score
                    }
                }
            };

            await PlayFabServerAPI.UpdatePlayerStatisticsAsync(updateStatRequest);
        }

        [VyExtensionRoute("claim_token")]
        public static async VyTask<VyServerResponseDto> ClaimToken(PlayFabRequest pfRequest)
        {
            try
            {
                var claimRequest = pfRequest.Data.GetJsonContent<ClaimTokenRequest>();

                var tokenInfo = claimRequest.RandomDrop
                    ? VenlyDashTokens.GetRandomTokenInfo()
                    : VenlyDashTokens.GetTokenInfo(claimRequest);

                //Check UserBalance
                var balance = -1;

                //Ignore if Randome
                if (!claimRequest.RandomDrop)
                {
                    balance = await GetCoinBalance(pfRequest);

                    if (balance < tokenInfo.Cost)
                        return VyServerResponseDto.Failed(
                            $"Insufficient Coin Balance. (Cost={tokenInfo.Cost}, Required={balance}");
                }

                //Get User Wallet
                var walletId = await PlayFabExtensions.GetWalletIdForUser(pfRequest.PlayFabId);
                var userWallet = await VenlyAPI.Wallet.GetWallet(walletId).AwaitResult();

                //Mint Token
                var mintParams = new VyMintTokensRequest()
                {
                    Destinations = new VyTokenDestinationDto[]
                    {
                        new VyTokenDestinationDto()
                        {
                            Address = userWallet.Address,
                            Amount = 1
                        }
                    }
                };

                var token = await VenlyAPI.Nft.MintTokens(VenlyDashTokens.ContractId, tokenInfo.TokenId, mintParams).AwaitResult();
                var multiToken = token[0].Metadata.ToObject<VyMultiTokenDto>();

                if (!claimRequest.RandomDrop)
                {
                    //Return to sender
                    balance -= tokenInfo.Cost;

                    //Update User Balance
                    await UpdateCoinBalance(pfRequest, balance);
                }

                return VyServerResponseDto.Succeeded(new ClaimTokenResponse
                {
                    Token = multiToken,
                    CoinBalance = balance
                });

            }
            catch (Exception ex)
            {
                return VyServerResponseDto.Failed(ex);
            }
        }
    }
}
