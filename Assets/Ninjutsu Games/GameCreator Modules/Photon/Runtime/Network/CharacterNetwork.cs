#if PHOTON_UNITY_NETWORKING

using System;
using System.Collections;
using System.Collections.Generic;
using GameCreator.Characters;
using GameCreator.Core;
using GameCreator.Core.Hooks;
using Photon.Pun;
using Photon.Pun.UtilityScripts;
using Photon.Realtime;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;
#if PHOTON_RPG
using NJG.RPG;
#endif

#if PHOTON_RPG
using NJG.GC.AI;
#endif

namespace NJG.PUN
{
    [AddComponentMenu("")]
    [RequireComponent(typeof(PhotonView), typeof(Character)), DisallowMultipleComponent]
    public class CharacterNetwork : ObjectNetwork, IPunObservable, IPunInstantiateMagicCallback
    {
        [Serializable]
        public class LocomotionSettings
        {
            //public bool useCompression = true;
            public bool syncPosition = true;
            //public bool syncDirection = true;
            //public bool syncRotation = true;
            public bool syncRunSpeed = true;
            public bool syncCanRun = true;
            public bool syncCanJump = true;
            public bool syncJump = true;
            public bool syncAngularSpeed = true;
            public bool syncGravity = true;
        }
        public static CharacterNetwork Player { get; private set; }

        private const float STOP_THRESHOLD = 0.05f;
        private const float SMOOTH_SPEED = 0.2f;

        public float teleportIfDistance = 2;
        //public float rotationDampening = 10;
        public LocomotionSettings locomotion = new LocomotionSettings();
        public bool syncAttachments;
        public bool networkCulling;

        private Vector3 targetPosition;
        //private float remoteRotation;
        private Character character;
        private int firstUpdate;
        //private ILocomotionSystem.TargetRotation cRotation = new ILocomotionSystem.TargetRotation(true);
        //private Vector3 lastPosition;
        private const string JUMP = "Jump";
        private const string RPG_CHARACTER = "UpdateCharacter";
        private const string RPC_ANGULAR_SPEED = "UpdateAngularSpeed";
        private const string RPC_CONTROLS = "UpdateControls";
        private const string RPC_JUMP = "UpdateJump";
        private const string RPC_GRAVITY = "UpdateGravity";
        private const string RPC_ON_ATTACH = "OnAttach";
        private const string RPC_ON_DETACH = "OnDetach";
        private const string LOCAL_FORMAT = "[{0}] {1} - Local{2}";
        private const string REMOTE_FORMAT = "[{0}] {1} - Remote{2}";
        private const string MASTER = " [MasterClient]";
        private const string CLONE = "(Clone)";
        private const string SPLIT_STRING = "|";
        private const string SPLIT_STRING2 = "%";
        private const string ATTACHMENT_FORMAT = "{0}|{1}";
        private const string ATTACHMENT_ERROR = "Could not find attachment '{0}'. You need add it to the Attachment List in Photon Setttings";
        private const string ATTACHMENT_CHARACTER_ERROR = "Could not find attachment '{0}' in this character";
        private readonly Hashtable emptyHashtable = new Hashtable();

        private float currentAngularSpeed;
        private float lastAngularSpeed;

        private Vector2 currentControls;

        private Vector2 currentJump;
        private Vector2 lastJump;

        private Vector2 currentGravity = Vector2.zero;
        private Vector2 lastGravity;

        private float updateRate = 0.5f;
        private float lastUpdate;
        private Vector2 lastControls;
        private string originalName;
        private CharacterAttachments characterAttachments;
        private static DatabasePhoton DB;
        private Vector3 targetDirection;
        private Vector3 faceDirectionVelocity;
        private Vector3 targetPositionVelocity;

        public CharacterLocomotion Locomotion
        {
            get { if(mLocomotion == null) mLocomotion = character.characterLocomotion; return mLocomotion; }
        }
        private CharacterLocomotion mLocomotion;

        #if PHOTON_RPG
        public Actor Actor
        {
            get { if (mActor == null) mActor = GetComponent<Actor>(); return mActor; }
        }
        private Actor mActor;
#endif
        //private float distance;
        //private float minDirectionSend = 0.099f;

#if PHOTON_STATS
        public StatsNetwork Stats
        {
            get { if(mStats == null) mStats = GetComponent<StatsNetwork>(); return mStats; }
        }
        private StatsNetwork mStats;
#endif

        public bool isNpc;
#if PHOTON_RPG
        public NPCCharacter npc;
#endif
        
        private const int MASK_STATS = 1;
        private const int MASK_POSITION = 2;
        private const int MASK_DIRECTION = 4;
        //public const int MASK_Rotation = 8;
        private const int MASK_SPEED = 8;
        private const int MASK_RUN = 16;
        private const int MASK_JUMP = 32;
        private const int MASK_PATROL_POINT = 64;
        private const int MASK_HOME = 128;

        private Vector3 lastDirection;
        private Vector3 lastSentPosition;
        //private float lastRotation;
        private float lastSpeed;
        private bool lastRun;
        private bool lastSentJump;
        private bool isPlayer;

#if PHOTON_RPG
        private int lastPatrolPoint;
        private Vector3 lastHomePosition;
#endif
        private double lastReceivedTime;
        private bool useSmoothPosition;
        private float targetDistance;

