using System.Collections.Generic;
using UnityEngine;

namespace Cadi.Scripts.UI.GraphicSystems
{
    public static class GraphicUtils
    {
        private static readonly Dictionary<Texture, Sprite> s_SpriteCache = new Dictionary<Texture, Sprite>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ClearCache()
        {
            s_SpriteCache.Clear();
        }

        public static Sprite GetSpriteFromTexture(Texture texture)
        {
            if (texture == null)
                return null;

            if (texture is not Texture2D texture2D)
            {
                Debug.LogWarning($"Cannot create Sprite from texture type {texture.GetType().Name}. Expected Texture2D.");
                return null;
            }

            if (s_SpriteCache.TryGetValue(texture, out var sprite))
                return sprite;

            var newSprite = Sprite.Create(
                texture2D,
                new Rect(0, 0, texture2D.width, texture2D.height),
                Vector2.one * 0.5f
            );

            s_SpriteCache[texture] = newSprite;
            return newSprite;
        }

        public static Texture TextureFromSprite(Sprite sprite)
        {
            return sprite?.texture;
        }
    }
}