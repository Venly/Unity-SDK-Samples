using System;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Venly;
using Venly.Core;
using Venly.Models.Shared;
using Venly.Models.Wallet;
using Random = UnityEngine.Random;
#if UNITY_EDITOR
using UnityEditor;
#endif

public struct HighscoreEntry : System.IComparable<HighscoreEntry>
{
    public int position;
    public string name;
    public int score;

    public int CompareTo(HighscoreEntry other)
    {
        // We want to sort from highest to lowest, so inverse the comparison.
        return other.score.CompareTo(score);
    }
}

/// <summary>
/// Web3 Player Data Controller (adapted from Original PlayerData Class)
/// </summary>
public class PlayerDataWeb3
{
    protected static PlayerDataWeb3 m_Instance;

    public static PlayerDataWeb3 instance
    {
        get
        {
            if(m_Instance == null)
                m_Instance = new PlayerDataWeb3();

            return m_Instance;
        }
    }

    protected string saveFile = "";


    //public int coins { get; private set; }
    //public int premium;
    public Dictionary<Consumable.ConsumableType, int> consumables = new Dictionary<Consumable.ConsumableType, int>();   // Inventory of owned consumables and quantity.

    public List<string> characters = new List<string>();    // Inventory of characters owned.

    private int _usedCharacter = -1;
    public int usedCharacter
    {
        get
        {
            if (_usedCharacter >= characters.Count)
            {
                _usedCharacter = 0;
            }

            return _usedCharacter;
        }
        set => _usedCharacter = value;
    }

    public string desiredAccessory = null;
    public int usedAccessory = -1;
    public List<string> characterAccessories = new List<string>();  // List of owned accessories, in the form "charName:accessoryName".
    public List<string> themes = new List<string>();                // Owned themes.
    public int usedTheme;                                           // Currently used theme.
    public List<HighscoreEntry> highscores = new List<HighscoreEntry>();
    public List<MissionBase> missions = new List<MissionBase>();

    public string previousName = "Trash Cat";

    public bool licenceAccepted = true;
    public bool tutorialDone = true; //always skip tutorial...

    public float masterVolume = float.MinValue, musicVolume = float.MinValue, masterSFXVolume = float.MinValue;

    //ftue = First Time User Expeerience. This var is used to track thing a player do for the first time. It increment everytime the user do one of the step
    //e.g. it will increment to 1 when they click Start, to 2 when doing the first run, 3 when running at least 300m etc.
    public int ftueLevel = 0;
    //Player win a rank ever 300m (e.g. a player having reached 1200m at least once will be rank 4)
    public int rank = 0;

    // This will allow us to add data even after production, and so keep all existing save STILL valid. See loading & saving for how it work.
    // Note in a real production it would probably reset that to 1 before release (as all dev save don't have to be compatible w/ final product)
    // Then would increment again with every subsequent patches. We kept it to its dev value here for teaching purpose. 
    static int s_Version = 13;


    //WEB 3 Properties
    public VyWalletDto UserWallet { get; set; }
    public VyMultiTokenDto[] NFTs { get; set; }

    public List<HighscoreEntry> LocalHighscore { get; private set; } = null;
    public List<HighscoreEntry> GlobalHighscore { get; private set; } = null;

    //PlayFab
    public class UserData
    {
        public UserData()
        {
            Nickname = "DEMO";
        }

        public string Nickname { get; set; }
        public int Coins { get; private set; }

        public void UpdateCoins(int coins)
        {
            Coins = coins;
        }
    }

    public UserData User { get; set; } = new (){Nickname = "Unnamed"};

    public void Init()
    {
        //if we create the PlayerData, mean it's the very first call, so we use that to init the database
        //this allow to always init the database at the earlier we can, i.e. the start screen if started normally on device
        //or the Loadout screen if testing in editor
        CoroutineHandler.StartStaticCoroutine(CharacterDatabase.LoadDatabase());
        CoroutineHandler.StartStaticCoroutine(ThemeDatabase.LoadDatabase());

        //Save File
        saveFile = Application.persistentDataPath + "/web3_save.bin";

        if (File.Exists(saveFile))
        {
            // If we have a save, we read it.
            Read();
        }
        else
        {
            // If not we create one with default data.
            NewSave();
        }

        CheckMissionsCount();
    }

#region LOAD USERDATA