        #region Network Culling
        public int orderIndex;
        public static CullArea CullArea;
        public List<byte> previousActiveCells, activeCells;
        public Vector3 lastPosition, currentPosition;

        public double LastReceivedUpdate
        {
            get { return lastReceivedTime; }
        }

        public float Lag
        {
            get { return Mathf.Abs((float)(PhotonNetwork.Time - LastReceivedUpdate)); }
        }

        private bool initialized;
        private bool hasSetAttachmentListeners;
        private float rot;
        private float vel;
#endregion

        // CONSTRUCTORS: --------------------------------------------------------------------------

#if UNITY_EDITOR
        private void HideStuff()
        {
            hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
        }

        private void OnValidate()
        {
            HideStuff();
        }
#endif

#if UNITY_EDITOR
        public void SetupPhotonView()
        {
            if (photonView.ObservedComponents == null) photonView.ObservedComponents = new List<Component>();

            if (photonView.Synchronization == ViewSynchronization.Off) photonView.Synchronization = ViewSynchronization.UnreliableOnChange;
            
            if (photonView.ObservedComponents.Count > 0)
            {
                for (int i = 0; i < photonView.ObservedComponents.Count; i++)
                {
                    if (photonView.ObservedComponents[i] == null)
                    {
                        photonView.ObservedComponents[i] = this;
                        return;
                    }
                }
            }
            if (!photonView.ObservedComponents.Contains(this))
            {
                photonView.ObservedComponents.Add(this);
            }
        }
#endif

        private void Awake()
        {
#if UNITY_EDITOR
            HideStuff();
#endif
            if (DB == null) DB = DatabasePhoton.Load();
            
            character = gameObject.GetComponent<Character>();

            currentAngularSpeed = lastAngularSpeed = Locomotion.angularSpeed;
            currentJump = lastJump = new Vector2(Locomotion.jumpForce, Locomotion.jumpTimes);
            currentGravity = lastGravity = new Vector2(Locomotion.gravity, Locomotion.maxFallSpeed);
            currentControls = lastControls = new Vector2(Locomotion.canRun ? 1 : 0, Locomotion.canJump ? 1 : 0);

            originalName = gameObject.name.Replace(CLONE, string.Empty);

#if PHOTON_RPG
            isNPC = character is NPCCharacter;
            npc = character as NPCCharacter;
#endif

            if (character && PhotonNetwork.InRoom)
            {
                

                /*if (character is PlayerCharacter)
                {
                    if (!photonView.IsMine)
                    {
                        //locomotion.faceDirection = CharacterLocomotion.FACE_DIRECTION.None;
                        //locomotion.SetIsControllable(false);

                        HookPlayer hookPlayer = gameObject.GetComponent<HookPlayer>();
                        Destroy(hookPlayer);

                        /// Workaround to reset the HookPlayer reference to the local player.
                        if (Player)
                        {
                            hookPlayer = Player.GetComponent<HookPlayer>();
                            Destroy(hookPlayer);
                            Player.gameObject.AddComponent<HookPlayer>();
                        }
                    }
                    else
                    {
                        photonView.Owner.TagObject = gameObject;
                        Player = this;
                    }
                }*/
            }

#if PHOTON_RPG
            Actor.onActorReady += (OnActorReady);
            Actor.onClassChange += (OnClassChange);
#endif
        }

#if PHOTON_RPG
        private void OnClassChange(Actor arg0)
        {
            Locomotion.runSpeed = Actor.actorClass.Speed;
        }

        private void OnActorReady(Actor arg0)
        {
            Locomotion.runSpeed = Actor.actorClass.Speed;
        }
#endif
        public override void OnEnable()
        {
            base.OnEnable();

#if UNITY_EDITOR
            HideStuff();
            SetupPhotonView();
#endif

            if (PhotonNetwork.UseRpcMonoBehaviourCache)
            {
                photonView.RefreshRpcMonoBehaviourCache();
            }

            if (networkCulling && photonView.IsMine)
            {

                if (CullArea == null)
                {
                    CullArea = FindObjectOfType<CullArea>();
                }

                previousActiveCells = new List<byte>(0);
                activeCells = new List<byte>(0);

                currentPosition = lastPosition = transform.position;
            }

#if UNITY_EDITOR
            FormatName();
#endif
        }

        public override void OnDisable()
        {
            if (Application.isPlaying && PhotonNetwork.InRoom && photonView && photonView.IsMine)
            {
                PhotonNetwork.RemoveRPCs(photonView);
            }
            base.OnDisable();
        }

        private void OnJump(int jumpNumber)
        {
            if(photonView.IsMine && gameObject.activeSelf) photonView.RPC(JUMP, RpcTarget.Others);
        }

        /*private void OnDash()
        {

        }*/

