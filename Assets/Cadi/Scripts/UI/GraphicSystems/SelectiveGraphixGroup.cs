using System;
using System.Collections.Generic;
using Cadi.Scripts.CacherSystem;
using Cadi.Scripts.EventSystem;
using Cadi.Scripts.UI.FX;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Cadi.Scripts.UI.GraphicSystems
{
    public class SelectiveGraphixGroup : CacherMonoBehaviour
    {
        [SerializeField, FoldoutGroup("Settings"), OnValueChanged(nameof(OnSettingsChanged))]
        [ShowIf(nameof(m_IsNested), IsNested.Nested)]
        private float m_Padding = 5;
        
        [SerializeField, FoldoutGroup("Setup"), OnValueChanged(nameof(OnSettingsChanged))]
        [EnumToggleButtons]
        private IsNested m_IsNested = IsNested.Single;
        
        [SerializeField, FoldoutGroup("Setup"), OnValueChanged(nameof(OnSettingsChanged))]
        [ShowIf(nameof(m_IsNested), IsNested.Nested)]
        [EnumToggleButtons]
        protected SlotLoc m_MainContentSlot = GraphicSystems.SlotLoc.Child;

        [SerializeField, FoldoutGroup("Setup")]
        [Range(0, 30)]
        private int m_ChildCount;

        [FormerlySerializedAs("m_MainSlot")]
        [SerializeField]
        [FoldoutGroup("Settings")]
        [BoxGroup("Settings/Root Slot")]
        [InlineProperty(LabelWidth = 68)]
        [OnValueChanged(nameof(OnSettingsChanged), true)]
        private Slot m_RootSettings = new();

        [FormerlySerializedAs("m_BackgroundSlot")]
        [BoxGroup("Settings/Child Slot")]
        [InlineProperty]
        [SerializeField, FoldoutGroup("Settings"), ShowIf(nameof(m_IsNested), IsNested.Nested)]
        [OnValueChanged(nameof(OnSettingsChanged), true)]
        private SlotConfig m_ChildSetting = new();

        [SerializeField, FoldoutGroup("Runtime/FX"), OnValueChanged(nameof(OnSettingsChanged))] [InlineProperty(LabelWidth = 68)]
        private UIFxType m_FxForeground;
        [SerializeField, FoldoutGroup("Runtime/FX"), OnValueChanged(nameof(OnSettingsChanged))] [InlineProperty(LabelWidth = 68)]
        private UIFxType m_FxBackground;
        
        [SerializeField, FoldoutGroup("Runtime"), OnValueChanged(nameof(OnSettingsChanged))] [InlineProperty(LabelWidth = 68)]
        private bool m_AllowOverridenChildren = false;
        [SerializeField, FoldoutGroup("Runtime"), OnValueChanged(nameof(OnSettingsChanged))] [InlineProperty(LabelWidth = 68)]
        protected bool m_AllowMultipleSelection = false;
        [SerializeField, Min(2), ShowIf(nameof(m_AllowMultipleSelection)), FoldoutGroup("Runtime")] [InlineProperty(LabelWidth = 68)]
        private int m_MultiSelectLimit = 2;
        [SerializeField, FoldoutGroup("Runtime"), OnValueChanged(nameof(OnSettingsChanged))] [InlineProperty(LabelWidth = 68)]
        private bool m_AllowDeselection = false;


        // -----------------------------------------------------------
        // Public accessors
        // -----------------------------------------------------------

        public bool AllowMultipleSelection => m_AllowMultipleSelection;
        public bool AllowDeselection => m_AllowDeselection;

        public SlotConfig RootSettings => m_RootSettings;
        public SlotConfig ChildSlot => m_ChildSetting;
        public IsNested IsNested => m_IsNested;
        public float ContentPadding => m_Padding;
        public float BgContentPadding => m_Padding;
        public GraphicType GraphicType => m_RootSettings.Type;
        public GraphicType BgGraphicType => m_ChildSetting.Type;
        public bool AllowOverridenChildren => m_AllowOverridenChildren;
        public UIFxType FxForeground => m_FxForeground;
        public UIFxType FxBackground => m_FxBackground;

        // -----------------------------------------------------------
        // Children
        // -----------------------------------------------------------

        [SerializeField, CachedField(RefSearch.Children, includeInactive: true)]
        protected List<Graphix> m_SImages = new();

        [CachedField(addComponentIfMissing: true), SerializeField]
        protected Canvas m_Canvas;

        [CachedField(addComponentIfMissing: true), SerializeField]
        protected GraphicRaycaster m_GraphicRaycaster;

        private readonly HashSet<ISelectiveGraphix> m_SelectedImages = new();

        public Action<ISelectiveGraphix, bool> OnImage;
        public int SortingOrder => m_Canvas.sortingOrder;

        // -----------------------------------------------------------
        // Lifecycle
        // -----------------------------------------------------------

        protected override void OnAwake()
        {
            base.OnAwake();

            EM.AddListener<SGraphixSelectedEvent>(OnSImageSelected, Priority.Critical, this);
            EM.AddListener<SGraphixDeselectedEvent>(OnSImageDeselected, Priority.Critical, this);
            EM.AddListener<SGraphixCreatedEvent>(OnSImageSpawned, Priority.Critical, this);

            InitImages();
        }

        protected virtual void OnDestroy()
        {
            EM.RemoveListener<SGraphixSelectedEvent>(OnSImageSelected, this);
            EM.RemoveListener<SGraphixDeselectedEvent>(OnSImageDeselected, this);
            EM.RemoveListener<SGraphixCreatedEvent>(OnSImageSpawned, this);
        }

        // -----------------------------------------------------------
        // Settings push
        // -----------------------------------------------------------

        private void OnSettingsChanged()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                ResolveReferences();
            }
#endif

            AdjustImages();

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEditor.EditorUtility.SetDirty(this);
            }
