using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelLoader : MonoBehaviour
{
    public string[] AdditiveScenesToLoad = new string[0];

    private void Start()
    {
        foreach (var scene in AdditiveScenesToLoad)
        {
            LoadSceneAdditive(scene);
        }
    }

    public void LoadLevel(string name)
    {
        SceneManager.LoadScene(name);
    }

    public void LoadSceneAdditive(string scene)
    { 
        SceneManager.LoadScene(scene,LoadSceneMode.Additive);
    }
}
