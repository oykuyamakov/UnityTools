using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Cadi.Scripts.UI.Helpers
{
    public class UISlicedSpriteAnimator : MonoBehaviour
    {
        [SerializeField]
        private Sprite[] m_Sprites;

        [SerializeField]
        private float m_Delay = 0.25f;

        [SerializeField]
        private bool m_Loop = true;

        [SerializeField]
        private Image[] m_Images;

        private Coroutine m_Coroutine;

        private void OnEnable()
        {
            StartAnim();
        }

        private void OnDisable()
        {
            StopAnim(true);
        }

        private void Build()
        {
            m_Images = new Image[m_Sprites.Length];

            for (int i = 0; i < m_Sprites.Length; i++)
            {
                Sprite sprite = m_Sprites[i];

                GameObject go = new GameObject(sprite.name, typeof(RectTransform), typeof(CanvasRenderer),
                    typeof(Image));
                go.transform.SetParent(transform, false);

                Image image = go.GetComponent<Image>();
                image.sprite = sprite;
                image.enabled = false;
                image.SetNativeSize();

                RectTransform rt = go.GetComponent<RectTransform>();
                ApplySpriteOriginalPosition(rt, sprite);

                m_Images[i] = image;
            }
        }

        private void ApplySpriteOriginalPosition(RectTransform rt, Sprite sprite)
        {
            Rect rect = sprite.rect;
            Texture texture = sprite.texture;

            float x = rect.x + rect.width * 0.5f - texture.width * 0.5f;
            float y = rect.y + rect.height * 0.5f - texture.height * 0.5f;

            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            rt.anchoredPosition = new Vector2(x, y);
        }

        private void StartAnim()
        {
            m_Loop = true;
            m_Coroutine = StartCoroutine(RevealRoutine());
        }

        private void StopAnim(bool force)
        {
            if (force)
            {
                foreach (var t in m_Images)
                    t.enabled = false;
            }

            if (m_Coroutine != null)
                StopCoroutine(m_Coroutine);
        }

        private IEnumerator RevealRoutine()
        {
            while (m_Loop)
            {
                foreach (var t in m_Images)
                    t.enabled = false;

                foreach (var t in m_Images)
                {
                    yield return new WaitForSeconds(m_Delay);
                    t.enabled = true;
                }

                yield return new WaitForSeconds(m_Delay);
            }

            m_Coroutine = null;
        }
    }
}