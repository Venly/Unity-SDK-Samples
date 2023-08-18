using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VenlyDash_Azure
{
    public class TokenInfo
    {
        public TokenInfo(string name, int cost, int tokenId)
        {
            Name = name;
            Cost = cost;
            TokenId = tokenId;
        }

        public string Name;
        public int Cost;
        public int TokenId;
    }

    static class VenlyDashTokens
    {
        public static readonly int ContractId = 38173;
        private static readonly List<TokenInfo> _accessories = new List<TokenInfo>
        {
            new ("Rubbish Raccoon:Party Hat", 1000, 3),
            new ("Rubbish Raccoon:Safety", 1000, 4),
            new ("Rubbish Raccoon:Shirt Venly",1000, 5),
            new ("Rubbish Raccoon:Cap Venly",1000, 20),
            new ("Rubbish Raccoon:Shirt GDCVenly",500, 45),
            new ("Trash Cat:Party Hat",1000, 6),
            new ("Trash Cat:Safety",1000, 7),
            new ("Trash Cat:Shirt Venly",1000, 8),
            new ("Trash Cat:Smart",1000, 30),
            new ("Trash Cat:Cap Venly",1000, 21),
            new ("Trash Cat:Shirt GDCVenly",500, 44),
        };

        private static readonly List<TokenInfo> _characters = new List<TokenInfo>
        {
            new ("Rubbish Raccoon", 3000, 1),
        };

        public static TokenInfo GetRandomTokenInfo()
        {
            var rngItemType = Random.Shared.Next(0, 101);
            if (rngItemType > 25) //Accessory
            {
                return _accessories[Random.Shared.Next(0, _accessories.Count)];
            }

            return _characters[Random.Shared.Next(0, _characters.Count)];
        }

        public static TokenInfo GetTokenInfo(ClaimTokenRequest request)
        {
            if (request.ItemType == "accessory") return GetAccessoryTokenInfo(request.ItemName);
            if (request.ItemType == "character") return GetCharacterTokenInfo(request.ItemName);

            throw new ArgumentException($"TokenInfo for itemType=\'{request.ItemType}\' not found.");
        }

        public static TokenInfo GetAccessoryTokenInfo(string character, string accessory)
        {
            return GetAccessoryTokenInfo($"{character}:{accessory}");
        }

        public static TokenInfo GetAccessoryTokenInfo(string fullName)
        {
            var token =  _accessories.FirstOrDefault(t => t.Name.Equals(fullName));

            if(token == null)
                throw new ArgumentException($"TokenInfo for (accessory) item=\'{fullName}\' not found.");

            return token;
        }

        public static TokenInfo GetCharacterTokenInfo(string character)
        {
            var token = _characters.FirstOrDefault(t => t.Name.Equals(character));

            if (token == null)
                throw new ArgumentException($"TokenInfo for (character) item=\'{character}\' not found.");

            return token;
        }
    }
}
