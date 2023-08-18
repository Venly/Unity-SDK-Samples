using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Venly.Core;
using Venly.Models.Shared;

namespace Assets.Scripts.Web3
{
    public static class Web3Resources
    {
        private static readonly Dictionary<int, Texture2D> _hashedTextureLUT = new Dictionary<int, Texture2D>();
        private static readonly Dictionary<int, Sprite> _hashedSpriteLUT = new Dictionary<int, Sprite>();
        private static readonly Sprite _invalidUrlSprite;

        public static VyTask<Sprite> GetTokenSprite(VyMultiTokenDto token)
        {
            Web3TokenEntry tokenEntry = new Web3TokenEntry();
            if (Web3TokensSO.Instance != null)
            {
                var tokenTypeId = token.GetAttributeValue<string>("tokenTypeId");
                tokenEntry = Web3TokensSO.Instance.Tokens.FirstOrDefault(t => t.tokenId == tokenTypeId);
            }

            if (tokenEntry.tokenSprite == null)
            {
                return DownloadSprite(token.ImageUrl);
            }

            return VyTask<Sprite>.Succeeded(tokenEntry.tokenSprite);
        }

        public static VyTask<Sprite> DownloadSprite(string url, bool useCache = true)
        {
            if (string.IsNullOrEmpty(url))
            {
                return VyTask<Sprite>.Failed(new ArgumentNullException("url", "Failed to download token image."));
            }

            var spriteHash = url.GetHashCode();

            if (useCache && _hashedTextureLUT.ContainsKey(spriteHash))
                return VyTask<Sprite>.Succeeded(_hashedSpriteLUT[spriteHash]);

            var taskNotifier = VyTask<Sprite>.Create();

            DownloadTexture(url, useCache)
                .OnSuccess(t =>
                {
                    var s = Sprite.Create(t, new Rect(0.0f, 0.0f, t.width, t.height), new Vector2(0.5f, 0.5f), 100.0f);
                    _hashedSpriteLUT[spriteHash] = s;

                    taskNotifier.NotifySuccess(s);
                })
                .OnFail(taskNotifier.NotifyFail);

            return taskNotifier.Task;
        }

        private static VyTask<Texture2D> DownloadTexture(string url, bool useCache = true)
        {
            if(string.IsNullOrEmpty(url))
                return VyTask<Texture2D>.Failed(new ArgumentNullException("Failed to download image, url is null."));

            var textureHash = url.GetHashCode();

            if (useCache && _hashedTextureLUT.ContainsKey(textureHash))
                return VyTask<Texture2D>.Succeeded(_hashedTextureLUT[textureHash]);

            var taskNotifier = VyTask<Texture2D>.Create();
            VenlyUnityUtils.DownloadImage(url)
                .OnSuccess(tex =>
                {
                    _hashedTextureLUT.Add(textureHash, tex);
                    taskNotifier.NotifySuccess(tex);
                })
                .OnFail(taskNotifier.NotifyFail);

            return taskNotifier.Task;
        }
    }
}