        private void Start()
        {
            if (initialized) return;
            initialized = true;

            if (networkCulling && photonView.IsMine && PhotonNetwork.InRoom)
            {
                if (CullArea.NumberOfSubdivisions == 0)
                {
                    photonView.Group = CullArea.FIRST_GROUP_ID;

                    PhotonNetwork.SetInterestGroups(CullArea.FIRST_GROUP_ID, true);
                }
            }

            if (character && PhotonNetwork.InRoom && gameObject)
            {
                character.onJump.AddListener(OnJump);
                //character.onDash.AddListener(OnDash);

                if (character is PlayerCharacter)
                {
                    isPlayer = character is PlayerCharacter;
                    if (!photonView.IsMine)
                    {
                        //locomotion.faceDirection = CharacterLocomotion.FACE_DIRECTION.None;
                        //locomotion.SetIsControllable(false);

                        HookPlayer hookPlayer = gameObject.GetComponent<HookPlayer>();
                        Destroy(hookPlayer);

                        // Workaround to reset the HookPlayer reference to the local player.
                        if (Player)
                        {
                            hookPlayer = Player.GetComponent<HookPlayer>();
                            Destroy(hookPlayer);
                            Player.gameObject.AddComponent<HookPlayer>();
                        }
                    }
                    else
                    {
                        Player = this;
                    }

                    photonView.Owner.TagObject = gameObject;
                }
            }

            if (PhotonNetwork.InRoom && !photonView.IsMine && character is PlayerCharacter)
            {
                Locomotion.SetIsControllable(false);

                if (Locomotion.faceDirection != CharacterLocomotion.FACE_DIRECTION.Target)
                {
                    Locomotion.faceDirection = CharacterLocomotion.FACE_DIRECTION.Target;
                    Locomotion.faceDirectionTarget.target = TargetPosition.Target.Invoker;
                }
                //Locomotion.overrideFaceDirection = CharacterLocomotion.OVERRIDE_FACE_DIRECTION.Target;
                //Locomotion.overrideFaceDirectionTarget.target = GameCreator.Core.TargetPosition.Target.Position;
            }
                

            /*if (syncAttachments)
                {
                    while (character.GetCharacterAnimator().GetCharacterAttachments() == null)
                    {
                        yield return null;
                    }

                    characterAttachments = character.GetCharacterAnimator().GetCharacterAttachments();
                    if (photonView.IsMine)
                    {
                        characterAttachments.onAttach += (OnAttach);
                        characterAttachments.onDetach += (OnDettach);
                    }
                }*/


        }

#if UNITY_EDITOR
        private void FormatName()
        {
            if (!PhotonNetwork.InRoom || !photonView) return;

            Player player = photonView.Owner;
            bool master = player?.IsMasterClient ?? false;
            //int actorNumber = player == null ? -1 : player.ActorNumber;

            if (photonView.IsMine)
            {
                gameObject.name = string.Format(LOCAL_FORMAT, photonView.ViewID, originalName, master ? MASTER : string.Empty);                
            }
            else
            {
                gameObject.name = string.Format(REMOTE_FORMAT, photonView.ViewID, originalName, master ? MASTER : string.Empty);
            }
        }
#endif

        private void SetupAttachments()
        {
            if (syncAttachments && !hasSetAttachmentListeners)
            {
                characterAttachments = character.GetCharacterAnimator().GetCharacterAttachments();
                if (characterAttachments)
                {
                    characterAttachments.onAttach.AddListener(OnAttach);
                    characterAttachments.onDetach.AddListener(OnDettach);
                    hasSetAttachmentListeners = true;
                }
            }
        }

