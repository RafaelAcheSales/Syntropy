using UnityEditor;
using UnityEngine;

namespace NJG.PUN
{
    [CustomEditor(typeof(RPCNetwork))]

    public class RPCNetworkEditor : Editor
    {
        private readonly string[] exlude = new string[] { "m_Script" };
        private string INFO = "Component automatically added to handle RPC igniters.\nOnly 1 instance per parent or PhotonView is needed.";

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawPropertiesExcluding(serializedObject, exlude);

            EditorGUILayout.HelpBox(INFO, MessageType.Warning);

            serializedObject.ApplyModifiedProperties();
        }
    }
}