#endif
        }

        [Button, FoldoutGroup("Setup")]
        public void Refresh()
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
                return;

            m_ChildCount = Mathf.Max(0, m_ChildCount);

            ResolveReferences();

            for (int i = m_SImages.Count - 1; i >= m_ChildCount; i--)
            {
                var image = m_SImages[i];

                if (image != null)
                    UnityEditor.Undo.DestroyObjectImmediate(image.gameObject);
            }

            ResolveReferences();

            for (int i = m_SImages.Count; i < m_ChildCount; i++)
            {
                Type childType = m_IsNested == IsNested.Nested
                    ? typeof(NestedSelectiveGraphix)
                    : typeof(SelectiveGraphix);

                var go = new GameObject($"SImage_{i}", typeof(RectTransform), childType);
                UnityEditor.Undo.RegisterCreatedObjectUndo(go, "Create Selective Graphix");

                go.transform.SetParent(transform, false);

                var rect = go.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                //m_SImages.Add(go.GetComponent<Graphix>());
            }

            ResolveReferences();
            AdjustImages();
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        private void AdjustImages()
        {
            for (int i = 0; i < m_SImages.Count; i++)
            {
                var image = m_SImages[i];
                if (image == null)
                    continue;

                var selective = image as ISelectiveGraphix;
                if (selective == null)
                    continue;

#if UNITY_EDITOR
                image.name = $"SImage_{i}";
                image.transform.SetSiblingIndex(i);
#endif

                selective.EditorBind(this);

#if UNITY_EDITOR
                if (!Application.isPlaying)
                    UnityEditor.EditorUtility.SetDirty(image);
#endif
            }
        }

        // -----------------------------------------------------------
        // Content helpers
        // -----------------------------------------------------------

        public void SetContent(List<Sprite> content, bool preserveAspect, Sprite back = null)
        {
            for (int i = 0; i < m_SImages.Count; i++)
            {
                if (i >= content.Count)
                {
                    m_SImages[i].gameObject.SetActive(false);
                    continue;
                }

                var image = m_SImages[i];
                image.gameObject.SetActive(true);

                if (image is NestedGraphix or NestedSelectiveGraphix)
                {
                    var nested = image as NestedGraphix;
                    nested.SetContent(
                        content[i],
                        preserveAspect,
                        false,
                        m_MainContentSlot
                    );

                    if (back != null)
                    {
                        nested.SetContent(
                            back,
                            preserveAspect,
                            false,
                            m_MainContentSlot == SlotLoc.Child ? SlotLoc.This : SlotLoc.Child
                        );
                    }
                }
                else
                {
                    image.SetContent(content[i], preserveAspect, false);
                }
            }
        }
        // public void SetContent(List<Texture> content, Texture back = null)
        // {
        //     for (int i = 0; i < m_SImages.Count; i++)
        //     {
        //         if (i >= content.Count)
        //         {
        //             m_SImages[i].gameObject.SetActive(false);
        //         }
        //         else
        //         {
        //             m_SImages[i].gameObject.SetActive(true);
        //             m_SImages[i].SetTargetContent(content[i]);
        //
        //             if (back != null && m_SImages[i] is NestedGraphix nested)
        //                 nested.SetSecondSlotContent(back);
        //         }
        //     }
        // }

        public List<Graphix> GetImagesRandomOrder()
        {
            var shuffled = new List<Graphix>(m_SImages);
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
            }

            return shuffled;
        }

        private void InitImages()
        {
            for (int i = 0; i < m_SImages.Count; i++)
            {
                var selective = m_SImages[i] as ISelectiveGraphix;
                selective?.SetGroup(i, this);
            }
        }

        public void DeselectAll()
        {
            foreach (var image in m_SImages)
            {
                if (image is ISelectiveGraphix selective)
                    selective.TryDeselect();
            }
        }

        public void UnlockAll()
        {
            foreach (var image in m_SImages)
            {
                if (image is ISelectiveGraphix selective)
                    selective.UnLock();
            }
        }

        public void LockAll(bool disableVis)
        {
            foreach (var image in m_SImages)
            {
                if (image is ISelectiveGraphix selective)
                    selective.Lock(disableVis);
            }
        }

        // -----------------------------------------------------------
        // Event handlers
        // -----------------------------------------------------------

        protected virtual void OnSImageSpawned(SGraphixCreatedEvent evt)
        {
        }

        protected virtual void OnSImageSelected(SGraphixSelectedEvent evt)
        {
            var sImage = evt.SelectiveGraphix;
            if (m_SelectedImages.Contains(sImage))
            {
                Debug.LogWarning($"Image {sImage.name} is already selected in group {this.name}");
                return;
            }

            if (m_AllowMultipleSelection && m_SelectedImages.Count >= m_MultiSelectLimit)
            {
                sImage.TryDeselect();
                return;
            }

            m_SelectedImages.Add(sImage);
            OnImage?.Invoke(sImage, true);
        }

        protected virtual void OnSImageDeselected(SGraphixDeselectedEvent evt)
        {
            var sImage = evt.SelectiveGraphix;
            if (!m_SelectedImages.Remove(sImage))
                return;

            OnImage?.Invoke(sImage, false);
        }
    }
}