        private void Update()
        {
            if (!PhotonNetwork.InRoom) return;

            if (photonView.IsMine)
            {
                if (syncAttachments && !hasSetAttachmentListeners)
                {
                    SetupAttachments();
                }

                if (networkCulling)
                {
                    lastPosition = currentPosition;
                    currentPosition = transform.position;

                    // This is a simple position comparison of the current and the previous position. 
                    // When using Network Culling in a bigger project keep in mind that there might
                    // be more transform-related options, e.g. the rotation, or other options to check.
                    if (currentPosition != lastPosition)
                    {
                        if (HaveActiveCellsChanged())
                        {
                            UpdateInterestGroups();
                        }
                    }
                }

                if (Time.time > lastUpdate)
                {
                    //currentSpeed.x = locomotion.runSpeed;
                    currentAngularSpeed = Locomotion.angularSpeed;
                    if (locomotion.syncAngularSpeed && Math.Abs(currentAngularSpeed - lastAngularSpeed) > PhotonNetwork.PrecisionForFloatSynchronization)
                    {
                        lastAngularSpeed = currentAngularSpeed;
                        photonView.RPC(RPC_ANGULAR_SPEED, RpcTarget.Others, currentAngularSpeed);
                    }

                    currentGravity.x = Locomotion.gravity;
                    currentGravity.y = Locomotion.maxFallSpeed;
                    if (locomotion.syncGravity && currentGravity != lastGravity)
                    {
                        lastGravity = currentGravity;
                        photonView.RPC(RPC_GRAVITY, RpcTarget.Others, currentGravity);
                    }

                    currentJump.x = Locomotion.jumpForce;
                    currentJump.y = Locomotion.jumpTimes;
                    if (locomotion.syncJump && lastJump != currentJump)
                    {
                        lastJump = currentJump;
                        photonView.RPC(RPC_JUMP, RpcTarget.Others, currentJump);
                    }

                    currentControls.x = Locomotion.canRun ?  1 : 0;
                    currentControls.y = Locomotion.canJump ? 1 : 0;
                    if (currentControls != lastControls)
                    {
                        lastControls = currentControls;
                        //photonView.RPC(RPC_CONTROLS, RpcTarget.Others, lastControls);
                    }

                    lastUpdate = Time.time + updateRate;
                }
            }
            else
#if PHOTON_RPG
            if(!isNPC)
#endif
            {
                if (isPlayer && Locomotion.isControllable)
                {
                    Locomotion.isControllable = false;
                }

                /*if (locomotion.syncRotation)
                {
                    Quaternion rot = Quaternion.Euler(0, remoteRotation, 0);
                    //Vector3 targetDir = new Vector3(0, 0, remoteRotation);
                    if (transform.rotation != rot)
                    {
                        float lag = Mathf.Abs((float)(PhotonNetwork.Time - lastReceivedTime));
                        //transform.rotation = Quaternion.Lerp(transform.rotation, rot, Time.smoothDeltaTime * rotationDampening);
                        //transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * rotationDampening * lag);
                    }
                }*/

                //rot = Mathf.SmoothDampAngle(rot, remoteRotation, ref vel, Time.deltaTime * rotationDampening);
                //transform.rotation = Quaternion.AngleAxis(rot, Vector3.up);

                //transform.rotation = Quaternion.Lerp(transform.rotation,  Quaternion.Euler(remoteRotation), Time.deltaTime * rotationDampening);

                //transform.rotation = Quaternion.Lerp(transform.rotation,  Quaternion.Euler(remoteRotation), this.angle * (1.0f / PhotonNetwork.SerializationRate));

                //transform.position = Vector3.MoveTowards(transform.position, this.m_NetworkPosition, this.m_Distance * (1.0f / PhotonNetwork.SerializationRate));
                //transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.Euler(remoteRotation), this.angle * (1.0f / PhotonNetwork.SerializationRate));
            }

            if (!photonView.IsMine)
            {
                if (targetDirection != Vector3.zero)
                {
                    if (Locomotion.faceDirection != CharacterLocomotion.FACE_DIRECTION.Target)
                    {
                        Locomotion.faceDirection = CharacterLocomotion.FACE_DIRECTION.Target;
                        Locomotion.faceDirectionTarget.target = TargetPosition.Target.Invoker;
                    }
                    Locomotion.faceDirectionTarget.offset = 
                        Vector3.SmoothDamp(Locomotion.faceDirectionTarget.offset, targetDirection, ref faceDirectionVelocity, SMOOTH_SPEED);
                }
                else
                {
                    if (Locomotion.faceDirection != CharacterLocomotion.FACE_DIRECTION.MovementDirection)
                    {
                        Locomotion.faceDirection = CharacterLocomotion.FACE_DIRECTION.MovementDirection;
                    }
                }
                if (useSmoothPosition)
                {
                    //transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 5f);
                    transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref targetPositionVelocity, SMOOTH_SPEED);
                    //transform.position = Vector3.MoveTowards(transform.position, targetPosition, this.targetDistance * (1.0f / PhotonNetwork.SerializationRate));
                }
            }
        }

#region Network Culling

        /// <summary>
        ///     Checks if the previously active cells have changed.
        /// </summary>
        /// <returns>True if the previously active cells have changed and false otherwise.</returns>
        private bool HaveActiveCellsChanged()
        {
            if (CullArea.NumberOfSubdivisions == 0)
            {
                return false;
            }

            previousActiveCells = new List<byte>(activeCells);
            activeCells = CullArea.GetActiveCells(transform.position);

            // If the player leaves the area we insert the whole area itself as an active cell.
            // This can be removed if it is sure that the player is not able to leave the area.
            while (activeCells.Count <= CullArea.NumberOfSubdivisions)
            {
                activeCells.Add(CullArea.FIRST_GROUP_ID);
            }

            if (activeCells.Count != previousActiveCells.Count)
            {
                return true;
            }

            if (activeCells[CullArea.NumberOfSubdivisions] != previousActiveCells[CullArea.NumberOfSubdivisions])
            {
                return true;
            }

            return false;
        }

        /// <summary>
        ///     Unsubscribes from old and subscribes to new interest groups.
        /// </summary>
        private void UpdateInterestGroups()
        {
            List<byte> disable = new List<byte>(0);

            foreach (byte groupId in previousActiveCells)
            {
                if (!activeCells.Contains(groupId))
                {
                    disable.Add(groupId);
                }
            }

            PhotonNetwork.SetInterestGroups(disable.ToArray(), activeCells.ToArray());
        }

#endregion

        // ATTACHMENTS: -----------------------------------------------------------------------------------------------

        private void OnAttach(CharacterAttachments.EventData attachmentData)
        {
            if (photonView.IsMine && attachmentData != null && gameObject.activeSelf)
            {

                Vector3 pos = attachmentData.attachment.transform.localPosition;
                Vector3 rot = attachmentData.attachment.transform.localEulerAngles;
                string attachment = attachmentData.attachment.name.Replace(CLONE, string.Empty);

                photonView.RPC(RPC_ON_ATTACH, RpcTarget.Others, (int)attachmentData.bone, attachment, pos, rot);
            }
        }

        private void OnDettach(CharacterAttachments.EventData attachmentData)
        {
            if (photonView.IsMine && attachmentData != null && gameObject.activeSelf)
            {

                string attachment = attachmentData.attachment.name;

                photonView.RPC(RPC_ON_DETACH, RpcTarget.Others, (int)attachmentData.bone, attachment, attachmentData.isDestroy);
            }
        }

        private GameObject GetAttachment(HumanBodyBones bone, string attachment)
        {
            if (characterAttachments.attachments.ContainsKey(bone))
            {
                var list = characterAttachments.attachments[bone];
                foreach (var at in list)
                {
                    if (at.prefab.name == attachment)
                    {
                        return at.prefab;
                    }
                }
            }

            return null;
        }

