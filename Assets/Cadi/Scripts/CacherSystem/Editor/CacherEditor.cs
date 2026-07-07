#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector.Editor;
#endif
using UnityEditor;
using UnityEngine;

namespace Cadi.Scripts.CacherSystem.Editor
{
#if ODIN_INSPECTOR
    // -----------------------------------------------------------------------
    // Attribute processor: automatically hides [CachedField] properties from
    // Odin's main tree so they only appear in the custom cached foldout.
    // -----------------------------------------------------------------------
    public sealed class CachedFieldAttributeProcessor : OdinAttributeProcessor<CacherMonoBehaviour>
    {
        public override void ProcessChildMemberAttributes(
            InspectorProperty parentProperty,
            MemberInfo member,
            List<Attribute> attributes)
        {
            if (member.GetCustomAttribute<CachedFieldAttribute>() != null)
                attributes.Add(new HideInInspector());
        }
    }

    // -----------------------------------------------------------------------
    // Custom editor: lets Odin draw non-cached fields (with full attribute
    // support), then appends the cached references foldout as IMGUI.
    // -----------------------------------------------------------------------
    [CustomEditor(typeof(CacherMonoBehaviour), true)]
    [CanEditMultipleObjects]
    public sealed class CacherEditor : OdinEditor
    {
        private sealed class CachedFieldInfo
        {
            public FieldInfo Field;
            public CachedFieldAttribute Attr;
        }

        private static readonly Dictionary<Type, List<CachedFieldInfo>> s_CachedFieldsByType = new();

        private static readonly GUIContent s_CachedHeader = new("Cached References");
        private static readonly GUIContent s_ResolveNow = new("Resolve Now");
        private static readonly GUIContent s_ShowMeta = new("Show meta");

        private bool m_ShowMeta;

        protected override void DrawTree()
        {
            // Odin draws everything; cached fields are already hidden
            // by CachedFieldAttributeProcessor above.
            base.DrawTree();

            EditorGUILayout.Space(8);
            DrawCachedSection();
            GUILayout.Space(10);
        }

        private void DrawCachedSection()
        {
            var t0 = (CacherMonoBehaviour)target;
            if (t0 == null)
                return;

            Type type = t0.GetType();
            List<CachedFieldInfo> cached = GetCachedFields(type);

            if (cached.Count == 0)
                return;

            string foldoutKey = $"CacherMonoBehaviourEditor_Foldout_{type.FullName}";
            bool expanded = SessionState.GetBool(foldoutKey, true);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    expanded = EditorGUILayout.Foldout(expanded, s_CachedHeader, true);

                    GUILayout.FlexibleSpace();

                    m_ShowMeta = GUILayout.Toggle(m_ShowMeta, s_ShowMeta, EditorStyles.miniButton);

                    using (new EditorGUI.DisabledScope(targets.Length != 1 &&
                                                       serializedObject.isEditingMultipleObjects))
                    {
                        if (GUILayout.Button(s_ResolveNow, EditorStyles.miniButton))
                        {
                            ResolveSelected();
                        }
                    }
                }

                SessionState.SetBool(foldoutKey, expanded);

                if (!expanded)
                    return;

                EditorGUILayout.Space(4);

                DrawStatusLine();

                var warnings = GetCachedFieldWarnings(type);
                if (warnings.Count > 0)
                {
                    string msg = string.Join("\n• ", warnings);
                    EditorGUILayout.HelpBox("CachedField configuration warnings:\n• " + msg, MessageType.Warning);

                    Debug.LogWarning(
                        $"{type.Name} has {warnings.Count} CachedField configuration warnings. See Inspector for details.");

                    EditorGUILayout.Space(4);
                }

                EditorGUILayout.Space(4);

