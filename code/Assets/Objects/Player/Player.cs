using System;
using System.Collections;
using System.Collections.Generic;
using Entities.Scripts;
using Entities.Scripts.Utils;
using NUnit.Framework;
using Objects.EditorChanges;
using Objects.Engine;
using Objects.Enviorment;
using Objects.Inputs;

using Objects.Player.SubObjects;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using LogType = Entities.Scripts.Utils.LogType;
using Random = UnityEngine.Random;


namespace Objects.Player
{
    //[RequireComponent(typeof(BallSpawner))]
    public class Player : MonoBehaviour, IClickRelay, TriggerRelay
    {
        #region Variables

        private Transform Etransform => transform;

        public delegate void MoveDelegates(Vector2Int direction);

        [FormerlySerializedAs("move")] [SerializeField]
        private InputAction roll;

        [SerializeField] private InputAction shift;

        [FormerlySerializedAs("Fire")] [SerializeField]
        private InputAction fire;

        [FormerlySerializedAs("Undo")] [SerializeField]
        private InputAction undo;

        [FormerlySerializedAs("_actionAble")] [SerializeField, ReadOnlyField]
        private bool actionAble;

        [FormerlySerializedAs("CheckHoldTime")]
        [Header("Input specifics")]
        [SerializeField]
        [FormerlySerializedAs("_mySlimeCubes")]
        public List<SlimeCube> mySlimeCubes = new();

        private readonly Stack<AUndo> _undoStack = new();

        public PlayerAction Currentaction { get; private set; }

        private bool IsGrounded
        {
            get
            {
                if (!Physics.Raycast(transform.position, Vector3.down, out var hit, 1, CoKnos.RayCastForBounds))
                    return false;
                GameObject hitGameObject = hit.collider.gameObject;
                if (hitGameObject.GetComponent<SlimeCube>() &&
                    !hitGameObject.GetComponent<SlimeCube>().IsAttachedToPlayer)
                    return true;
                if (hitGameObject.GetComponent<Obstacle>())
                    return true;
                return false;
            }
        }

        [SerializeField] private bool shiftDisabled;
        private InputBuffer _inputBuffer;

        public AUndo[] UndoArray => _undoStack.ToArray();

        private AudioSource _audioSource;

        [SerializeField]

        private AudioClip _rollSound;
        #endregion


        void Start()
        {
            CoKnos.Player = this;
            SetUserAction();
            actionAble = true;
           

            _inputBuffer = new InputBuffer(this);
            _audioSource = GetComponent<AudioSource>();
        }

        private void Update()
        {
            _inputBuffer.Update(actionAble);
        }

        public void LogUndoStack()
        {
            foreach (var aUndo in _undoStack)
            {
                Debug.Log(aUndo);
            }
        }


        #region InputStuff

        /// <summary>
        /// This binds the InputActions for the new Unity Input System
        /// </summary>
        private void SetUserAction()
        {
            roll.started += ctx =>
            {
                void Succ(Vector2Int dir)
                {
                    RollAction rollAction = new RollAction(dir, this);
                    rollAction.StartAction();
                    Currentaction = rollAction;
                }

                AdvDebug.Log($"RollAction started with {ctx.ReadValue<Vector2>()}", LogType.Input, LogLevel.Verbose);
                if (actionAble)
                {
                    Succ(GLUtil.Vector2Int(ctx.ReadValue<Vector2>()));
                }
                else
                {
                    AdvDebug.Log($"RollAction with {ctx.ReadValue<Vector2>()} was not actionAble", LogType.Input,
                        LogLevel.Notice);
                    _inputBuffer.BufferInput(Succ,GLUtil.Vector2Int(ctx.ReadValue<Vector2>()));
                }
            };

            roll.canceled += _ => { Debug.Log("Move Ended"); };
            undo.started += _ =>
            {
                PlayerAction currTemp = Currentaction;
                if (Currentaction != null)
                {
                    Currentaction.CancelAction();
                    if (currTemp is FallingAction action)
                        PopUndo();
                    return;
                }

                PopUndo();
            };
        }