        public Hashtable SerializeAttachments()
        {
            if (characterAttachments == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning("Character Attachments has not been initialized yet.", gameObject);
#endif
                return new Hashtable();
            }

            Hashtable atts = new Hashtable(characterAttachments.attachments.Count);

            foreach (var att in characterAttachments.attachments)
            {
                List<CharacterAttachments.Attachment> attachments = att.Value;
                HumanBodyBones bone = att.Key;

                Hashtable atts2 = new Hashtable(attachments.Count);

                for (int i = 0; i< attachments.Count; i++)
                {
                    var attachment = attachments[i];
                    string key = attachment.prefab.name;// + attachment.prefab.GetInstanceID();
                    if (!atts2.ContainsKey(key))
                    {
                        //GameObject go = attachments[i].prefab;
                        //Transform tr = go.transform;
                        //atts2.Add(go.name.Replace(CLONE, string.Empty), string.Format(ATTACHMENT_FORMAT, attachment.locPosition, attachment.locRotation));
                        atts2.Add(attachment.prefab.name, string.Format(ATTACHMENT_FORMAT, attachment.locPosition, attachment.locRotation));
                    }
                }

                atts.Add((int)bone, atts2);
            }

            return atts;
        }

        public static Vector3 StringToVector3(string sVector)
        {
            // Remove the parentheses
            if (sVector.StartsWith("(") && sVector.EndsWith(")"))
            {
                sVector = sVector.Substring(1, sVector.Length - 2);
            }

            // split the items
            string[] sArray = sVector.Split(',');

            // store as a Vector3
            Vector3 result = new Vector3(
                float.Parse(sArray[0]),
                float.Parse(sArray[1]),
                float.Parse(sArray[2]));

            return result;
        }

        public void DeserializeAttachments(Hashtable attachments)
        {
            foreach (var o in attachments)
            {
                if (o.Value == null) continue;
                HumanBodyBones bone = (HumanBodyBones)o.Key;
                Hashtable atts = (Hashtable)o.Value;

                foreach (var att in atts)
                {
                    string prefabName = (string)att.Key;

                    GameObject prefab = DB.GetAttachmentPrefab(prefabName);
                    if (prefab == null)
                    {
#if UNITY_EDITOR
                        Debug.LogWarningFormat(ATTACHMENT_ERROR, prefabName);
#endif
                        continue;
                    }
                    string[] arr = (att.Value as string)?.Split(new[] { SPLIT_STRING }, StringSplitOptions.RemoveEmptyEntries);
                    Vector3 position = arr == null ? Vector3.zero : StringToVector3(arr[0]);
                    Vector3 rotation = arr == null ? Vector3.zero : StringToVector3(arr[1]);

                    characterAttachments.Remove(bone);
                    characterAttachments.Attach(bone, prefab, position, Quaternion.Euler(rotation));
                }
            }
        }

        // RPCS: -----------------------------------------------------------------------------------------------

        [PunRPC]
        private void OnAttach(int bone, string attachment, Vector3 position, Vector3 rotation)
        {
            if (!hasSetAttachmentListeners)
            {
                SetupAttachments();
            }

            if (characterAttachments == null) return;

            GameObject prefab = DB.GetAttachmentPrefab(attachment);
            if (prefab == null)
            {
#if UNITY_EDITOR
                Debug.LogWarningFormat(ATTACHMENT_ERROR, attachment);
#endif
                return;
            }

            characterAttachments.Attach((HumanBodyBones)bone, prefab, position, Quaternion.Euler(rotation));
        }

        [PunRPC]
        private void OnDetach(int bone, string attachment, bool isRemove)
        {
            if (!hasSetAttachmentListeners)
            {
                SetupAttachments();
            }

            if (characterAttachments == null) return;

                GameObject go = GetAttachment((HumanBodyBones)bone, attachment);
            if (go == null)
            {
#if UNITY_EDITOR
                Debug.LogWarningFormat(gameObject, ATTACHMENT_CHARACTER_ERROR, attachment);
#endif
                return;
            }

            if (isRemove) characterAttachments.Remove(go);
            else characterAttachments.Detach(go);
        }

        [PunRPC]
        private void UpdateJump(Vector2 jumpForce)
        {
            lastJump = jumpForce;
            Locomotion.jumpForce = lastJump.x;
            Locomotion.jumpTimes = (int)lastJump.y;
        }

        /*[PunRPC]
        private void UpdateControls(Vector2 newControls)
        {
            currentControls = newControls;
            locomotion.canRun = currentControls.x == 1;
            locomotion.canJump = currentControls.y == 1;
        }*/

        [PunRPC]
        private void UpdateAngularSpeed(float newSpeed)
        {
            currentAngularSpeed = newSpeed;
            //locomotion.runSpeed = currentSpeed.x;        
            Locomotion.angularSpeed = currentAngularSpeed;
        }

        [PunRPC]
        private void UpdateGravity(Vector2 newGravity)
        {
            currentGravity = newGravity;
            Locomotion.gravity = currentGravity.x;
            Locomotion.maxFallSpeed = currentGravity.y;
        }

        [PunRPC]
        private void Jump()
        {
            character.Jump();
        }

        [PunRPC]
        public override void ActionRPC()
        {
            base.ActionRPC();
        }

