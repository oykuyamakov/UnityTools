using Cadi.Scripts.EventSystem;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif
using UnityEngine;
using UnityEngine.EventSystems;

namespace Cadi.Scripts.UI.GraphicSystems
{
    public class SelectiveGraphix : Graphix, IPointerDownHandler, ISelectiveGraphix
    {
        [SerializeField]
#if ODIN_INSPECTOR
         [FoldoutGroup("Settings/Selection")]
        [InlineProperty, LabelText("Selection")][ShowIf(nameof(ShowSettings))]
#endif
        private SelectionController m_Selection = new();

        private bool m_ShowSettings;
        protected override bool ShowSettings => m_ShowSettings;

        // -----------------------------------------------------------
        // Properties
        // -----------------------------------------------------------

        public int RuntimeID => m_Selection.RuntimeID;
        public bool IsLocked => m_Selection.IsLocked;

        // -----------------------------------------------------------
        // Lifecycle
        // -----------------------------------------------------------

        private void Start()
        {
            Init();
            using var evt = SGraphixCreatedEvent.Rent(this);
            EM.SendEvent(evt, m_Selection.Group);
        }

        private void OnDisable()
        {
            m_Selection.OnDisable();
        }

        private new void OnDestroy()
        {
            m_Selection.OnDestroy();
            m_Slot.Dispose();
        }

        // -----------------------------------------------------------
        // IPointerDownHandler
        // -----------------------------------------------------------

        public void OnPointerDown(PointerEventData eventData)
        {
            m_Selection.HandlePointerDown();
        }

        // -----------------------------------------------------------
        // Public API
        // -----------------------------------------------------------

      
        public void Lock(bool disableVis)
        {
            m_Selection.Lock(disableVis);
            
            if (disableVis)
            {
                m_Slot.ApplyDefault();
            }
        } 
        public void UnLock() => m_Selection.UnLock();

        public void TryDeselect() => m_Selection.TryDeselect();

        public void SetGroup(int runtimeId, SelectiveGraphixGroup group)
        {
            m_Selection.SetGroup(runtimeId, group);
            
            m_Slot.ApplyDefault();
        }

        public void Init() =>
            m_Selection.BindToGraphix(this);

        public void EditorBind(SelectiveGraphixGroup group)
        {
            if (group == null)
                return;

            m_ShowSettings = group.AllowOverridenChildren;
            
            m_Slot.CopyFrom(group.RootSettings);
            m_Selection.EditorBind(group);

#if UNITY_EDITOR
            if (!Application.isPlaying)
                EditorSync();
            if (!Application.isPlaying)
                UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
    }
}
