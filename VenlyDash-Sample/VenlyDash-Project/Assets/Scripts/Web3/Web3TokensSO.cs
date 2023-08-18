using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[Serializable]
public struct Web3TokenEntry
{
    public string tokenName;
    public string tokenId;
    public Sprite tokenSprite;

    public override string ToString()
    {
        return $"Token {tokenId}";
    }
}

public class Web3TokensSO : ScriptableObject
{
    private static Web3TokensSO _instance;
    public static Web3TokensSO Instance
    {
        get
        {
            if (_instance == null)
                _instance = Resources.Load<Web3TokensSO>("VenlyDashTokens");

            return _instance;
        }
    }

//#if UNITY_EDITOR
//    [MenuItem("Venly Dash Debug/CreateTokenSO")]
//    public static void Create()
//    {
//        var so = ScriptableObject.CreateInstance<Web3TokensSO>();
//        AssetDatabase.CreateAsset(so, "Assets/Resources/VenlyDashTokens.asset");
//        AssetDatabase.SaveAssets();
//    }
//#endif

    [SerializeField]
    public List<Web3TokenEntry> Tokens;
}
