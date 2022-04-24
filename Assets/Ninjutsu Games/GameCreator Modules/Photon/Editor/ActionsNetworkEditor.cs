using UnityEditor;

namespace NJG.PUN
{
    [CustomEditor(typeof(ActionsNetwork))]
    public class ActionsNetworkEditor : Editor
    {
        private readonly string[] exlude = new string[] { "m_Script" };

        public override void OnInspectorGUI()
        {
            if (serializedObject == null) return;

            serializedObject.Update();

            DrawPropertiesExcluding(serializedObject, exlude);

            //EditorGUILayout.HelpBox("Instance ID: "+target.GetInstanceID(), MessageType.None);

            serializedObject.ApplyModifiedProperties();
        }

        // INITIALIZERS: -----------------------------------------------------------------------------------------------

        private void OnEnable()
        {
            //if (this.target != null) this.target.hideFlags = HideFlags.None;
            //if (this.target != null) this.target.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
        }
    }
}
