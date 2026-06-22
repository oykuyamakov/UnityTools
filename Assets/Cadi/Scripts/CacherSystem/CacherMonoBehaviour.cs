using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Cadi.Scripts.CacherSystem
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public sealed class CachedFieldAttribute : PropertyAttribute
    {
        public readonly RefSearch Search;
        public readonly bool IncludeInactive;
        public readonly bool Required;
        public readonly bool AddComponentIfMissing;
        public readonly string AddComponentIfMissingBoundToBoolField;

        public CachedFieldAttribute(
            RefSearch search = RefSearch.Self,
            bool includeInactive = true,
            bool required = true, bool addComponentIfMissing = false, string addIfMissingBoolField = null)
        {
            Search = search;
            IncludeInactive = includeInactive;
            Required = required;
            AddComponentIfMissing = addComponentIfMissing;
            AddComponentIfMissingBoundToBoolField = addIfMissingBoolField;
        }
    }

    public interface IAutoReferenceResolver
    {
        bool IsResolved { get; }
        void ResolveReferences();

    }

    public enum RefSearch
    {
        Self,
        Parent,
        Children
    }
    

    public abstract class CacherMonoBehaviour : MonoBehaviour, IAutoReferenceResolver
    {
        [SerializeField, HideInInspector]
        private bool m_IsResolved;

        private bool m_LastResolveHadErrors;
        
        public bool IsResolved => m_IsResolved;
        public bool LastResolveHadErrors => m_LastResolveHadErrors;

        private static readonly Dictionary<Type, List<FieldBinding>> s_BindingsCache = new();

        // Clear the cache on every domain reload so stale FieldInfo instances from a
        // previous assembly are not reused after script recompilation in the Editor.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ClearBindingsCache() => s_BindingsCache.Clear();

        private sealed class FieldBinding
        {
            public FieldInfo Field;
            public CachedFieldAttribute Attr;
            public bool IsArray;
            public bool IsList;
            public Type ElementType;
        }

        private void Awake()
        {
            OnAwake();
        }

        protected virtual void OnAwake()
        {
            if (m_IsResolved && !HasMissingRequiredRefs()) 
                return;
            
            Debug.LogWarning($"{name} ({GetType().Name}) has missing references - Resolving references", this);
                
            ResolveReferences();
        }
        
        public virtual void ResolveReferences()
        {
            m_LastResolveHadErrors = false;
            ResolveAllAttributedFields();
            m_IsResolved = !m_LastResolveHadErrors;
        }

#if UNITY_EDITOR

        //Reset is only called in Edit mode. If you add components at runtime, Reset won't be called.
        private  void Reset()
        {
            ResolveReferences();

            OnReset();
        }

        protected virtual void OnReset()
        {
            
        }

        private void OnValidate()
        {
            ResolveReferences();
            
            OnValidated();
        }

        protected virtual void OnValidated()
        {
          
        }
#endif

        private void ResolveAllAttributedFields()
        {
            List<FieldBinding> bindings = GetBindingsForType(GetType());

            foreach (var b in bindings)
            {
                if (b.IsArray)
                {
                    Array arr = ResolveArray(b.ElementType, b.Attr);
                    b.Field.SetValue(this, arr);
                    ValidateRequired(b, arr);
                    continue;
                }

                if (b.IsList)
                {
                    object listObj = ResolveList(b.Field.FieldType, b.ElementType, b.Attr);
                    b.Field.SetValue(this, listObj);
                    ValidateRequired(b, listObj as IList);
                    continue;
                }

                UnityEngine.Object resolved = ResolveSingle(b.Field.FieldType, b.Attr);
                b.Field.SetValue(this, resolved);
                ValidateRequired(b, resolved);
            }
        }

        private bool HasMissingRequiredRefs()
        {
            List<FieldBinding> bindings = GetBindingsForType(GetType());

            foreach (var b in bindings)
            {
                if (!b.Attr.Required)
                {
                    continue;
                }

                object value = b.Field.GetValue(this);

                if (b.IsArray)
                {
                    if (value is not Array arr || arr.Length == 0)
                    {
                        return true;
                    }

                    continue;
                }

                if (b.IsList)
                {
                    if (value is not IList list || list.Count == 0)
                    {
                        return true;
                    }

                    continue;
                }

                UnityEngine.Object obj = value as UnityEngine.Object;
                if (obj == null)
                {
                    return true;
                }
            }

            return false;
        }
        
        private bool ShouldAddComponentIfMissing(CachedFieldAttribute attr)
        {
            if (attr.AddComponentIfMissing)
                return true;

            if (string.IsNullOrEmpty(attr.AddComponentIfMissingBoundToBoolField))
                return false;

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            FieldInfo f = FindFieldInHierarchy(GetType(), attr.AddComponentIfMissingBoundToBoolField, flags);
            if (f != null && f.FieldType == typeof(bool))
                return (bool)f.GetValue(this);
            
            //the bool field to add if missing is not found or not bool, log error and return false
#if UNITY_EDITOR
            Debug.LogError(
                $"{name} ({GetType().Name}) CachedField AddIfMissingBoolField '{attr.AddComponentIfMissingBoundToBoolField}' not found or not bool.",
                this);
#endif
            return false;

        }

        private static FieldInfo FindFieldInHierarchy(Type type, string fieldName, BindingFlags flags)
        {
            var cur = type;
            while (cur != null && cur != typeof(MonoBehaviour))
            {
                var f = cur.GetField(fieldName, flags | BindingFlags.DeclaredOnly);
                if (f != null) return f;
                cur = cur.BaseType;
            }
            return null;
        }

        private UnityEngine.Object ResolveSingle(Type fieldType, CachedFieldAttribute attr)
        {
            if (!typeof(Component).IsAssignableFrom(fieldType))
            {
                return null;
            }

            if (attr.Search == RefSearch.Self)
            {
                Component found = GetComponent(fieldType);
                if (found == null && ShouldAddComponentIfMissing(attr))
                    found = gameObject.AddComponent(fieldType);
                return found;
            }

            // AddComponent is meaningless for Parent/Children — warn if misconfigured.
            if (ShouldAddComponentIfMissing(attr))
            {
                Debug.LogWarning(
                    $"{name} ({GetType().Name}) AddComponentIfMissing is only supported for RefSearch.Self" +
                    $" (field type: {fieldType.Name}).",
                    this);
            }

            if (attr.Search == RefSearch.Parent)
                return GetComponentInParent(fieldType, attr.IncludeInactive);

            return GetComponentInChildren(fieldType, attr.IncludeInactive);
        }

        private Component[] ResolveMany(Type elementType, CachedFieldAttribute attr)
        {
            return attr.Search switch
            {
                RefSearch.Self => GetComponents(elementType),
                RefSearch.Parent => GetComponentsInParent(elementType, attr.IncludeInactive),
                _ => GetComponentsInChildren(elementType, attr.IncludeInactive)
            };
        }

        private Array ResolveArray(Type elementType, CachedFieldAttribute attr)
        {
            if (!typeof(Component).IsAssignableFrom(elementType))
            {
                return Array.CreateInstance(elementType, 0);
            }

            Component[] comps = ResolveMany(elementType, attr);
            Array arr = Array.CreateInstance(elementType, comps.Length);

            for (int i = 0; i < comps.Length; i++)
            {
                arr.SetValue(comps[i], i);
            }

            return arr;
        }

        private object ResolveList(Type listType, Type elementType, CachedFieldAttribute attr)
        {
            object listObj = Activator.CreateInstance(listType);
            IList list = listObj as IList;

            if (list == null || !typeof(Component).IsAssignableFrom(elementType))
            {
                return listObj;
            }

            Component[] comps = ResolveMany(elementType, attr);

            foreach (var t in comps)
            {
                list.Add(t);
            }

            return listObj;
        }

        private void ValidateRequired(FieldBinding b, UnityEngine.Object resolved)
        {
            if (!b.Attr.Required)
            {
                return;
            }

            if (resolved != null)
            {
                return;
            }

            m_LastResolveHadErrors = true;
            Debug.LogWarning($"{name} ({GetType().Name}) missing required ref: {b.Field.Name}", this);
        }

        private void ValidateRequired(FieldBinding b, Array arr)
        {
            if (!b.Attr.Required)
            {
                return;
            }

            if (arr != null && arr.Length > 0)
            {
                return;
            }

            m_LastResolveHadErrors = true;
            Debug.LogWarning($"{name} ({GetType().Name}) missing required ref array: {b.Field.Name}", this);
        }

        private void ValidateRequired(FieldBinding b, IList list)
        {
            if (!b.Attr.Required)
            {
                return;
            }

            if (list != null && list.Count > 0)
            {
                return;
            }

            m_LastResolveHadErrors = true;
            Debug.LogWarning($"{name} ({GetType().Name}) missing required ref list: {b.Field.Name}", this);
        }

        private static List<FieldBinding> GetBindingsForType(Type type)
        {
            if (s_BindingsCache.TryGetValue(type, out var cached))
            {
                return cached;
            }

            var bindings = new List<FieldBinding>(16);
            var seenFields = new HashSet<string>();

            // Use DeclaredOnly and walk up the hierarchy so that private fields
            // declared on base classes are also discovered. Without this,
            // GetFields(NonPublic) only returns private fields on the exact type.
            const BindingFlags declaredFlags =
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.DeclaredOnly;

            Type cursor = type;
            while (cursor != null && cursor != typeof(CacherMonoBehaviour))
            {
                foreach (var f in cursor.GetFields(declaredFlags))
                {
                    // seenFields prevents processing a field twice when a subclass
                    // hides a base-class field with the same name.
                    if (!seenFields.Add(f.Name))
                        continue;

                    CachedFieldAttribute attr = f.GetCustomAttribute<CachedFieldAttribute>();
                    if (attr == null)
                        continue;

                    var b = new FieldBinding();
                    b.Field = f;
                    b.Attr = attr;

                    Type ft = f.FieldType;

                    if (ft.IsArray)
                    {
                        b.IsArray = true;
                        b.ElementType = ft.GetElementType();
                    }
                    else if (IsListType(ft, out var elementType))
                    {
                        b.IsList = true;
                        b.ElementType = elementType;
                    }

                    bindings.Add(b);
                }

                cursor = cursor.BaseType;
            }

            s_BindingsCache[type] = bindings;
            return bindings;
        }

        private static bool IsListType(Type t, out Type elementType)
        {
            elementType = null;

            if (!t.IsGenericType)
            {
                return false;
            }

            if (t.GetGenericTypeDefinition() != typeof(List<>))
            {
                return false;
            }

            elementType = t.GetGenericArguments()[0];
            return true;
        }
    }
}