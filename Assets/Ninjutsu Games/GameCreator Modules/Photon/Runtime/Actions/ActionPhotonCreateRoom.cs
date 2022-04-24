namespace GameCreator.Core
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Events;
    using Core;
    using Characters;
    using Variables;
    using Photon.Realtime;
    using Photon.Pun;

#if UNITY_EDITOR
    using UnityEditor;
#endif

    [AddComponentMenu("")]
    public class ActionPhotonCreateRoom : IAction
    {
        public StringProperty roomName = new StringProperty("Development");
        public IntProperty maxPlayers = new IntProperty(0);
        public IntProperty playerTTL = new IntProperty(0);
        public IntProperty emptyRoomTTL = new IntProperty(0);

        // EXECUTABLE: ----------------------------------------------------------------------------

        public override IEnumerator Execute(GameObject target, IAction[] actions, int index)
        {
            RoomOptions roomOptions = new RoomOptions() { };
            roomOptions.PublishUserId = true;
            roomOptions.MaxPlayers = (byte)((float)maxPlayers.GetValue(target));
            roomOptions.PlayerTtl = playerTTL.GetValue(target);
            roomOptions.EmptyRoomTtl = emptyRoomTTL.GetValue(target);
            PhotonNetwork.CreateRoom(string.IsNullOrEmpty(roomName.GetValue(target)) ? null : roomName.GetValue(target), roomOptions, TypedLobby.Default);
            yield return 0;
        }

        // +--------------------------------------------------------------------------------------+
        // | EDITOR                                                                               |
        // +--------------------------------------------------------------------------------------+

#if UNITY_EDITOR

        public const string CUSTOM_ICON_PATH = "Assets/Ninjutsu Games/GameCreator Modules/Photon/Icons/Actions/";

        public static new string NAME = "Photon/Create Room";
        private const string NODE_TITLE = "Create Room: {0}";

        // PROPERTIES: ----------------------------------------------------------------------------

        private SerializedProperty spRoomName;
        private SerializedProperty spMaxPlayers;
        private SerializedProperty spPlayerTTL;
        private SerializedProperty spRoomTtl;

        // INSPECTOR METHODS: ---------------------------------------------------------------------

        public override string GetNodeTitle()
        {
            return string.Format(NODE_TITLE, roomName);
        }

        protected override void OnEnableEditorChild()
        {
            spRoomName = serializedObject.FindProperty("roomName");
            spMaxPlayers = serializedObject.FindProperty("maxPlayers");
            spPlayerTTL = serializedObject.FindProperty("playerTTL");
            spRoomTtl = serializedObject.FindProperty("emptyRoomTTL");
        }

        protected override void OnDisableEditorChild()
        {
            spRoomName = null;
            spMaxPlayers = null;
            spPlayerTTL = null;
            spRoomTtl = null;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(spRoomName);
            
            EditorGUILayout.PropertyField(spMaxPlayers, new GUIContent("Max Players", "Max number of players that can be in the room at any time. 0 means 'no limit'."));
            EditorGUILayout.PropertyField(spPlayerTTL, new GUIContent("Player Ttl", "Time To Live (TTL) for an 'actor' in a room. If a client disconnects, this actor is inactive first and removed after this timeout. In milliseconds."));
            EditorGUILayout.PropertyField(spRoomTtl, new GUIContent("Empty Room Ttl", "Time To Live (TTL) for a room when the last player leaves. Keeps room in memory for case a player re-joins soon. In milliseconds.."));

            serializedObject.ApplyModifiedProperties();
        }

#endif
    }
}