        [PunRPC]
        private void UpdateCharacter(Vector3 position, bool canUseNavigationMesh, 
            Vector2 controls, Vector2 speed, Vector2 gravity, Vector2 jump, int faceDirection, Hashtable attachments)
        {
            
            var lcm = Locomotion;

            lcm.canRun = (int)controls.x == 1;
            lcm.canJump = (int)controls.y == 1;

            lcm.canUseNavigationMesh = canUseNavigationMesh;

            lcm.angularSpeed = speed.y;
            lcm.runSpeed = speed.x;

            lcm.gravity = gravity.x;
            lcm.maxFallSpeed = gravity.y;

            lcm.jumpForce = jump.x;
            lcm.jumpTimes = (int)jump.y;

            transform.position = position;
            if (canUseNavigationMesh && lcm.navmeshAgent)
            {
                lcm.navmeshAgent.Warp(transform.position);
            }
            //lcm.faceDirection = (CharacterLocomotion.FACE_DIRECTION)faceDirection;

            if (attachments != null && attachments.Count > 0)
            {
                StartCoroutine(CorUpdateAttachments(attachments));
            }
        }

        private IEnumerator CorUpdateAttachments(Hashtable attachments)
        {
            while (character.GetCharacterAnimator().GetCharacterAttachments() == null)
            {
                yield return null;
            }

            characterAttachments = character.GetCharacterAnimator().GetCharacterAttachments();
            DeserializeAttachments(attachments);
        }

        // PHOTON EVENTS: -----------------------------------------------------------------------------------------------

        public override void OnMasterClientSwitched(Player newMasterClient)
        {
#if UNITY_EDITOR
            if (PhotonNetwork.InRoom) FormatName();
#endif
        }

        public override void OnPlayerEnteredRoom(Player player)
        {
            if (photonView.IsMine && gameObject.activeSelf)
            {
                var lcm = Locomotion;
                photonView.RPC(RPG_CHARACTER, player, transform.position, lcm.canUseNavigationMesh, currentControls, new Vector2(Locomotion.runSpeed, currentAngularSpeed), 
                    currentGravity, currentJump, (int)lcm.faceDirection, syncAttachments ? SerializeAttachments() : emptyHashtable);
            }
        }

        

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                // Initialize sendmask and sendflags
                int sendMask = 0;

                bool sendPosition = false;
                bool sendDirection = false;
                //bool sendRotation = false;
                bool sendSpeed = false;
                bool sendRun = false;
                bool sendJump = false;

#if PHOTON_STATS
                bool sendStats = false;
#endif

#if PHOTON_RPG
                bool sendAIPatrolPoint = false;
                bool sendAIHome = false;
#endif


#if PHOTON_STATS
                if (Stats != null && Stats.attributesChanged != null && Stats.attributesChanged.Count > 0)
                {
                    sendStats = true;
                    sendMask += MASK_STATS;
                }
#endif


                //PhotonNetwork.PrecisionForVectorSynchronization
                if (locomotion.syncPosition && 
                    !lastSentPosition.AlmostEquals(transform.position, PhotonNetwork.PrecisionForVectorSynchronization) || firstUpdate < 5)
                {
                    if(firstUpdate < 5) firstUpdate++;
                    sendPosition = true;
                    lastSentPosition = transform.position;
                    sendMask += MASK_POSITION;
                }

                //direction = transform.position - storedPosition;
                //Debug.LogWarningFormat("Direction {0} moveDIr: {1}", direction, Locomotion.currentLocomotionSystem.movementDirection);
                targetDirection = Locomotion.currentLocomotionSystem.movementDirection;
                //storedPosition = transform.position;

                if(locomotion.syncPosition && 
                    !lastDirection.AlmostEquals(targetDirection, PhotonNetwork.PrecisionForVectorSynchronization))
                {
                    sendDirection = true;
                    lastDirection = targetDirection;
                    sendMask += MASK_DIRECTION;
                }

                /*if (locomotion.syncRotation && 
                    !isNPC && !lastRotation.AlmostEquals(transform.eulerAngles.y, PhotonNetwork.PrecisionForFloatSynchronization))
                {
                    sendRotation = true;
                    lastRotation = transform.eulerAngles.y;
                    sendMask += MASK_Rotation;
                }*/

                if (locomotion.syncRunSpeed && Math.Abs(lastSpeed - Locomotion.runSpeed) > PhotonNetwork.PrecisionForFloatSynchronization)
                {
                    sendSpeed = true;
                    lastSpeed = Locomotion.runSpeed;
                    sendMask += MASK_SPEED;
                }

                if (locomotion.syncCanRun && lastRun != Locomotion.canRun)
                {
                    sendRun = true;
                    lastRun = Locomotion.canRun;
                    sendMask += MASK_RUN;
                }

                if (locomotion.syncCanJump && lastSentJump != Locomotion.canJump)
                {
                    sendJump = true;
                    lastSentJump = Locomotion.canJump;
                    sendMask += MASK_JUMP;
                }

#if PHOTON_RPG
                if (isNPC && npc.currPatrolPoint != lastPatrolPoint)
                {
                    lastPatrolPoint = npc.currPatrolPoint;
                    sendAIPatrolPoint = true;
                    sendMask += MASK_PatrolPoint;
                }

                if (isNPC && npc.home != lastHomePosition)
                {
                    lastHomePosition = npc.home;
                    sendAIHome = true;
                    sendMask += MASK_Home;
                }
#endif

                // Send the bitmask that identifies which parameters will be updated
                stream.SendNext(sendMask);

