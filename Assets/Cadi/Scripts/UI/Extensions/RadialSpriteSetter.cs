using System.Collections;
using Cadi.Scripts.CacherSystem;
using UnityEngine;
using UnityEngine.UI;

namespace Cadi.Scripts.UI.Extensions
{
    [RequireComponent(typeof(Image))]
    public sealed class RadialSpriteSetter : CacherMonoBehaviour
    {
        [SerializeField, CachedField]
        private Image m_Image;
        
        private Coroutine m_Coroutine;

        public void Set(Sprite sprite, float dur = 1f)
        {
            
            if (m_Coroutine != null)
            {
                StopCoroutine(m_Coroutine);
            }
            
            m_Image.fillMethod = Image.FillMethod.Radial360;
            m_Image.fillAmount = 0f;
            m_Image.sprite = sprite;
            
            
            m_Coroutine = StartCoroutine(Fill(dur));
        }

        private IEnumerator Fill(float dur)
        {
            var elapsed = 0f;
            while (elapsed < dur)
            {
                m_Image.fillAmount = elapsed * 10f;
                elapsed += Time.deltaTime;
                yield return null;
            }
            m_Image.fillAmount = 1f;
        }
    }
}
