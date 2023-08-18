using UnityEngine;
using UnityEngine.UI;
using Venly.Core;

public class GameOverUI : MonoBehaviour
{
    private bool _isClaiming = false;
    public Button BtnOK;
    public Text TxtCollected;
    public Text TxtScore;

    public void HandleEndSession(int amount, int score)
    {
        if (_isClaiming)
        {
            Debug.LogWarning("[GameOverUI] ClaimCoins called multiple times");
            return;
        }

        BtnOK.interactable = false;
        TxtCollected.text = $"{amount}";
        TxtScore.text = $"Score: {score}";

        Web3Controller.ShowLoader("Processing Score");

        var deferredTask = VyTask.Create();

        Web3Controller.ClaimCoins(amount)
            .OnSuccess(() =>
            {
                //Update Score
                Web3Controller.UpdateLeaderboard(score)
                    .OnSuccess(deferredTask.NotifySuccess)
                    .OnFail(deferredTask.NotifyFail);
            })
            .OnFail(deferredTask.NotifyFail);

        deferredTask.Task
            .OnFail(Web3Controller.ShowException)
            .Finally(() =>
            {
                Web3Controller.HideLoader();
                BtnOK.interactable = true;
            });
    }
}
