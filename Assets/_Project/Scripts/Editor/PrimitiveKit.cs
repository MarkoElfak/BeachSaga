using BeachTycoon.Gameplay;
using UnityEditor;
using UnityEngine;

namespace BeachTycoon.EditorTools
{
    /// <summary>Helpers for assembling low-poly placeholder prefabs out of Unity primitives.</summary>
    internal static class PrimitiveKit
    {
        public static GameObject Empty(string name, Transform parent, Vector3 localPosition, Vector3 localEuler = default)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.SetLocalPositionAndRotation(localPosition, Quaternion.Euler(localEuler));
            return go;
        }

        /// <summary>Creates a collider-free primitive with a palette material.</summary>
        public static GameObject Part(Transform parent, PrimitiveType type, string name, Vector3 localPosition,
            Vector3 localScale, Material material, Vector3 localEuler = default)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.SetLocalPositionAndRotation(localPosition, Quaternion.Euler(localEuler));
            go.transform.localScale = localScale;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            return go;
        }

        /// <summary>A custom mesh (from MeshFactory) with one material per submesh.</summary>
        public static GameObject MeshPart(Transform parent, Mesh mesh, string name, Vector3 localPosition,
            Vector3 localScale, Material[] materials, Vector3 localEuler = default)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false);
            go.transform.SetLocalPositionAndRotation(localPosition, Quaternion.Euler(localEuler));
            go.transform.localScale = localScale;
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            go.GetComponent<MeshRenderer>().sharedMaterials = materials;
            return go;
        }

        public static GameObject MeshPart(Transform parent, Mesh mesh, string name, Vector3 localPosition,
            Vector3 localScale, Material material, Vector3 localEuler = default)
        {
            return MeshPart(parent, mesh, name, localPosition, localScale, new[] { material }, localEuler);
        }

        /// <summary>A gable roof: a cube rolled 45 degrees, so only the upper half shows above the walls.</summary>
        public static GameObject GableRoof(Transform parent, string name, Vector3 ridgeBase, float width, float depth, Material material)
        {
            float side = width / Mathf.Sqrt(2f);
            return Part(parent, PrimitiveType.Cube, name, ridgeBase, new Vector3(side, side, depth), material, new Vector3(0f, 0f, 45f));
        }

        public static BoxCollider AddBox(GameObject go, Vector3 center, Vector3 size)
        {
            var box = go.AddComponent<BoxCollider>();
            box.center = center;
            box.size = size;
            return box;
        }

        /// <summary>Flat world-space progress bar whose fill scales from the left edge.</summary>
        public static ProgressBarView ProgressBar(Transform parent, Vector3 localPosition, float width,
            Material back, Material fill)
        {
            GameObject bar = Empty("ProgressBar", parent, localPosition);
            Part(bar.transform, PrimitiveType.Cube, "Back", Vector3.zero, new Vector3(width, 0.05f, 0.24f), back);

            GameObject pivot = Empty("FillPivot", bar.transform, new Vector3(-width * 0.5f, 0.03f, 0f));
            Part(pivot.transform, PrimitiveType.Cube, "Fill", new Vector3(width * 0.5f, 0f, 0f),
                new Vector3(width, 0.05f, 0.16f), fill);

            var view = bar.AddComponent<ProgressBarView>();
            SetReference(view, "fillPivot", pivot.transform);
            return view;
        }

        /// <summary>Saves the hierarchy as a prefab asset (overwriting) and destroys the scene copy.</summary>
        public static GameObject SavePrefab(GameObject root, string path)
        {
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        public static void SetReference(Object target, string fieldName, Object value)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = FindProperty(serialized, fieldName);
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetReferences(Object target, string fieldName, Object[] values)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = FindProperty(serialized, fieldName);
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static SerializedProperty FindProperty(SerializedObject serialized, string fieldName)
        {
            SerializedProperty property = serialized.FindProperty(fieldName);
            if (property == null)
            {
                throw new System.ArgumentException(
                    $"{serialized.targetObject.GetType().Name} has no serialized field '{fieldName}'.");
            }

            return property;
        }
    }
}
