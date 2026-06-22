using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

//using Sirenix.OdinInspector;

namespace Cadi.Scripts.CustomAttributes
{
    [CreateAssetMenu]
    public class AtlasDefinition : ScriptableObject
    {
        public SpriteAtlas BoundAtlas;
        public bool CleanFirst = true;

        //[ListDrawerSettings(NumberOfItemsPerPage = 30)]
        public List<Sprite> Sprites = new List<Sprite>();
    }
}
