using UnityEngine;

namespace CadiKazani.Scripts.Utility.GameObjectHelpers
{
    public class TransformPerserver : MonoBehaviour
    {
        [System.Flags]
        public enum PerserveMode
        {
            None = 0,
            Position = 1 << 0,
            Rotation = 1 << 1,
            Scale = 1 << 2,
        }

        [SerializeField]
        private PerserveMode m_PerserveMode = PerserveMode.None;

        [SerializeField]
        private bool m_RunOnUpdate;

        [SerializeField]
        private bool m_StoreOnValidate = true;

        [SerializeField]
        private bool m_StoreOnAwake = false;

        [SerializeField]
        private bool m_PreserveAgainstParentChanges = false;

        private Vector3 m_InitialPosition;
        private Quaternion m_InitialRotation;
        private Vector3 m_InitialScale;

        // Parent tracking (only used when m_PreserveAgainstParentChanges is enabled)
        private Transform m_LastParent;
        private Vector3 m_InitialWorldPosition;
        private Quaternion m_InitialWorldRotation;
        private Vector3 m_InitialLossyScale;

        private void OnValidate()
        {
            if (!m_StoreOnValidate)
                return;

            StoreValues();
        }

        private void Awake()
        {
            if (!m_StoreOnAwake)
                return;

            StoreValues();
        }

        private void LateUpdate()
        {
            if (!m_RunOnUpdate)
                return;

            PreserveTransform();
        }

        private void StoreValues()
        {
            m_InitialPosition = transform.localPosition;
            m_InitialRotation = transform.localRotation;
            m_InitialScale = transform.localScale;

            m_LastParent = transform.parent;

            m_InitialWorldPosition = transform.position;
            m_InitialWorldRotation = transform.rotation;
            m_InitialLossyScale = transform.lossyScale;
        }

        private void PreserveTransform()
        {
            if (m_PreserveAgainstParentChanges)
            {
                PreserveAgainstParentChanges();
                return;
            }

            PreserveLocal();
        }

        private void PreserveLocal()
        {
            if ((m_PerserveMode & PerserveMode.Position) != 0)
            {
                transform.localPosition = m_InitialPosition;
            }

            if ((m_PerserveMode & PerserveMode.Rotation) != 0)
            {
                transform.localRotation = m_InitialRotation;
            }

            if ((m_PerserveMode & PerserveMode.Scale) != 0)
            {
                transform.localScale = m_InitialScale;
            }
        }

        private void PreserveAgainstParentChanges()
        {
            // If parent changed, re-store baselines so we don't "teleport" due to stale data.
            if (transform.parent != m_LastParent)
            {
                StoreValues();
                return;
            }

            // Preserve world-space position/rotation so parent TRS changes don't affect this object.
            if ((m_PerserveMode & PerserveMode.Position) != 0)
            {
                transform.position = m_InitialWorldPosition;
            }

            if ((m_PerserveMode & PerserveMode.Rotation) != 0)
            {
                transform.rotation = m_InitialWorldRotation;
            }

            // Preserve world-space scale (approx) by compensating for parent's lossyScale.
            if ((m_PerserveMode & PerserveMode.Scale) != 0)
            {
                var parent = transform.parent;
                if (parent == null)
                {
                    transform.localScale = m_InitialScale;
                    return;
                }

                Vector3 parentLossy = parent.lossyScale;
                Vector3 desiredLossy = m_InitialLossyScale;

                transform.localScale = new Vector3(
                    SafeDiv(desiredLossy.x, parentLossy.x),
                    SafeDiv(desiredLossy.y, parentLossy.y),
                    SafeDiv(desiredLossy.z, parentLossy.z)
                );
            }
        }

        private float SafeDiv(float a, float b)
        {
            if (Mathf.Abs(b) < 0.000001f)
                return 0f;

            return a / b;
        }
    }
}