    public VyTask RefreshWallet(bool reloadData = false)
    {
        if(UserWallet == null)
            return VyTask.Failed("[RefreshWallet] Current UserWallet is null");

        ////Demo Path
        //if(Web3Controller.IsDemo)
        //    return Web3Controller.SignIn_Demo();

        ////Non-Demo Path
        var taskNotifier = VyTask.Create();

        Web3Controller.RetrieveWallet(UserWallet.Id)
            .OnSuccess(wallet =>
            {
                UserWallet = wallet;

                if (reloadData)
                {
                    Refresh()
                        .Finally(taskNotifier.NotifySuccess);
                }
                else taskNotifier.NotifySuccess();
            })
            .OnFail(taskNotifier.NotifyFail);

        return taskNotifier.Task;
    }

    public VyTask Refresh(VyWalletDto walletOverride = null)
    {
        if (walletOverride != null)
            UserWallet = walletOverride;

        if (UserWallet == null)
            return VyTask.Failed("[LoadUserData] UserWallet is NULL");

        var taskNotifier = VyTask.Create();

        taskNotifier.Scope(async () =>
        {
            //Retrieve UserWallet NFTs (MultiTokens)
            NFTs = await Web3Controller.RetrieveNFTs().AwaitResult();
            Debug.Log($"[PlayerDataWeb3::Refresh] Found {NFTs?.Length} valid token(s)");

            //Refresh CoinBalance
            int coinBalance = await Web3Controller.RetrieveCoinBalance().AwaitResult();
            User.UpdateCoins(coinBalance);

            //Refresh UserItems (Inventory)
            RefreshUserItems();

            //Notify Success
            taskNotifier.NotifySuccess();
        });

        return taskNotifier.Task;

//        //DEMO PATH
//        if (Web3Controller.IsDemo)
//        {
//            taskNotifier.Scope(async () =>
//            {
//                NFTs = Web3MockData.GetMockMultiTokens();
//                await RefreshUserCoins();
//                RefreshUserItems();

//                taskNotifier.NotifySuccess();
//            });
//        }
//        else
//        {
//#if ENABLE_VENLY_PLAYFAB
//            taskNotifier.Scope(async () =>
//            {
//                var retrievedTokens = await VenlyAPI.Wallet.GetMultiTokenBalances(UserWallet.Id).AwaitResult();
//                NFTs = retrievedTokens.Where(t =>
//                    t.Contract.Address.Equals(Web3Controller.ContractAddress, StringComparison.OrdinalIgnoreCase)).ToArray();

//                Debug.Log($"[InitializeWallet] Found {NFTs?.Length} valid token(s)");

//                //Get Coins
//                await RefreshUserCoins();

//                //Get NFTs
//                RefreshUserItems();

//                taskNotifier.NotifySuccess();
//            });
//#else
//            //TODO
//            taskNotifier.NotifyFail(new NotImplementedException());
//#endif
    //}

        //return taskNotifier.Task;
    }

    private void RefreshUserItems()
    {
        if (NFTs == null)
        {
            Debug.LogWarning("[RefreshUserItems] NFT Array is NULL");
            return;
        }

        //Clear User Items
        characters.Clear();
        characterAccessories.Clear();
        themes.Clear();

        //Defaults
        characters.Add("Trash Cat");
        themes.Add("Venly");

        //Populate
        foreach (var token in NFTs)
        {
            if (!token.HasAttribute("itemType"))
                continue;

            var itemType = token.GetAttribute("itemType").As<string>();

            switch (itemType)
            {
                case "character":
                {
                    var characterName = token.GetAttribute("characterName").As<string>();

                    if (!characters.Contains(characterName))
                    {
                        characters.Add(characterName);
                        Debug.Log($"[RefreshUserItems] Character Added: \'{characterName}\'");
                    }

                    break;
                }
                case "accessory":
                {
                    var characterName = token.GetAttribute("characterName")?.As<string>()??string.Empty;
                    var accessoryName = token.GetAttribute("accessoryName")?.As<string>()??string.Empty;
                    var fullName = $"{characterName}:{accessoryName}";

                    if (!characterAccessories.Contains(fullName))
                    {
                        characterAccessories.Add(fullName);
                        Debug.Log($"[RefreshUserItems] Accessory Added: \'{fullName}\'");
                    }

                    break;
                }
                default:
                    Debug.LogWarning($"[RefreshUserItems] Unknown ItemType \'{itemType}\'");
                    break;
            }
        }

        //Set Default Outfit
        if (_usedCharacter < 0 && usedAccessory < 0)
        {
            var priorityAccessories = new[]
            {
                "Trash Cat:Shirt Venly",
                "Rubbish Racoon:Shirt Venly",
                "Trash Cat:Cap Venly",
                "Rubbish Racoon:Cap Venly",
                "Trash Cat:Shirt GDCVenly",
                "Rubbish Racoon:Shirt GDCVenly",
                null
            };

            foreach (var priorityAcc in priorityAccessories)
            {
                if (SelectOutput(priorityAcc))
                    break;
            }
        }
    }