        /// <summary>
        /// Enable and Disable the InputActions
        /// </summary>
        private void OnEnable()
        {
            roll.Enable();
            shift.Enable();
            fire.Enable();
            undo.Enable();
        }

        private void OnDisable()
        {
            roll.Disable();
            shift.Enable();
            fire.Disable();
            undo.Disable();
        }

        #endregion

        
        #region Movement

        #region Actions

        public abstract class PlayerAction
        {
            protected Player Player;


            public abstract void StartAction();
            public abstract void CancelAction();

            public abstract string Log();
        }

        /// <summary>
        /// Represents a RollAction
        /// gets Initialized with a Vector2Int and the acting TriggerRelay
        /// StartAction() starts the Enumerator Pivot() that handles the "Animation", it also sets the player to not actionable
        /// CancelAction() stops the Enumerator and undoes the changes, this is only supposed to be called if their
        ///     is a collision in the roll path, as for that's pretty cancer to raycast in before action
        /// EndAction() is called by Pivot() when the animation is done, it pushes the undo onto the stack and sets the player to actionable again
        /// </summary>
        private class RollAction : PlayerAction
        {
            private readonly Vector2Int _vec;
            private UTransform _playerPosTemp;
            private Coroutine _coroutine;
            

            public RollAction(Vector2Int vec, Player player)
            {
                Player = player;
                _vec = vec;
            }

            public override void StartAction()
            {
                _playerPosTemp = new UTransform(Player.transform, Player);
                Player.Currentaction = this;
                Player.actionAble = false;
                _coroutine = Player.StartCoroutine(Pivot(_vec));
                
            }

            public override void CancelAction()
            {
                Player.StopCoroutine(_coroutine);
                _playerPosTemp.Undo();
                Player.actionAble = true;
                Player.Currentaction = null;
                // TriggerRelay.EndCheck();
            }

            public override string Log()
            {
                return $"Roll action: {_playerPosTemp.Log()} in Direction {_vec}";
            }

            private void EndAction()
            {
                Player.PushUndo(_playerPosTemp);
                //TriggerRelay._actionAble = true;
                Player.Currentaction = null;
                Player.EndCheck();
            }

            /// <summary>
            /// Enumerator that handles the "Animation" of the RollAction
            /// </summary>
            /// <param name="vec">Vector in which direction is rolled, this should only be a (+-1,0) or (0, +-1) Vector</param>
            /// <returns></returns>
            IEnumerator Pivot(Vector2Int vec)
            {
                Vector3 dimensions = Player.GetTurnSlimeCube(vec);
                Vector3 pivotPoint =
                    new Vector3(dimensions.x + (vec.x * 0.5f), dimensions.y - 0.5f, dimensions.z + (vec.y * 0.5f)) +
                    Player.transform.position;

                Vector3 pivotAxis = new Vector3(vec.y, 0, -vec.x);
                Debug.DrawRay(pivotPoint, pivotAxis, Color.green, 15);
                Player._audioSource.pitch = Random.Range(0.9f, 1.1f);
                Player._audioSource.PlayOneShot(Player._rollSound);

                float leftToMove = 90;
                while (true)
                {
                    float stepSize = Mathf.Min(leftToMove, 120 * Time.deltaTime);
                    leftToMove -= stepSize;
                    Player.transform.RotateAround(pivotPoint, pivotAxis, stepSize);
                    if (leftToMove <= 0)
                        break;
                    yield return new WaitForNextFrameUnit();
                }
                

//Execute the EndAction Binding to tell the Code, the Enumerator are done
                EndAction();
                if (Player.actionAble && Player.roll.ReadValue<Vector2>() == GLUtil.Vector2Int(vec))
                {
                    _playerPosTemp = new UTransform(Player.transform, Player);
                    Player.Currentaction = this;
                    Player.actionAble = false;
                    _coroutine = Player.StartCoroutine(Pivot(_vec));
                }
            }
        }
        
        public class FallingAction : PlayerAction
        {
            private Coroutine _coroutine;

