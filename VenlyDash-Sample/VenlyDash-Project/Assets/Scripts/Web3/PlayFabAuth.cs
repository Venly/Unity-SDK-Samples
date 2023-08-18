//BASED ON PlayFabAuthService.cs
//From the PlayFab GitHub Sample Project
//https://github.com/PlayFab/PlayFab-Samples/blob/master/Samples/Unity/PlayFabSignIn/Assets/Scripts/PlayFab/PlayFabAuthService.cs

#if ENABLE_VENLY_PLAYFAB
using PlayFab;
using PlayFab.ClientModels;
using Venly.Backends.PlayFab;
using Venly.Core;

public static class PlayFabAuth
{
    public static GetPlayerCombinedInfoRequestParams InfoRequestParams = new GetPlayerCombinedInfoRequestParams()
    {
        GetPlayerProfile = true
    };

    public static VyTask<LoginResult> SignUp(string email, string password, string nickname = null)
    {
        var taskNotifier = VyTask<LoginResult>.Create();

        RegisterPlayFabUserRequest registerUserRequest = new RegisterPlayFabUserRequest
        {
            TitleId = PlayFabSettings.TitleId,
            Email = email,
            Password = password,
            DisplayName = nickname,
            RequireBothUsernameAndEmail = false,
            InfoRequestParameters = InfoRequestParams
        };

        PlayFabClientAPI.RegisterPlayFabUser(registerUserRequest,
            (registerResult) =>
            {
                SignIn(email, password)
                    .OnComplete(taskNotifier.Notify);
            },
            (error) =>
            {
                taskNotifier.NotifyFail(error.ToVyException());
            });

        return taskNotifier.Task;
    }

    public static VyTask<LoginResult> SignIn(string email, string password)
    {
        // If username & password is empty, then do not continue, and Call back to Authentication UI Display 
        if (string.IsNullOrEmpty(email) && string.IsNullOrEmpty(password))
        {
            return VyTask<LoginResult>.Failed(new PlayFabError()
            {
                ErrorMessage = "Sign-In failed, Email and/or Password are empty!"
            }.ToVyException());
        }

        //Create promise for current call
        var taskNotifier = VyTask<LoginResult>.Create();

        // We have not opted for remember me in a previous session, so now we have to login the user with email & password.
        PlayFabClientAPI.LoginWithEmailAddress(
            new LoginWithEmailAddressRequest()
            {
                TitleId = PlayFabSettings.TitleId,
                Email = email,
                Password = password,
                InfoRequestParameters = InfoRequestParams
            },

            // Success
            (result) =>
            {
                taskNotifier.NotifySuccess(result);
            },

            // Failure
            (error) =>
            {
                taskNotifier.NotifyFail(error.ToVyException());
            });

        return taskNotifier.Task;
    }
}
#endif