    public bool SelectOutput(string fullName = null, bool force = false)
    {
        if (!force && _usedCharacter >= 0 && usedAccessory >= 0)
            return true;

        if (string.IsNullOrEmpty(fullName))
        {
            _usedCharacter = 0;
            usedAccessory = -1;
            desiredAccessory = characterAccessories.FirstOrDefault(a => a.Contains(characters[_usedCharacter]));

            return true;
        }

        var character = fullName.Split(':')[0];

        if (!characters.Contains(character)) return false;
        if (!characterAccessories.Contains(fullName)) return false;

        _usedCharacter = characters.IndexOf(character);
        desiredAccessory = fullName;

        return true;
    }

    public bool IsLeaderboardLoaded(bool isGlobal)
    {
        if (LocalHighscore == null) return false;
        if (isGlobal && GlobalHighscore == null) return false;

        return true;
    }

    public VyTask RefreshHighscores(bool refreshGlobal = true, bool refreshLocal = true)
    {
        var taskNotifier = VyTask.Create();

        taskNotifier.Scope(async () =>
        {
            if (refreshLocal)
            {
                LocalHighscore = await Web3Controller.RetrieveLeaderboard(true).AwaitResult();
            }

            if (refreshGlobal)
            {
                GlobalHighscore = await Web3Controller.RetrieveLeaderboard(false).AwaitResult();
            }
        });

        return taskNotifier.Task;


        //if (Web3Controller.IsDemo)
        //{
            

        //    return VyTask.Succeeded();
        //}

        ////NON-DEMO PATH
        //var taskNotifier = VyTask.Create();
        //taskNotifier.Scope(async () =>
        //{   

        //    if (refreshLocal)
        //    {
        //        LocalHighscore = new List<HighscoreEntry>();
        //        LocalHighscore = await Web3Controller.GetLeaderboard(true).AwaitResult();
        //    }

        //    if (refreshGlobal)
        //    {
        //        GlobalHighscore = new List<HighscoreEntry>();
        //        GlobalHighscore = await Web3Controller.GetLeaderboard(false).AwaitResult();
        //    }

        //    taskNotifier.NotifySuccess();
        //});

        //return taskNotifier.Task;
    }
#endregion

    public VyTask AddCoins(int amount)
    {
        return UpdateCoins(User.Coins + amount);
    }

    public VyTask RemoveCoins(int amount)
    {
        return UpdateCoins(User.Coins - amount);
    }

    public VyTask UpdateCoins(int amount)
    {
        var taskNotifier = VyTask.Create();

        taskNotifier.Scope(async () =>
        {
            var newBalance = await Web3Controller.UpdateCoinBalance(amount).AwaitResult();
            User.UpdateCoins(newBalance);
        });

        return taskNotifier.Task;

//        if (Web3Controller.IsDemo)
//        {
//            User.UpdateCoins(amount);
//            return VyTask.Succeeded();
//        }

//#if ENABLE_VENLY_PLAYFAB
//        var taskNotifier = VyTask.Create();

//        //Update PlayFab User Coins data
//        var updateRequest = new UpdateUserDataRequest
//        {
//            AuthenticationContext = User.AuthContext,
//            Data = new Dictionary<string, string>
//            {
//                {"coins", amount.ToString()}
//            }
//        };

//        PlayFabClientAPI.UpdateUserData(updateRequest, (result) =>
//            {
//                User.UpdateCoins(amount);
//                taskNotifier.NotifySuccess();
//            },
//            (error) =>
//            {
//                taskNotifier.NotifyFail(new Exception($"Failed to update player Coins data. (PlayFab Error = {error.ErrorMessage})"));
//            });

//        return taskNotifier.Task;
//#else
//        //TODO
//        var taskNotifier = VyTask.Create();
//        taskNotifier.NotifyFail(new NotImplementedException());
//        return taskNotifier.Task;
//#endif
    }

    public void Consume(Consumable.ConsumableType type)
    {
        if (!consumables.ContainsKey(type))
            return;

        consumables[type] -= 1;
        if (consumables[type] == 0)
        {
            consumables.Remove(type);
        }

        SaveLocalData();
    }