            public FallingAction(Player player)
            {
                Player = player;
            }

            public override void StartAction()
            {
                Player.Currentaction = this;
                _coroutine = Player.StartCoroutine(FallingEnumerator());
            }

            public override void CancelAction()
            {
                Player.StopCoroutine(_coroutine);
                Vector3 position = Player.transform.position;
                position = new Vector3(position.x, Mathf.CeilToInt(position.y), position.z);
                Player.transform.position = position;
                //TriggerRelay.EndCheck();
                Player.Currentaction = null;
                Player.EndCheck();
            }

            public override string Log()
            {
                return $"Falling action";
            }

            IEnumerator FallingEnumerator()
            {
                float speed = 0f;
                const float acc = 10f;
                while (true)
                {
                    speed += acc * Time.deltaTime;
                    Player.transform.position += new Vector3(0, -speed * Time.deltaTime, 0);
                    yield return new WaitForNextFrameUnit();
                }
                // ReSharper disable once IteratorNeverReturns //This is intended
            }
        }

        #endregion


        #region Support Methods

        //Snaps TriggerRelay back on rotation and position grid and starts an enumerator emulating falling if none are grounded
        private void EndCheck()
        {
            if (!CheckAll())
            {
                FallingAction fallingAction = new(this);
                fallingAction.StartAction();
            }
            else
            {
                actionAble = true;
            }

            GLUtil.SnapTransform(transform);


            bool CheckAll()
            {
                if (IsGrounded)
                    return true;
                foreach (SlimeCube slimeCube in mySlimeCubes)
                {
                    if (slimeCube.IsGrounded) return true;
                }

                return false;
            }
        }

        private Vector3Int GetTurnSlimeCube(Vector2Int vector2Int)
        {
            Vector3Int best = new Vector3Int(int.MaxValue * -vector2Int.x, int.MinValue, int.MaxValue * -vector2Int.y);

            if (IsGrounded || EdgeGrounded(vector2Int))
                best = Vector3Int.zero;

            foreach (SlimeCube slimeCube in mySlimeCubes)
            {
                if (slimeCube.IsGrounded || slimeCube.EdgeGrounded(vector2Int))
                {
                    Vector3Int currSlimeCube = slimeCube.GetLowestPointInDirection(vector2Int);

                    switch (vector2Int.x, vector2Int.y)
                    {
                        case (1, 0):
                            if (currSlimeCube.x > best.x || (currSlimeCube.x == best.x && currSlimeCube.y > best.y))
                                best = currSlimeCube;
                            break;
                        case (-1, 0):
                            if (currSlimeCube.x < best.x || (currSlimeCube.x == best.x && currSlimeCube.y > best.y))
                                best = currSlimeCube;
                            break;
                        case (0, 1):
                            if (currSlimeCube.z > best.z || (currSlimeCube.z == best.z && currSlimeCube.y > best.y))
                                best = currSlimeCube;
                            break;
                        case (0, -1):
                            if (currSlimeCube.z < best.z || (currSlimeCube.z == best.z && currSlimeCube.y > best.y))
                                best = currSlimeCube;
                            break;
                        default:
                            AdvDebug.LogError($"LOL, shouldn't happen, do sth");
                            break;
                    }
                }
            }
            


            return best;

            bool EdgeGrounded(Vector2Int dirI)
            {
                RaycastHit hit;
                if (!Physics.Raycast(transform.position, Vector3.down + new Vector3(dirI.x, 0, dirI.y),
                        out hit,
                        Mathf.Sqrt(2.0f), CoKnos.RayCastForBounds))
                    return false;
                GameObject hitGameObject = hit.collider.gameObject;
                if (hitGameObject.GetComponent<SlimeCube>() &&
                    !hitGameObject.GetComponent<SlimeCube>().IsAttachedToPlayer)
                    return true;
                if (hitGameObject.GetComponent<Obstacle>())
                    return true;
                return false;
            }
        }

        #endregion

        #endregion

        #region SlimeCubeControl
        

