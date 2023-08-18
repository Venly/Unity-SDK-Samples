using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ShopUI : MonoBehaviour
{
    public ConsumableDatabase consumableDatabase;

    public ShopItemList itemList;
    public ShopCharacterList characterList;
    public ShopAccessoriesList accessoriesList;
    public ShopThemeList themeList;

    [Header("UI")]
    public Text coinCounter;
    public Text premiumCounter;
    public Button cheatButton;

    protected ShopList m_OpenList;

    protected const int k_CheatCoins = 1000000;
    protected const int k_CheatPremium = 1000;

    void Start ()
    {
        Web3Controller.Init(true);

        consumableDatabase.Load();
        CoroutineHandler.StartStaticCoroutine(CharacterDatabase.LoadDatabase());
        CoroutineHandler.StartStaticCoroutine(ThemeDatabase.LoadDatabase());


        cheatButton.interactable = true;

        m_OpenList = characterList;
        m_OpenList.Open();
	}
	
	void Update ()
    {
        coinCounter.text = PlayerDataWeb3.instance.User.Coins.ToString();
        //premiumCounter.text = PlayerDataWeb3.instance.premium.ToString();
    }

    public void OpenItemList()
    {
        m_OpenList.Close();
        itemList.Open();
        m_OpenList = itemList;
    }

    public void OpenCharacterList()
    {
        m_OpenList.Close();
        characterList.Open();
        m_OpenList = characterList;
    }

    public void OpenThemeList()
    {
        m_OpenList.Close();
        themeList.Open();
        m_OpenList = themeList;
    }

    public void OpenAccessoriesList()
    {
        m_OpenList.Close();
        accessoriesList.Open();
        m_OpenList = accessoriesList;
    }

    public void LoadScene(string scene)
    {
        SceneManager.LoadScene(scene, LoadSceneMode.Single);
    }

	public void CloseScene()
	{
        SceneManager.UnloadSceneAsync("shop");
	    LoadoutState loadoutState = GameManager.instance.topState as LoadoutState;
	    if(loadoutState != null)
        {
            loadoutState.Refresh();
        }
	}

	public void CheatCoin()
	{
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
        return ; //you can't cheat in production build
#endif

        PlayerDataWeb3.instance.AddCoins(k_CheatCoins);
        //PlayerDataWeb3.instance.premium += k_CheatPremium;
        PlayerDataWeb3.instance.SaveLocalData();
	}
}
