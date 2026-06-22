using _App.Scripts.Utility.UI;
using Cadi.Scripts.CacherSystem;
using Cadi.Scripts.UI.Extensions;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Cadi.Scripts.UI
{
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(CanvasGroup))]
    public class RootCanvas : CacherMonoBehaviour
    {
        [SerializeField]
        protected bool m_OnUICam = true;

        [SerializeField]
        [ShowIf(nameof(m_OnUICam))]
        protected int m_CanvasSortingOrder = 10;

        [CachedField, SerializeField]
        protected Canvas m_Canvas;

        [CachedField(addComponentIfMissing: true), SerializeField]
        protected CanvasGroup m_CanvasGroup;


        public override void ResolveReferences()
        {
            base.ResolveReferences();

            //only perform if acitive in scene
            if (!gameObject.scene.IsValid())
                return;

            if (UICamera.Instance == null)
                return;

            Adjust();
        }

        protected void Awake()
        {
            Adjust();
        }

        protected virtual void Adjust()
        {
            if (m_OnUICam)
            {
                m_Canvas.AdjustFor(RenderMode.ScreenSpaceCamera, m_CanvasSortingOrder, UICamera.Instance.Get());
            }
        }
    }
}