#if ENABLE_VENLY_PLAYFAB
using System;
using System.Collections.Generic;
using System.Linq;
using PlayFab;
using PlayFab.ClientModels;
using Venly;
using Venly.Backends.PlayFab;
using Venly.Core;
using Venly.Models.Shared;
using Venly.Models.Wallet;

internal class BackendHandler_PlayFab : BackendHandlerBase
{
    private LoginResult _loginResult;

    public override VyTask SignIn(string email, string password)
    {
        var taskNotifier = VyTask.Create();

        //SignIn with PlayFab, retrieve walletId
        taskNotifier.Scope(async () =>
        {
            //Sign In with PlayFab
            var loginResult = await PlayFabAuth.SignIn(email, password).AwaitResult();

            //Set API Provider Authentication Context
            VenlyAPI.SetProviderData(VyProvider_PlayFab.AuthContextDataKey, loginResult.AuthenticationContext);

            //Store Login Result
            _loginResult = loginResult;

            //Update UserData (todo: refactor)
            PlayerDataWeb3.instance.User = new()
            {
                Nickname = loginResult.InfoResultPayload.PlayerProfile.DisplayName
            };

            //Retrieve Wallet for User
            var wallet = await VenlyAPI.ProviderExtensions.GetWalletForUser().AwaitResult();

            //Set Wallet
            await PlayerDataWeb3.instance.Refresh(wallet);

            taskNotifier.NotifySuccess();
        });

        return taskNotifier.Task;
    }

    public override VyTask SignUp(string email, string password, string pincode, string nickname = null)
    {
        var taskNotifier = VyTask.Create();

        //SignIn with PlayFab, retrieve walletId
        taskNotifier.Scope(async () =>
        {
            //Sign In with PlayFab
            var loginResult = await PlayFabAuth.SignUp(email, password, nickname).AwaitResult();

            //Set API Provider Authentication Context
            VenlyAPI.SetProviderData(VyProvider_PlayFab.AuthContextDataKey, loginResult.AuthenticationContext);

            //Update UserData
            PlayerDataWeb3.instance.User = new()
            {
                Nickname = loginResult.InfoResultPayload.PlayerProfile.DisplayName
            };

            //Create Wallet for User
            var createParams = new VyCreateWalletRequest()
            {
                Chain = DefaultChain,
                Identifier = DefaultWalletIdentifier,
                Description = $"VenlyDash Demo Wallet for PlayFab User (id={loginResult.PlayFabId} | {email})",
                Pincode = pincode,
                WalletType = eVyWalletType.WhiteLabel
            };

            var wallet = await VenlyAPI.ProviderExtensions.CreateWalletForUser(createParams).AwaitResult();

            //Set Wallet
            await PlayerDataWeb3.instance.Refresh(wallet);

            taskNotifier.NotifySuccess();
        });

        return taskNotifier.Task;
    }

    public override VyTask<int> RetrieveCoinBalance()
    {
        var taskNotifier = VyTask<int>.Create();

        //Retrieve Coins From PlayFab
        var dataRequest = new GetUserDataRequest
        {
            PlayFabId = _loginResult.PlayFabId,
            AuthenticationContext = _loginResult.AuthenticationContext,
            Keys = new List<string> {"coins"}
        };

        //Execute PlayFab Call
        PlayFabClientAPI.GetUserData(dataRequest, (dataResult) =>
            {
                int coinBalance = 0;
                if (dataResult.Data.ContainsKey("coins"))
                {
                    var coinsData = dataResult.Data["coins"];
                    if (!int.TryParse(coinsData.Value, out coinBalance))
                    {
                        taskNotifier.NotifyFail(new Exception("Failed to parse CoinBalance (PlayFab)"));
                    }
                }

                taskNotifier.NotifySuccess(coinBalance);
            },
            (error) =>
            {
                taskNotifier.NotifyFail(
                    new Exception($"Failed to retrieve Coins data (PlayFab Error = {error.ErrorMessage})"));
            });

        return taskNotifier.Task;
    }

    public override VyTask<int> UpdateCoinBalance(int coinBalance)
    {
        var taskNotifier = VyTask<int>.Create();

        //Update PlayFab User Coins data
        var updateRequest = new UpdateUserDataRequest
        {
            AuthenticationContext = _loginResult.AuthenticationContext,
            Data = new Dictionary<string, string>
            {
                {"coins", coinBalance.ToString()}
            }
        };

        PlayFabClientAPI.UpdateUserData(updateRequest, (result) =>
            {
                taskNotifier.NotifySuccess(coinBalance);
            },
            (error) =>
            {
                taskNotifier.NotifyFail(new Exception($"Failed to update player Coins data. (PlayFab Error = {error.ErrorMessage})"));
            });

        return taskNotifier.Task;
    }

    public override VyTask<List<HighscoreEntry>> RetrieveLeaderboard(bool aroundPlayer, int maxEntries = 10)
    {
        var refreshNotifier = VyTask<List<HighscoreEntry>>.Create();

        if (aroundPlayer)
        {
            //Get Leaderboard
            var getStatRequest = new GetLeaderboardAroundPlayerRequest()
            {
                AuthenticationContext = _loginResult.AuthenticationContext,
                PlayFabId = _loginResult.PlayFabId,
                MaxResultsCount = maxEntries,
                StatisticName = "VenlyDashScore"
            };

            PlayFabClientAPI.GetLeaderboardAroundPlayer(getStatRequest, result =>
                {
                    var highscores = result.Leaderboard.Select(le => new HighscoreEntry
                    {
                        position = le.Position,
                        name = le.DisplayName,
                        score = le.StatValue
                    }).ToList();

                    refreshNotifier.NotifySuccess(highscores);
                },
                error => { refreshNotifier.NotifyFail(error.ToVyException()); });
        }
        else
        {
            //Get Leaderboard
            var getStatRequest = new GetLeaderboardRequest()
            {
                AuthenticationContext = _loginResult.AuthenticationContext,
                StartPosition = 0,
                MaxResultsCount = maxEntries,
                StatisticName = "VenlyDashScore"
            };

            PlayFabClientAPI.GetLeaderboard(getStatRequest, result =>
                {
                    var highscores = result.Leaderboard.Select(le => new HighscoreEntry
                    {
                        position = le.Position,
                        name = le.DisplayName,
                        score = le.StatValue
                    }).ToList();

                    refreshNotifier.NotifySuccess(highscores);
                },
                error => { refreshNotifier.NotifyFail(error.ToVyException()); });
        }

        return refreshNotifier.Task;
    }

    public override VyTask UpdateLeaderboard(int score)
    {
        var updateNotifier = VyTask.Create();

        //Store to leaderboard
        var updateRequest = new UpdatePlayerStatisticsRequest
        {
            AuthenticationContext = _loginResult.AuthenticationContext,
            Statistics = new List<StatisticUpdate>
            {
                new StatisticUpdate
                {
                    StatisticName = "VenlyDashScore",
                    Value = score
                }
            }
        };

        PlayFabClientAPI.UpdatePlayerStatistics(updateRequest, result =>
            {
                updateNotifier.NotifySuccess();
            },
            error =>
            {
                updateNotifier.NotifyFail(error.ToVyException());
            });

        return updateNotifier.Task;
    }
}
#endif