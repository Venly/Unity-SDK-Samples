using UnityEngine;
using UnityEngine.UI;

public class AdsForMission : MonoBehaviour
{
    public MissionUI missionUI;

    public Text newMissionText;
    public Button adsButton;

    void OnEnable ()
    {
        adsButton.gameObject.SetActive(false);
        newMissionText.gameObject.SetActive(false);

        // Only present an ad offer if less than 3 missions.
        if (PlayerDataWeb3.instance.missions.Count >= 3)
        {
            return;
        }
    }

    void AddNewMission()
    {
        PlayerDataWeb3.instance.AddMission();
        PlayerDataWeb3.instance.SaveLocalData();
        StartCoroutine(missionUI.Open());
    }
}