                using (new EditorGUI.DisabledScope(true))
                {
                    DrawCachedFieldsReadOnly(cached);
                }
            }
        }

        private void DrawStatusLine()
        {
            bool anyErrors = false;
            bool anyUnresolved = false;

            for (int i = 0; i < targets.Length; i++)
            {
                var c = targets[i] as CacherMonoBehaviour;
                if (c == null)
                    continue;

                anyErrors |= c.LastResolveHadErrors;
                anyUnresolved |= !c.IsResolved;
            }

            string status =
                anyErrors ? "Status: Missing required references (see Console)." :
                anyUnresolved ? "Status: Not resolved yet." :
                "Status: OK.";

            var style = anyErrors ? EditorStyles.boldLabel : EditorStyles.miniLabel;
            EditorGUILayout.LabelField(status, style);
        }

        private void ResolveSelected()
        {
            Undo.RecordObjects(targets, "Resolve Cached References");

            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] is not CacherMonoBehaviour c || c == null)
                    continue;

                c.ResolveReferences();
                EditorUtility.SetDirty(c);
            }

            serializedObject.Update();
            Repaint();
        }

        private void DrawCachedFieldsReadOnly(List<CachedFieldInfo> cached)
        {
            serializedObject.Update();

            for (int i = 0; i < cached.Count; i++)
            {
                CachedFieldInfo info = cached[i];

                SerializedProperty prop = serializedObject.FindProperty(info.Field.Name);

                if (prop != null)
                {
                    using (new EditorGUILayout.VerticalScope())
                    {
                        EditorGUILayout.PropertyField(prop, includeChildren: true);

                        if (m_ShowMeta)
                            DrawMeta(info.Attr);
                    }

                    EditorGUILayout.Space(2);
                    continue;
                }

                object value = info.Field.GetValue(target);
                DrawFallbackValue(info.Field, value);

                if (m_ShowMeta)
                    DrawMeta(info.Attr);

                EditorGUILayout.Space(2);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private static void DrawMeta(CachedFieldAttribute attr)
        {
            EditorGUILayout.LabelField(
                $"Search: {attr.Search} | IncludeInactive: {attr.IncludeInactive} | Required: {attr.Required} | AddIfMissing: {attr.AddComponentIfMissing}" +
                (string.IsNullOrEmpty(attr.AddComponentIfMissingBoundToBoolField)
                    ? ""
                    : $" | AddIfMissingBool: {attr.AddComponentIfMissingBoundToBoolField}"),
                EditorStyles.miniLabel);
        }

        private static void DrawFallbackValue(FieldInfo field, object value)
        {
            string label = ObjectNames.NicifyVariableName(field.Name);

            if (value is UnityEngine.Object uo)
            {
                EditorGUILayout.ObjectField(label, uo, field.FieldType, allowSceneObjects: true);
                return;
            }

            if (value is IList list)
            {
                EditorGUILayout.LabelField(label, $"{field.FieldType.Name} (Count: {list.Count})",
                    EditorStyles.miniLabel);
                return;
            }

            if (value is Array arr)
            {
                EditorGUILayout.LabelField(label, $"{field.FieldType.Name} (Length: {arr.Length})",
                    EditorStyles.miniLabel);
                return;
            }

            EditorGUILayout.LabelField(label, value != null ? value.ToString() : "null");
        }

        private static List<CachedFieldInfo> GetCachedFields(Type type)
        {
            if (s_CachedFieldsByType.TryGetValue(type, out var cached))
                return cached;

            var list = new List<CachedFieldInfo>(16);

            const BindingFlags flags =
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.DeclaredOnly;

            Type cur = type;

            while (cur != null && cur != typeof(MonoBehaviour))
            {
                FieldInfo[] fields = cur.GetFields(flags);

                for (int i = 0; i < fields.Length; i++)
                {
                    FieldInfo f = fields[i];

                    var attr = f.GetCustomAttribute<CachedFieldAttribute>();
                    if (attr == null)
                        continue;

                    list.Add(new CachedFieldInfo
                    {
                        Field = f,
                        Attr = attr
                    });
                }

                cur = cur.BaseType;
            }

            list.Sort((a, b) => string.CompareOrdinal(a.Field.Name, b.Field.Name));

            s_CachedFieldsByType[type] = list;
            return list;
        }

        private static readonly Dictionary<Type, List<string>> s_WarningCacheByType = new();

        private static List<string> GetCachedFieldWarnings(Type inspectedRuntimeType)
        {
            if (s_WarningCacheByType.TryGetValue(inspectedRuntimeType, out var cached))
                return cached;

            var warnings = new List<string>();

            var cachedFields = GetCachedFields(inspectedRuntimeType);

            for (int i = 0; i < cachedFields.Count; i++)
            {
                FieldInfo f = cachedFields[i].Field;

                bool isSerializedByUnity =
                    f.IsPublic ||
                    f.GetCustomAttribute<SerializeField>() != null;

                if (!isSerializedByUnity)
                {
                    warnings.Add(
                        $"'{f.DeclaringType.Name}.{f.Name}' is [CachedField] but not serialized. Add [SerializeField] (or make it public).");
                }

                Type declaring = f.DeclaringType;
                if (f.IsPrivate && declaring != null && !declaring.IsSealed)
                {
                    warnings.Add(
                        $"'{declaring.Name}.{f.Name}' is private and [CachedField] on a non-sealed class. Derived types will not resolve this field with the current resolver. Make it protected (recommended) or seal '{declaring.Name}', or update resolver to walk base types.");
                }
            }

            s_WarningCacheByType[inspectedRuntimeType] = warnings;
            return warnings;
        }
    }
#endif

}
#endif