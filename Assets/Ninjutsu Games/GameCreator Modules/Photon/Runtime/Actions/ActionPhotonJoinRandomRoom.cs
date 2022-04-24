namespace GameCreator.Core
{
    using UnityEngine;
    using Photon.Pun;

#if UNITY_EDITOR
    using UnityEditor;
#endif

    [AddComponentMenu("")]
    public class ActionPhotonJoinRandomRoom : IAction
    {
        // EXECUTABLE: ----------------------------------------------------------------------------

        public override bool InstantExecute(GameObject target, IAction[] actions, int index)
        {
            return PhotonNetwork.JoinRandomRoom();
        }

        // +--------------------------------------------------------------------------------------+
        // | EDITOR                                                                               |
        // +--------------------------------------------------------------------------------------+

#if UNITY_EDITOR

        public const string CUSTOM_ICON_PATH = "Assets/Ninjutsu Games/GameCreator Modules/Photon/Icons/Actions/";

        public static new string NAME = "Photon/Join Random Room";
        private const string NODE_TITLE = "Join Random Room";
        private const string INFO = "Joins any available room of the currently used lobby and fails if none is available.";

        // PROPERTIES: ----------------------------------------------------------------------------


        // INSPECTOR METHODS: ---------------------------------------------------------------------

        public override string GetNodeTitle()
        {
            return NODE_TITLE;
        }
        
        public override void OnInspectorGUI()
        {
            this.serializedObject.Update();

            EditorGUILayout.HelpBox(INFO, MessageType.Info);

            this.serializedObject.ApplyModifiedProperties();
        }

#endif
    }
}