        public void AttachSlimeCube(SlimeCube slimeCube)
        {
            var transform1 = slimeCube.transform;
            transform1.parent = transform;
            slimeCube.IsAttachedToPlayer = true;
            mySlimeCubes.Add(slimeCube);
            slimeCube.ClickRelay = this;
            GLUtil.SnapTransform(transform1);
        }

        public void RemoveSlimeCube(SlimeCube slimeCube)
        {
            slimeCube.transform.parent = null;
            slimeCube.IsAttachedToPlayer = false;
            mySlimeCubes.Remove(slimeCube);
            slimeCube.ClickRelay = null;
            GLUtil.SnapTransform(slimeCube.transform);
        }

        public void PushSlimeIn(SlimeCube t)
        {
            PushUndo(new UMerge(t, this));
            AttachSlimeCube(t);
        }
        
        #endregion


        #region UndoClasses

        /// <summary>
        /// ABSTRACT CLASS FOR UNDO
        /// Tick is the Tick the will reset to, if activated
        /// Each Undo Action only stores necessary information to undo the last action
        /// </summary>
        public abstract class AUndo
        {
            public int Tick;

            protected static Player Player;

            public abstract void Undo();

            public abstract string Log();
        }

        private class UTransform : AUndo
        {
            private readonly UMove _uMove;
            private readonly URotate _uRotate;

            public UTransform(Vector3 position, Quaternion rotation, Player player)
            {
                Player = player;
                _uMove = new UMove(position, player);
                _uRotate = new URotate(rotation, player);
            }


            public UTransform(Transform transform, Player player)
            {
                Player = player;
                _uMove = new UMove(transform.position, player);
                _uRotate = new URotate(transform.rotation, player);
            }


            public override void Undo()
            {
                _uMove.Undo();
                _uRotate.Undo();
            }

            public override string Log()
            {
                return _uMove.Log() + " and " + _uRotate.Log();
            }
        }

        private class UMove : AUndo
        {
            private readonly Vector3 _position;

            public UMove(Vector3 position, Player player)
            {
                Player = player;
                _position = position;
            }


            public override void Undo()
            {
                Player.transform.position = _position;
            }

            public override string Log()
            {
                return $"Moved from {_position}";
            }
        }

        private class URotate : AUndo
        {
            private Quaternion _rotation;

            public URotate(Quaternion rotation, Player player)
            {
                Player = player;
                _rotation = rotation;
            }


            public override void Undo()
            {
                Player.transform.rotation = _rotation;
            }

            public override string Log()
            {
                return $"Rotated from {_rotation}";
            }
        }


        public class UMerge : AUndo
        {
            private readonly SlimeCube _slimeCube;
            private readonly Vector3 _position;
            private readonly Quaternion _rotation;

            public UMerge(SlimeCube slimeCube, Player player)
            {
                Player = player;
                _slimeCube = slimeCube;
                var transform1 = slimeCube.transform;
                _position = transform1.position;
                _rotation = transform1.rotation;
            }

            public override void Undo()
            {
                Player.RemoveSlimeCube(_slimeCube);
                var transform1 = _slimeCube.transform;
                transform1.position = _position;
                transform1.rotation = _rotation;
            }

            public override string Log()
            {
                return $"Merged {_slimeCube}";
            }
        }

        public class USplit : AUndo
        {
            private readonly SlimeCube _slimeCube;
            private readonly Vector3 _position;
            private readonly Quaternion _rotation;

            public USplit(SlimeCube slimeCube, Player player)
            {
                Player = player;
                _slimeCube = slimeCube;
                var transform1 = slimeCube.transform;
                _position = transform1.position;
                _rotation = transform1.rotation;
            }

            public override void Undo()
            {
                _slimeCube.RelayHoldAll();
                Player.AttachSlimeCube(_slimeCube);
                var transform1 = _slimeCube.transform;
                transform1.position = _position;
                transform1.rotation = _rotation;
            }

            public override string Log()
            {
                return $"Split {_slimeCube}";
            }
        }