    public void AddConsumable(Consumable.ConsumableType type)
    {
        if (!consumables.ContainsKey(type))
        {
            consumables[type] = 0;
        }

        consumables[type] += 1;

        SaveLocalData();
    }

    public void AddToken(VyMultiTokenDto token, int coinBalance)
    {
        User.UpdateCoins(coinBalance);
        AddToken(token);
    }

    public void AddToken(VyMultiTokenDto token)
    {
        var nftList = NFTs.ToList();
        nftList.Add(token);
        NFTs = nftList.ToArray();

        RefreshUserItems();
    }

    public void RemoveToken(VyMultiTokenDto token)
    {
        var nftList = NFTs.ToList();
        nftList.Remove(token);
        NFTs = nftList.ToArray();

        RefreshUserItems();
    }

    public void AddCharacter(string name)
    {
        characters.Add(name);
    }

    public void AddTheme(string theme)
    {
        themes.Add(theme);
    }

    public void AddAccessory(string name)
    {
        characterAccessories.Add(name);
    }

    // Mission management

    // Will add missions until we reach 2 missions.
    public void CheckMissionsCount()
    {
        while (missions.Count < 2)
            AddMission();
    }

    public void AddMission()
    {
        int val = Random.Range(0, (int)MissionBase.MissionType.MAX);

        MissionBase newMission = MissionBase.GetNewMissionFromType((MissionBase.MissionType)val);
        newMission.Created();

        missions.Add(newMission);
    }

    public void StartRunMissions(TrackManager manager)
    {
        for (int i = 0; i < missions.Count; ++i)
        {
            missions[i].RunStart(manager);
        }
    }

    public void UpdateMissions(TrackManager manager)
    {
        for (int i = 0; i < missions.Count; ++i)
        {
            missions[i].Update(manager);
        }
    }

    public bool AnyMissionComplete()
    {
        for (int i = 0; i < missions.Count; ++i)
        {
            if (missions[i].isComplete) return true;
        }

        return false;
    }

    public void ClaimMission(MissionBase mission)
    {
        //premium += mission.reward;

        missions.Remove(mission);

        CheckMissionsCount();

        SaveLocalData();
    }

    // High Score management

    public int GetScorePlace(int score)
    {
        HighscoreEntry entry = new HighscoreEntry();
        entry.score = score;
        entry.name = "";

        int index = highscores.BinarySearch(entry);

        return index < 0 ? (~index) : index;
    }

    public void InsertScore(int score, string name)
    {
        HighscoreEntry entry = new HighscoreEntry();
        entry.score = score;
        entry.name = name;

        highscores.Insert(GetScorePlace(score), entry);

        // Keep only the 10 best scores.
        while (highscores.Count > 10)
            highscores.RemoveAt(highscores.Count - 1);
    }

    // File management
    public static void NewSave()
    {
        m_Instance.characters.Clear();
        m_Instance.themes.Clear();
        m_Instance.missions.Clear();
        m_Instance.characterAccessories.Clear();
        m_Instance.consumables.Clear();

        m_Instance.usedCharacter = -1;
        m_Instance.usedTheme = 0;
        m_Instance.usedAccessory = -1;

        //m_Instance.coins = 0;
        //m_Instance.premium = 0;

        m_Instance.characters.Add("Trash Cat");
        m_Instance.themes.Add("Venly");
        m_Instance.ftueLevel = 0;
        m_Instance.rank = 0;

        m_Instance.CheckMissionsCount();

        m_Instance.SaveLocalData();
    }

