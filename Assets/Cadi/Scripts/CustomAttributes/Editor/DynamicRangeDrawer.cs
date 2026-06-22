#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Cadi.Scripts.CustomAttributes.Editor
{
    [CustomPropertyDrawer(typeof(DynamicRangeAttribute))]
    public class DynamicRangeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var range = (DynamicRangeAttribute)attribute;

            if (!TryResolveRange(property, range, out float min, out float max))
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            if (min > max)
            {
                (min, max) = (max, min);
            }

            if (property.propertyType == SerializedPropertyType.Float)
            {
                float v = property.floatValue;
                v = EditorGUI.Slider(position, label, v, min, max);
                property.floatValue = Mathf.Clamp(v, min, max);
                return;
            }

            if (property.propertyType == SerializedPropertyType.Integer)
            {
                int imin = Mathf.RoundToInt(min);
                int imax = Mathf.RoundToInt(max);

                if (imin > imax)
                {
                    (imin, imax) = (imax, imin);
                }

                int v = property.intValue;
                v = EditorGUI.IntSlider(position, label, v, imin, imax);
                property.intValue = Mathf.Clamp(v, imin, imax);
                return;
            }

            EditorGUI.PropertyField(position, property, label);
        }

        private static bool TryResolveRange(SerializedProperty target, DynamicRangeAttribute range, out float min, out float max)
        {
            min = 0f;
            max = 1f;

            bool hasMin = TryResolveBound(target, range.MinConst, range.MinField, out min);
            bool hasMax = TryResolveBound(target, range.MaxConst, range.MaxField, out max);

            return hasMin && hasMax;
        }

        private static bool TryResolveBound(SerializedProperty target, float? constValue, string fieldName, out float value)
        {
            if (constValue.HasValue)
            {
                value = constValue.Value;
                return true;
            }

            if (!string.IsNullOrEmpty(fieldName))
            {
                var prop = FindRelativeProperty(target, fieldName);
                if (prop != null && TryGetNumericValue(prop, out value))
                {
                    return true;
                }
            }

            value = 0f;
            return false;
        }

        private static bool TryGetNumericValue(SerializedProperty prop, out float value)
        {
            if (prop.propertyType == SerializedPropertyType.Float)
            {
                value = prop.floatValue;
                return true;
            }

            if (prop.propertyType == SerializedPropertyType.Integer)
            {
                value = prop.intValue;
                return true;
            }

            value = 0f;
            return false;
        }

        /// <summary>
        /// Finds a sibling property next to 'property' inside the same serialized container,
        /// including when property lives inside arrays/lists.
        /// </summary>
        private static SerializedProperty FindRelativeProperty(SerializedProperty property, string fieldName)
        {
            // Example paths:
            //  "jointAnchors.Array.data[0].StepDist"
            //  "someStruct.DistanceFromRootUp"
            // We want to replace the last segment with fieldName.

            string path = property.propertyPath;

            int lastDot = path.LastIndexOf('.');
            if (lastDot >= 0)
            {
                path = path.Substring(0, lastDot + 1) + fieldName;
            }
            else
            {
                // Root-level field
                path = fieldName;
            }

            return property.serializedObject.FindProperty(path);
        }
    }
}
#endif