        public class UMultiMerge : AUndo
        {
            private UContainer _container;
            private List<SlimeCube> _slimeCubes;

            public UMultiMerge(MergedSlimeCubes msc, Player player)
            {
                _slimeCubes = new List<SlimeCube>();
                List<AUndo> aUndos = new List<AUndo>();
                foreach (SlimeCube mscMySlimeCube in msc.MySlimeCubes)
                {
                    aUndos.Add(new UMerge(mscMySlimeCube, player));
                    _slimeCubes.Add(mscMySlimeCube);
                }

                _container = new UContainer(aUndos);
            }

            public void Do()
            {
                foreach (SlimeCube slimeCube in _slimeCubes)
                {
                    Player.AttachSlimeCube(slimeCube);
                }
            }

            public override void Undo()
            {
                _container.Undo();
                MergedSlimeCubes mergedSlimeCubes = new MergedSlimeCubes(_slimeCubes);
            }

            public override string Log()
            {
                return $"MultiMerge with " + _container.Log();
            }
        }

        public class UMultiSplit : AUndo
        {
            private UContainer _container;
            private List<SlimeCube> _connected, _noCon;

            public UMultiSplit(SlimeCube clicked, Player player)
            {
                List<AUndo> aUndos = new List<AUndo>();
                aUndos.Add(new UTransform(player.transform, player));
                (_connected, _noCon) = Player.CheckIntegrity(Player.mySlimeCubes, Player, clicked);
                foreach (SlimeCube slimeCube in _noCon)
                {
                    aUndos.Add(new USplit(slimeCube, player));
                }

                AdvDebug.Log($"Multisplit Integrity Check found {_noCon.Count}");

                _container = new UContainer(aUndos);
            }

            public void Do()
            {
                foreach (SlimeCube slimeCube in _noCon)
                {
                    Player.RemoveSlimeCube(slimeCube);
                }

                MergedSlimeCubes mergedSlimeCubes = new MergedSlimeCubes(_noCon);
            }

            public override void Undo()
            {
                foreach (SlimeCube slimeCube in _noCon)
                {
                    slimeCube.RelayHoldAll();
                }

                _container.Undo();
            }

            public override string Log()
            {
                return "MultiSplit: " + _container.Log();
            }
        }
        // UContainer: Container, containing multiple Undo's

        public class UContainer : AUndo
        {
            private List<AUndo> _undos;

            public UContainer(List<AUndo> undos)
            {
                _undos = undos;
            }

            public override void Undo()
            {
                foreach (var undo in _undos)
                {
                    undo.Undo();
                }
            }

            public override string Log()
            {
                string log = "";
                foreach (var undo in _undos)
                {
                    log += undo.Log() + "\n";
                }

                return log;
            }
        }

        #endregion

        #region UndoFunction

        public void PushUndo(AUndo undo)
        {
            if (undo == null)
                return;
            _undoStack.Push(undo);
        }

        public void PopUndo()
        {
            //TODO Check Stack == null lul
            if (_undoStack.Count == 0)
            {
                if (SceneManager.GetActiveScene().name == "The Hub")
                    return;
                AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("The Hub");
                asyncLoad.completed+= operation =>
                {
                    CoKnos.OnSceneLoadEvent.Invoke("The Hub");
                };
                return;
                
            }
            if (_undoStack.TryPop(out AUndo undo))
            {
                undo.Undo();
            }
        }

        #endregion

        #region Trigger

        public void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.GetComponent<Goal>() != null)
            {
                //other.gameObject.GetComponent<Goal>().OnTriggerEnter(other);
                return;
            }