    public void Read()
    {
        BinaryReader r = new BinaryReader(new FileStream(saveFile, FileMode.Open));

        int ver = r.ReadInt32();

        if (ver < s_Version)
        {
            r.Close();

            NewSave();
            r = new BinaryReader(new FileStream(saveFile, FileMode.Open));
            ver = r.ReadInt32();
        }

        //coins = r.ReadInt32();

        consumables.Clear();
        //int consumableCount = r.ReadInt32();
        
        //for (int i = 0; i < consumableCount; ++i)
        //{
        //    consumables.Add((Consumable.ConsumableType)r.ReadInt32(), r.ReadInt32());
        //}

        ////Consumables Disabled
        //consumables.Clear();


        // Read character.
        //characters.Clear();
        //int charCount = r.ReadInt32();
        //for (int i = 0; i < charCount; ++i)
        //{
        //    string charName = r.ReadString();

        //    //if (charName.Contains("Raccoon") && ver < 11)
        //    //{//in 11 version, we renamed Raccoon (fixing spelling) so we need to patch the save to give the character if player had it already
        //    //    charName = charName.Replace("Racoon", "Raccoon");
        //    //}

        //    //characters.Add(charName);
        //}

        //usedCharacter = r.ReadInt32();

        //// Read character accesories.
        ////characterAccessories.Clear();
        //int accCount = r.ReadInt32();
        //for (int i = 0; i < accCount; ++i)
        //{
        //    r.ReadString();
        //    //characterAccessories.Add(r.ReadString());
        //}

        //// Read Themes.
        //themes.Clear();
        //int themeCount = r.ReadInt32();
        //for (int i = 0; i < themeCount; ++i)
        //{
        //    themes.Add(r.ReadString());
        //}

        usedTheme = r.ReadInt32();

        //// Save contains the version they were written with. If data are added bump the version & test for that version before loading that data.
        //if (ver >= 2)
        //{
        //    premium = r.ReadInt32();
        //}

        // Added highscores.
        if (ver >= 3)
        {
            highscores.Clear();
            int count = r.ReadInt32();
            for (int i = 0; i < count; ++i)
            {
                HighscoreEntry entry = new HighscoreEntry();
                entry.name = r.ReadString();
                entry.score = r.ReadInt32();

                highscores.Add(entry);
            }
        }

        // Added missions.
        if (ver >= 4)
        {
            missions.Clear();

            int count = r.ReadInt32();
            for (int i = 0; i < count; ++i)
            {
                MissionBase.MissionType type = (MissionBase.MissionType)r.ReadInt32();
                MissionBase tempMission = MissionBase.GetNewMissionFromType(type);

                tempMission.Deserialize(r);

                if (tempMission != null)
                {
                    missions.Add(tempMission);
                }
            }
        }

        // Added highscore previous name used.
        if (ver >= 7)
        {
            previousName = r.ReadString();
        }

        if (ver >= 8)
        {
            licenceAccepted = r.ReadBoolean();
        }

        if (ver >= 9)
        {
            masterVolume = r.ReadSingle();
            musicVolume = r.ReadSingle();
            masterSFXVolume = r.ReadSingle();
        }

        if (ver >= 10)
        {
            ftueLevel = r.ReadInt32();
            rank = r.ReadInt32();
        }

        if (ver >= 12)
        {
            tutorialDone = r.ReadBoolean();
        }

        //Overides
        tutorialDone = true;
        //coins = 100000;
        //premium = 100;

        r.Close();
    }

    public void SaveLocalData()
    {
        BinaryWriter w = new BinaryWriter(new FileStream(saveFile, FileMode.OpenOrCreate));

        w.Write(s_Version);
        //w.Write(coins);

        //w.Write(consumables.Count);
        //foreach (KeyValuePair<Consumable.ConsumableType, int> p in consumables)
        //{
        //    w.Write((int)p.Key);
        //    w.Write(p.Value);
        //}

        //// Write characters.
        //w.Write(characters.Count);
        //foreach (string c in characters)
        //{
        //    w.Write(c);
        //}

        //w.Write(usedCharacter);

        //w.Write(characterAccessories.Count);
        //foreach (string a in characterAccessories)
        //{
        //    w.Write(a);
        //}

        //// Write themes.
        //w.Write(themes.Count);
        //foreach (string t in themes)
        //{
        //    w.Write(t);
        //}

        w.Write(usedTheme);
        //w.Write(premium);

        // Write highscores.
        w.Write(highscores.Count);
        for (int i = 0; i < highscores.Count; ++i)
        {
            w.Write(highscores[i].name);
            w.Write(highscores[i].score);
        }

        // Write missions.
        w.Write(missions.Count);
        for (int i = 0; i < missions.Count; ++i)
        {
            w.Write((int)missions[i].GetMissionType());
            missions[i].Serialize(w);
        }

        // Write name.
        w.Write(previousName);

        w.Write(licenceAccepted);

        w.Write(masterVolume);
        w.Write(musicVolume);
        w.Write(masterSFXVolume);

        w.Write(ftueLevel);
        w.Write(rank);

        w.Write(tutorialDone);

        w.Close();
    }


}

// Helper class to cheat in the editor for test purpose
#if UNITY_EDITOR
public class PlayerDataWeb3Editor : Editor
{
    [MenuItem("Venly Dash Debug/Give 10000 VENS")]
    public static void GiveCoins()
    {
        PlayerDataWeb3.instance.AddCoins(10000);
        PlayerDataWeb3.instance.SaveLocalData();
    }
}
#endif