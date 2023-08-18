using System.Linq;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;

// Prefill the info on the player data, as they will be used to populate the leadboard.
public class Leaderboard : MonoBehaviour
{
	public RectTransform entriesRoot;
    public bool isGlobal;
	//public int entriesCount;

    //public GameObject entryPrefab;
	//public HighscoreUI playerEntry;
	//public bool forcePlayerDisplay;
	//public bool displayPlayer = true;

	public void Open()
	{
		gameObject.SetActive(true);

        if (!PlayerDataWeb3.instance.IsLeaderboardLoaded(isGlobal))
        {
            Refresh();
        }
        else Populate();
	}

	public void Close()
	{
		gameObject.SetActive(false);
	}

    public void Refresh()
    {
        Web3Controller.ShowLoader("Refreshing");
        PlayerDataWeb3.instance.RefreshHighscores(isGlobal, true)
            .OnSuccess(Populate)
            .OnFail(Web3Controller.ShowException)
            .Finally(Web3Controller.HideLoader);
    }

    private void SetScoreUI(HighscoreUI scoreUI, ref HighscoreEntry entry, bool isPlayer = false)
    {
        scoreUI.gameObject.SetActive(true);

        scoreUI.number.text = (entry.position + 1).ToString();
        scoreUI.score.text = entry.score.ToString();
        scoreUI.playerName.text = entry.name;

        scoreUI.SetHighlight(isPlayer);
    }

	public void Populate()
    {
		//Gather Data
        var localHighscores = PlayerDataWeb3.instance.LocalHighscore;
        var globalHighscores = PlayerDataWeb3.instance.GlobalHighscore;

        var playerHighscore = localHighscores.FirstOrDefault(e => e.name.Equals(PlayerDataWeb3.instance.User.Nickname));
        var playerHasScore = !string.IsNullOrEmpty(playerHighscore.name);

        var numEntries = entriesRoot.childCount;

        var playerMarked = false;
        var highscoreList = isGlobal ? globalHighscores : localHighscores;
        for (var i = 0; i < numEntries; ++i)
        {
            var entryGO = entriesRoot.GetChild(i).gameObject;
            var scoreScript = entryGO.GetComponent<HighscoreUI>();
            entryGO.gameObject.SetActive(false);

            if (i >= highscoreList.Count)
            {
                if (isGlobal && !playerMarked && playerHasScore)
                {
                    //Set Player Stats
                    playerMarked = true;
                    SetScoreUI(scoreScript, ref playerHighscore, true);
                }

                continue;
            }

            if (i == (numEntries-1) && !playerMarked && playerHasScore)
            {
                playerMarked = true;
                SetScoreUI(scoreScript, ref playerHighscore, true);
                continue;
            }

            var scoreEntry = highscoreList[i];
            var isPlayer = playerHasScore && scoreEntry.position == playerHighscore.position;
            if (isPlayer)
            {
                playerMarked = true;
                SetScoreUI(scoreScript, ref playerHighscore, true);
            }
            else SetScoreUI(scoreScript, ref scoreEntry, false);
        }
    }
}