            switch (Currentaction)
            {
                case (RollAction):
                    Currentaction.CancelAction();
                    break;
                case (FallingAction):
                    Currentaction.CancelAction();
                    break;
                case (null):
                    Debug.LogWarning(
                        $"Trigger Enter from {other.gameObject} This should not happen, Player is not in an Action");
                    //This Can Also be triggered if another object already canceled the Action TODO Fix that
                    break;
                default:
                    Debug.LogWarning($"This should not happen, TriggerRelay is in Action: {Currentaction.GetType()}");
                    break;
            }
        }

        public void HoldAll()
        {
            AdvDebug.LogWarning("Player has been called to HoldAll, this should not happen;");
        }

        #endregion

        public (List<SlimeCube>, List<SlimeCube>) CheckIntegrity(List<SlimeCube> allSlimeCubes, Player player,
            SlimeCube cutCube)
        {
            List<SlimeCube> connected = new List<SlimeCube>();
            for (int j = 0; j < 6; j++)
            {
                RaycastHit hit;
                if (Physics.Raycast(transform.position, GLUtil.Dir6Gen(j), out hit, 1, CoKnos.RayCastForBounds))
                {
                    SlimeCube cube = hit.transform.GetComponent<SlimeCube>();
                    if (cube != null && !connected.Contains(cube) && cube != cutCube)
                    {
                        connected.Add(cube);
                    }
                }
            }


            int i = 0;
            while (i < connected.Count)
            {
                SlimeCube curr = connected[i];
                for (int j = 0; j < 6; j++)
                {
                    RaycastHit? hitQ;
                    // ReSharper disable twice PossibleLossOfFraction
                    hitQ = curr.RayCastFromThis(GLUtil.Dir6Gen(j), 1);
                    if (hitQ != null)
                    {
                        RaycastHit hit = (RaycastHit)hitQ;
                        SlimeCube slm = hit.transform.GetComponent<SlimeCube>();
                        if (slm != null && !connected.Contains(slm) && slm != cutCube)
                        {
                            connected.Add(slm);
                        }
                    }
                }

                i++;
            }

            List<SlimeCube> noCon = new List<SlimeCube>();
            foreach (SlimeCube slimeCube in allSlimeCubes)
            {
                if (!connected.Contains(slimeCube))
                {
                    noCon.Add(slimeCube);
                }
            }

            return (connected, noCon);
        }


        //TODO at _actionable checks to all !!!!
        public void AttachMe(SlimeCube slimeCube)
        {
            if (!actionAble)
            {
                AdvDebug.Log($"Caught AttachMe Call, while not Actionable, this would play a sound");
                return;
            }

            PushSlimeIn(slimeCube);
        }

        public void AttachMe(SlimeCube slimeCube, MergedSlimeCubes mergedSlimeCubes)
        {
            if (!actionAble)
            {
                AdvDebug.Log($"Caught AttachMe(Multi) Call, while not Actionable, this would play a sound");
                return;
            }

            mergedSlimeCubes.HoldAll();
            UMultiMerge multiMerge = new UMultiMerge(mergedSlimeCubes, this);
            multiMerge.Do();
            PushUndo(multiMerge);
        }

        public void RelayClick(SlimeCube slimeCube)
        {
            if (!actionAble)
            {
                AdvDebug.Log($"Caught RelayClick for Detach, while not Actionable, this would play a sound");
                return;
            }

            UMultiSplit t = new UMultiSplit(slimeCube, this);
            t.Do();
            PushUndo(t);
            actionAble = false;
            EndCheck();
        }

        public void RelayHover(SlimeCube slimeCube)
        {
            slimeCube.SetHover();
        }
    }

    internal class InputBuffer
    {
        private readonly Player _player;
        private float ValidFor = 0.2f;
        Player.MoveDelegates _inputAction;
        private Vector2Int _dir;
        public InputBuffer(Player player)
        {
            _player = player;
        }

        public void BufferInput(Player.MoveDelegates inputAction, Vector2Int dir)
        {
            ValidFor = 0.2f;
            _inputAction = inputAction;
            _dir = dir;
        }

        public void Update(bool actionAble)
        {
            if (actionAble)
            {
                _inputAction?.Invoke(_dir);
            }
            else if (ValidFor > 0)
            {
                ValidFor -= Time.deltaTime;
            }
            else
            {
                _inputAction = null;
            }
        }
    }
}

//TODO Add Falling Actions to decoupeling and such