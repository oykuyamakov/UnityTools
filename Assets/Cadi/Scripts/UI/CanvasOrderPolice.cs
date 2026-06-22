using System.Collections.Generic;
using System.Linq;
using Cadi.Scripts.CacherSystem;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;
using UnityEngine.Serialization;

namespace Cadi.Scripts.UI
{
    [RequireComponent(typeof(CanvasGroup))][RequireComponent(typeof(Canvas))][DisallowMultipleComponent]
    public class CanvasOrderPolice : CacherMonoBehaviour
    {
        [CachedField,SerializeField]
        protected Canvas m_SelfCanvas;
        
        [CachedField,SerializeField]
        protected Canvas m_CanvasGroup;
        
        [SerializeField]
        [CachedField(RefSearch.Children, includeInactive: true, required: false)]
        protected List<Canvas> m_CanvasRefs = new();
        
        [ListDrawerSettings(
            DraggableItems = true,
            HideAddButton = true,
            HideRemoveButton = true,
            ListElementLabelName = nameof(CanvasOrderItem.Label)
        )][OnValueChanged(nameof(SortListByCurrentSortingOrder))][SerializeField]
        protected List<CanvasOrderItem> m_Canvases = new();

        public IReadOnlyList<Canvas> CanvasRefs => m_CanvasRefs;

        public int GetHighestOrder()
        {
            return m_Canvases.Last().SortingOrder;
        }

        public override void ResolveReferences()
        {
            base.ResolveReferences();
            SortListByCurrentSortingOrder();
        }

        public void SortListByCurrentSortingOrder()
        {
            var orderedCanvases = m_CanvasRefs
                .Where(c => c != null)
                .OrderBy(c => c.sortingOrder)
                .ThenBy(c => c.transform.GetSiblingIndex())
                .ToList();

            if (m_Canvases.IsNullOrEmpty() || m_Canvases.Count != orderedCanvases.Count)
            {
                m_Canvases = new List<CanvasOrderItem>();
                foreach (var canvas in orderedCanvases)
                {
                    var newOrdered = new CanvasOrderItem(canvas);
                    m_Canvases.Add(newOrdered);
                }
            }
            else
            {
                m_Canvases.RemoveAll(c => c == null || c.Label == "Missing Canvas" || !m_CanvasRefs.Contains(c.Canvas));

                for (int i = 1; i < m_Canvases.Count; i++)
                {
                    if (m_Canvases[i].SortingOrder < m_Canvases[i - 1].SortingOrder)
                        m_Canvases[i].SetSortingOrder(m_Canvases[i - 1].SortingOrder + 1);
                }
            }
        }
    }
    
    [System.Serializable]
    public class CanvasOrderItem
    {
        public CanvasOrderItem(Canvas canvas)
        {
            m_Canvas = canvas;
        }
        
        [SerializeField][InlineProperty][ReadOnly]
        private Canvas m_Canvas;
        
        public Canvas Canvas => m_Canvas;

        [ShowInInspector, ReadOnly, InlineProperty]
        public int SortingOrder => m_Canvas != null ? m_Canvas.sortingOrder : 0;

        public void SetSortingOrder(int order)
        {
            if (m_Canvas != null)
                m_Canvas.sortingOrder = order;
        }

        public string Label => m_Canvas != null
            ? $"{SortingOrder} - {m_Canvas.name}"
            : "Missing Canvas";
    }
}