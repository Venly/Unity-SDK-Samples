using UnityEngine;
using UnityEngine.SceneManagement;

public class StartButton : MonoBehaviour
{
    public void StartGame()
    {
        if (PlayerDataWeb3.instance.ftueLevel == 0)
        {
            PlayerDataWeb3.instance.ftueLevel = 1;
            PlayerDataWeb3.instance.SaveLocalData();

        }

        //SHOW LOADER
        Web3Controller.ShowLoader("Loading User Data");

        //PERFORM LOGIN/DEMO/SIGNUP
//#if ENABLE_VENLY_DEVMODE
//        Web3Controller.SignIn(null,null)
//#else
        Web3Controller.SignIn_Demo()
//#endif
            .OnSuccess(() =>
            {
                Debug.Log("LoadScene MAIN");
                SceneManager.LoadScene("main");
            })
            .OnFail(Web3Controller.ShowException)
            .Finally(Web3Controller.HideLoader);

    }
}
