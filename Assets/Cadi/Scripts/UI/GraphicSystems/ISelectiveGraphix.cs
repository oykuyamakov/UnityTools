using UnityEngine;

namespace Cadi.Scripts.UI.GraphicSystems
{
    public interface ISelectiveGraphix
    {
        int RuntimeID { get; }
        bool IsLocked { get; }
        void Lock(bool disableVis);
        void UnLock();
        void TryDeselect();
        void SetGroup(int runtimeId, SelectiveGraphixGroup group);
        void EditorBind(SelectiveGraphixGroup group);
        void Init();
        RectTransform CachedRectTransform { get; }

        void UpdateVisuals(bool selected);

        // These are provided by MonoBehaviour, so implementors get them for free.
        GameObject gameObject { get; }
        string name { get; }
        Transform transform { get; }
    }
}
