using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using Venly.Models.Shared;
using Venly.Models.Wallet;

static class Web3MockData
{
    //INITIAL UNLOCKED CHARACTERS (Trash Cat is unlocked by default)
    private static List<string> _initialCharacters = new()
    {
        "Rubbish Raccoon"
    };

    //INITIAL UNLOCKED ACCESSORIES
    private static List<string> _initialAccessories = new()
    {
        "Trash Cat:Party Hat",
        //"Trash Cat:Safety",
        //"Rubbish Raccoon:Party Hat",
        //"Rubbish Raccoon:Safety"
    };

    public static int InitialCoins { get; } = 100000;
    public static int DelayTime = -1; //defined in ms, use value >0 to activate an artificial delay
    public static bool UseDelay => DelayTime > 0;

    public static Task Delay()
    {
        if (UseDelay)
        {
            return Task.Delay(DelayTime);
        }

        return Task.CompletedTask;
    }

    public static VyWalletDto CreateMockWallet()
    {
        var walletJson =
            "{\"id\":\"aebf7eb4-7329-417a-886b-7dd0e8f2e81a\",\"address\":\"0x887564C58107b72Fa2eEE8ecC26a12BcF5d92Ac0\",\"walletType\":\"API_WALLET\",\"secretType\":\"MATIC\",\"createdAt\":\"2023-03-01T14:45:15.403\",\"archived\":false,\"description\":\"UserTestWalletforVenlyDashSample\",\"primary\":false,\"hasCustomPin\":true,\"identifier\":\"VENLY_DASH_USER\",\"balance\":{\"available\":true,\"secretType\":\"MATIC\",\"balance\":0.1937358,\"gasBalance\":0.0,\"symbol\":\"MATIC\",\"gasSymbol\":\"MATIC\",\"rawBalance\":\"0\",\"rawGasBalance\":\"0\",\"decimals\":18}}";

        return JsonConvert.DeserializeObject<VyWalletDto>(walletJson);
    }

    public static VyMultiTokenDto[] GetMockMultiTokens()
    {
        var tokens = new List<VyMultiTokenDto>();

        _initialCharacters.ForEach(c => tokens.Add(GetMockCharacterToken(c)));
        _initialAccessories.ForEach(a => tokens.Add(GetMockAccessoryToken(a)));

        return tokens.ToArray();
    }

    public static VyMultiTokenDto GetMockCharacterToken(string characterName)
    {
        var jsonStr =
            $"{{\"id\":\"3\",\"name\":\"{characterName}\",\"description\":\"Thistokenunlockthe'RubbishRaccoon'characterforVenlyDash\",\"url\":\"venly.io\",\"imageUrl\":\"https://lh3.googleusercontent.com/fxYXR-KWQNKFDV6K5-ld6eQrOX3B9euWg7kxC8txw0d47sFPHumGHsAjKV6gS7_GI3o=w2400\",\"imagePreviewUrl\":\"https://lh3.googleusercontent.com/fxYXR-KWQNKFDV6K5-ld6eQrOX3B9euWg7kxC8txw0d47sFPHumGHsAjKV6gS7_GI3o=w2400\",\"imageThumbnailUrl\":\"https://lh3.googleusercontent.com/fxYXR-KWQNKFDV6K5-ld6eQrOX3B9euWg7kxC8txw0d47sFPHumGHsAjKV6gS7_GI3o=w2400\",\"animationUrls\":[],\"fungible\":false,\"contract\":{{\"name\":\"VenlyDash\",\"description\":\"ContractusedforVenly's'VenlyDash'endlessrunnersample\",\"address\":\"0x88f9564d3894d66b4406c673b37bb8f91cce452f\",\"symbol\":\"VYDASH\",\"media\":[],\"type\":\"ERC_1155\",\"verified\":false,\"premium\":false,\"categories\":[],\"url\":\"www.venly.io\",\"imageUrl\":\"www.venly.io\"}},\"attributes\":[{{\"type\":\"property\",\"name\":\"itemType\",\"value\":\"character\"}},{{\"type\":\"property\",\"name\":\"characterName\",\"value\":\"{characterName}\"}},{{\"type\":\"system\",\"name\":\"tokenTypeId\",\"value\":\"1\"}},{{\"type\":\"property\",\"name\":\"maxSupply\",\"value\":\"100000\"}},{{\"type\":\"property\",\"name\":\"mintNumber\",\"value\":\"2\"}}],\"balance\":1,\"finalBalance\":1,\"transferFees\":false}}";

        return JsonConvert.DeserializeObject<VyMultiTokenDto>(jsonStr);
    }