                /*if (locomotion.useCompression)
                {
                    if (sendAIHome) stream.SendNext(Compression.PackVector(lastHomePosition));
                    if (sendAIPatrolPoint) stream.SendNext(lastPatrolPoint);
                    if (sendJump) stream.SendNext(Locomotion.canJump);
                    if (sendRun) stream.SendNext(Locomotion.canRun);
                    if (sendSpeed) stream.SendNext(Locomotion.runSpeed);
                    if (sendRotation) stream.SendNext(transform.eulerAngles.y);
                    if (sendDirection) stream.SendNext(Compression.PackVector(direction));
                    if (sendPosition) stream.SendNext(Compression.PackVector(transform.position));
                    if (sendStats)
                    {
                        stream.SendNext(Stats.attributesChanged);
                        //Stats.attributesChanged.Clear();
                    }
                }
                else
                {*/
#if PHOTON_RPG
                    if (sendAIHome) stream.SendNext(lastHomePosition);
                if (sendAIPatrolPoint) stream.SendNext(lastPatrolPoint);
#endif

                if (sendJump) stream.SendNext(Locomotion.canJump);
                    if (sendRun) stream.SendNext(Locomotion.canRun);
                    if (sendSpeed) stream.SendNext(Locomotion.runSpeed);
                    //if (sendRotation) stream.SendNext(transform.eulerAngles.y);
                    if (sendDirection) stream.SendNext(targetDirection);
                    if (sendPosition) stream.SendNext(transform.position);
#if PHOTON_STATS
                    if (sendStats)
                    {
                        stream.SendNext(Stats.attributesChanged);
                        //Stats.attributesChanged.Clear();
                    }
#endif
                //}
            }
            else
            {
                lastReceivedTime = info.SentServerTime;
                bool receivedPosition = false;
                bool receivedDirection = false;
                //bool useCompression = locomotion.useCompression;

                // Recieve bitmask of updated parameters
                int sendMask = (int)stream.ReceiveNext();

#if PHOTON_RPG
                if (isNPC && sendMask >= MASK_Home)
                {
                    sendMask -= MASK_Home;
                    //npc.home = useCompression ? Compression.UnpackVector((int)stream.ReceiveNext()) : (Vector3)stream.ReceiveNext();
                    npc.home = (Vector3)stream.ReceiveNext();
                }

                if (isNPC && sendMask >= MASK_PatrolPoint)
                {
                    sendMask -= MASK_PatrolPoint;
                    npc.currPatrolPoint = (int)stream.ReceiveNext();
                }
#endif

                // Read in new values for every bit flagged as updated (in highest to lowest order)
                if (sendMask >= MASK_JUMP)
                {
                    sendMask -= MASK_JUMP;
                    if(locomotion.syncCanJump) Locomotion.canJump = (bool)stream.ReceiveNext();
                    //Debug.LogWarning("CharNetwork Jump: " + stream.ToArray().ToStringFull());
                }

                if (sendMask >= MASK_RUN)
                {
                    sendMask -= MASK_RUN;
                    if (locomotion.syncCanRun) Locomotion.canRun = (bool)stream.ReceiveNext();
                    //Debug.LogWarning("CharNetwork Run: " + stream.ToArray().ToStringFull());
                }

                if (sendMask >= MASK_SPEED)
                {
                    sendMask -= MASK_SPEED;
                    if (locomotion.syncRunSpeed) Locomotion.runSpeed = (float)stream.ReceiveNext();
                    //Debug.LogWarning("CharNetwork Speed: " + stream.ToArray().ToStringFull());
                }

                /*if (sendMask >= MASK_Rotation)
                {
                    sendMask -= MASK_Rotation;
                    if (!isNPC && locomotion.syncRotation) remoteRotation = (float)stream.ReceiveNext();
                    //Debug.LogWarning("CharNetwork Rotation: " + stream.ToArray().ToStringFull());
                }*/

                if (sendMask >= MASK_DIRECTION)
                {
                    sendMask -= MASK_DIRECTION;
                    if (locomotion.syncPosition)
                    {
                        //direction = useCompression ? Compression.UnpackVector((int)stream.ReceiveNext()) : (Vector3)stream.ReceiveNext();
                        targetDirection = (Vector3)stream.ReceiveNext();
                        //Debug.LogWarning("CharNetwork Direction: " + stream.ToArray().ToStringFull());
                        receivedDirection = true;
                    }
                }

                if (sendMask >= MASK_POSITION)
                {
                    sendMask -= MASK_POSITION;
                    if (locomotion.syncPosition)
                    {
                        //targetPosition = useCompression ? Compression.UnpackVector((int)stream.ReceiveNext()) : (Vector3)stream.ReceiveNext();
                        targetPosition = (Vector3)stream.ReceiveNext();
                        receivedPosition = true;
                    }
                }
                
#if PHOTON_STATS
                if (sendMask >= MASK_STATS)
                {
                    sendMask -= MASK_STATS;                    
                    Stats.ReceiveUpdate((Hashtable)stream.ReceiveNext());
                    //Debug.LogWarning("CharNetwork Stats: " + stream.ToArray().ToStringFull()+ " sendMask: "+ sendMask);
                }
#endif

                if (receivedPosition)
                {
                    if (firstUpdate < 3)
                    {
                        firstUpdate++;
                        transform.position = targetPosition;
                        if (Locomotion.canUseNavigationMesh && Locomotion.navmeshAgent)
                        {
                            Locomotion.navmeshAgent.Warp(transform.position);
                        }
                    }
                    else if (receivedDirection)
                    {
                        float lag = Mathf.Abs((float)(PhotonNetwork.Time - lastReceivedTime));
                        targetPosition += targetDirection * lag;
                        targetDistance = Vector3.Distance(transform.position, targetPosition);
                        //Locomotion.currentLocomotionSystem.aimDirection = forward;
                        //Locomotion.currentLocomotionSystem.aimDirection = this.direction;
                        // Locomotion.currentLocomotionSystem.movementDirection = this.direction;
                    }

                    //if (targetPosition != lastTargetPosition)
                    //{
                    //lastTargetPosition = targetPosition;

                    bool setTargetPosition = false;

                    if (Locomotion.canUseNavigationMesh)
                    {
                        if (!Locomotion.navmeshAgent.enabled) Locomotion.navmeshAgent.enabled = true;

                        if (Locomotion.navmeshAgent && Locomotion.navmeshAgent.isOnNavMesh)
                        {
                            setTargetPosition = true;
                        }
                        else transform.position = targetPosition;
                    }
                    else
                    {
                        setTargetPosition = true;
                    }
                    //}

                    if (setTargetPosition)
                    {
#if PHOTON_RPG
                        if (!Actor.IsDeath() && !Locomotion.currentLocomotionSystem.isDashing && character.IsGrounded())
                        {
                            ///if (Locomotion.character.IsGrounded())
                            //{
                                //Locomotion.SetTarget(targetPosition, !Locomotion.character.IsGrounded() ? 
                                //    new ILocomotionSystem.TargetRotation(true, direction) : null, STOP_THRESHOLD);
                            //}
                            //else
                            //{
                            //    transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 5f);
                            //}

                            useSmoothPosition = false;
                            Locomotion.SetTarget(targetPosition, null, STOP_THRESHOLD);
                        }
                        else

                        {
                            useSmoothPosition = true;
                            /*transform.position = targetPosition;
                            if (Locomotion.canUseNavigationMesh && Locomotion.navmeshAgent)
                            {
                                Locomotion.navmeshAgent.Warp(transform.position);
                            }*/

                            //ILocomotionSystem.TargetRotation rot = new ILocomotionSystem.TargetRotation(true, direction);
                            //Locomotion.SetTarget(targetPosition, rot);
                        }
#else
                        if (character.enabled && !character.IsRagdoll() && !Locomotion.currentLocomotionSystem.isDashing && character.IsGrounded())
                        {
                            useSmoothPosition = false;
                            Locomotion.SetTarget(targetPosition, null, STOP_THRESHOLD);
                        }
                        else
                        {
                            if(character.GetCharacterAnimator().animator.enabled && character.enabled && !character.IsRagdoll()) useSmoothPosition = true;
                            else
                            {
                                useSmoothPosition = false;
                                if (Locomotion.canUseNavigationMesh && Locomotion.navmeshAgent)
                                {
                                    Locomotion.navmeshAgent.Warp(transform.position);
                                }
                                else transform.position = targetPosition;
                            }
                        }
                        //new ILocomotionSystem.TargetRotation(true, direction)
#endif
                    }

                    if (teleportIfDistance > 0 && !Locomotion.currentLocomotionSystem.isDashing)
                    {
                        float dist = Vector3.Distance(transform.position, targetPosition);
                        //float dist = (transform.position - targetPosition).sqrMagnitude;
                        if (dist >= teleportIfDistance) transform.position = targetPosition;
                    }
                }
            }

            if (networkCulling)
            {
                // If the player leaves the area we insert the whole area itself as an active cell.
                // This can be removed if it is sure that the player is not able to leave the area.
                while (activeCells.Count <= CullArea.NumberOfSubdivisions)
                {
                    activeCells.Add(CullArea.FIRST_GROUP_ID);
                }

                if (CullArea.NumberOfSubdivisions == 1)
                {
                    orderIndex = (++orderIndex % CullArea.SUBDIVISION_FIRST_LEVEL_ORDER.Length);
                    photonView.Group = activeCells[CullArea.SUBDIVISION_FIRST_LEVEL_ORDER[orderIndex]];
                }
                else if (CullArea.NumberOfSubdivisions == 2)
                {
                    orderIndex = (++orderIndex % CullArea.SUBDIVISION_SECOND_LEVEL_ORDER.Length);
                    photonView.Group = activeCells[CullArea.SUBDIVISION_SECOND_LEVEL_ORDER[orderIndex]];
                }
                else if (CullArea.NumberOfSubdivisions == 3)
                {
                    orderIndex = (++orderIndex % CullArea.SUBDIVISION_THIRD_LEVEL_ORDER.Length);
                    photonView.Group = activeCells[CullArea.SUBDIVISION_THIRD_LEVEL_ORDER[orderIndex]];
                }
            }
        }

        public void OnPhotonInstantiate(PhotonMessageInfo info)
        {
            if (Locomotion.canUseNavigationMesh && !Locomotion.navmeshAgent.enabled)
            {
                Locomotion.navmeshAgent.enabled = true;
            }

            IgniterOnPhotonInstantiate[] list = GetComponentsInChildren<IgniterOnPhotonInstantiate>();
            for(int i = 0, imax = list.Length; i<imax; i++)
            {
                list[i].ManualExecute(gameObject, info);
            }
        }
    }
}
#endif