    public static VyMultiTokenDto GetToken(ClaimTokenRequest claimRequest)
    {
        if (claimRequest.RandomDrop)
        {
            return GetRandomToken();
        }

        return claimRequest.ItemType switch
        {
            "character" => GetMockCharacterToken(claimRequest.ItemName),
            "accessory" => GetMockAccessoryToken(claimRequest.ItemName),
            _ => null
        };
    }

    private static VyMultiTokenDto GetMockAccessoryToken(string fullName)
    {
        var split = fullName.Split(':');
        return GetMockAccessoryToken(split[0], split[1]);
    }

    private static VyMultiTokenDto GetMockAccessoryToken(string characterName, string accessoryName)
    {
        var jsonStr =
            $"{{\"id\":\"3\",\"name\":\"{accessoryName}\",\"description\":\"Thistokenunlockthe'RubbishRaccoon'characterforVenlyDash\",\"url\":\"venly.io\",\"imageUrl\":\"https://lh3.googleusercontent.com/fxYXR-KWQNKFDV6K5-ld6eQrOX3B9euWg7kxC8txw0d47sFPHumGHsAjKV6gS7_GI3o=w2400\",\"imagePreviewUrl\":\"https://lh3.googleusercontent.com/fxYXR-KWQNKFDV6K5-ld6eQrOX3B9euWg7kxC8txw0d47sFPHumGHsAjKV6gS7_GI3o=w2400\",\"imageThumbnailUrl\":\"https://lh3.googleusercontent.com/fxYXR-KWQNKFDV6K5-ld6eQrOX3B9euWg7kxC8txw0d47sFPHumGHsAjKV6gS7_GI3o=w2400\",\"animationUrls\":[],\"fungible\":false,\"contract\":{{\"name\":\"VenlyDash\",\"description\":\"ContractusedforVenly's'VenlyDash'endlessrunnersample\",\"address\":\"0x88f9564d3894d66b4406c673b37bb8f91cce452f\",\"symbol\":\"VYDASH\",\"media\":[],\"type\":\"ERC_1155\",\"verified\":false,\"premium\":false,\"categories\":[],\"url\":\"www.venly.io\",\"imageUrl\":\"www.venly.io\"}},\"attributes\":[{{\"type\":\"property\",\"name\":\"itemType\",\"value\":\"accessory\"}},{{\"type\":\"property\",\"name\":\"characterName\",\"value\":\"{characterName}\"}},{{\"type\":\"property\",\"name\":\"accessoryName\",\"value\":\"{accessoryName}\"}},{{\"type\":\"system\",\"name\":\"tokenTypeId\",\"value\":\"1\"}},{{\"type\":\"property\",\"name\":\"maxSupply\",\"value\":\"100000\"}},{{\"type\":\"property\",\"name\":\"mintNumber\",\"value\":\"2\"}}],\"balance\":1,\"finalBalance\":1,\"transferFees\":false}}";

        return JsonConvert.DeserializeObject<VyMultiTokenDto>(jsonStr);
    }

    private static VyMultiTokenDto GetRandomToken()
    {
        var options = new List<string>
        {
            "Rubbish Raccoon",
            "Trash Cat:Party Hat",
            "Trash Cat:Safety",
            "Rubbish Raccoon:Party Hat",
            "Rubbish Raccoon:Safety"
        };

        var randomId = Random.Range(0, options.Count);
        var itemName = options[randomId];

        if (itemName.Contains(':')) return GetMockAccessoryToken(itemName);
        return GetMockCharacterToken(itemName);
    }
}
