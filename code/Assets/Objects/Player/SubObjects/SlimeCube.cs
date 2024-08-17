using System;
using System.Collections.Generic;
using Entities.Scripts.Utils;
using Objects.Engine;
using Objects.Enviorment;
using Objects.Inputs;

using Objects.Render;
using UnityEngine;
using LogType = Entities.Scripts.Utils.LogType;


namespace Objects.Player.SubObjects
{
    
    [RequireComponent(typeof(Collider))]
    public class SlimeCube : MonoBehaviour, IClickableObject
    {
        #region Variables

        public IClickRelay ClickRelay;
        public TriggerRelay TriggerRelay;
     
        private Shape _shape;

        private void Start()
        {

            _shape = GetComponent<Shape>();
        }

        public bool IsAttachedToPlayer
        {
            get
            {
                if (TriggerRelay == null)
                {
                    return false;
                }

                return TriggerRelay.GetType() == typeof(Player);
            }
            set
            {
                if (value)
                {
             
                        GetComponent<Shape>().blendStrength = CoKnos.BlendStrengthConnceted;
                        GetComponent<Shape>().layer = CoKnos.SlimeBlockConnectedLayer;
                    
                }
                else
                {
                   
                        GetComponent<Shape>().blendStrength = CoKnos.BlendStrengthDisConnceted;
                        GetComponent<Shape>().layer = CoKnos.SlimeBlockDisConnectedLayer;
                    
                }

                TriggerRelay = !IsAttachedToPlayer ? (value ? GetComponentInParent<Player>() : null) : TriggerRelay;
            }
        }

        private const bool UseBallSpawner = false;


        public bool IsGrounded
        {
            get
            {
                RaycastHit hit;

                if (!Physics.Raycast(transform.position, Vector3.down, out hit, 1, CoKnos.RayCastForBounds))
                    return false;
                GameObject hitGameObject = hit.collider.gameObject;
                if (IsAttachedToPlayer)
                    if (hitGameObject.GetComponent<SlimeCube>() &&
                        !hitGameObject.GetComponent<SlimeCube>().IsAttachedToPlayer)
                        return true;
                if (TriggerRelay != null)
                    if (hitGameObject.GetComponent<SlimeCube>() &&
                        hitGameObject.GetComponent<SlimeCube>().TriggerRelay != TriggerRelay)
                        return true;
                if (hitGameObject.GetComponent<Obstacle>())
                    return true;
                return false;
            }
        }

        #endregion

        #region Events

        private void OnTriggerEnter(Collider other)
        {
            AdvDebug.Log($"Had Trigger with {other}, {other.gameObject}, {other.gameObject.GetComponent<Player>()}",
                LogLevel.Verbose);
            if (TriggerRelay != null)
                TriggerRelay
                    .OnTriggerEnter(other); //This relays the trigger to the player, as if the player had triggered it
            else
            {
                AdvDebug.Log($"{this} has been triggered by {other.gameObject.name}", LogType.Collision,
                    LogLevel.Debug);
            }
        }

        #endregion

        #region Called by TriggerRelay

        public RaycastHit? RayCastFromThis(Vector3 direction, float distance, bool OnlyHitBOunds = true)
        {
            if (!OnlyHitBOunds)
            {
                RaycastHit hit;
                if (Physics.Raycast(transform.position, direction, out hit, distance))
                {
                    AdvDebug.Log($"Raycast from {this} hit {hit.collider.gameObject.name}", LogType.RayCast,
                        LogLevel.Verbose);
                    return hit;
                }

                return null;
            }
            else
            {
                RaycastHit hit;
                if (Physics.Raycast(transform.position, direction, out hit, distance, CoKnos.RayCastForBounds))
                {
                    AdvDebug.Log($"Raycast from {this} hit {hit.collider.gameObject.name}", LogType.RayCast,
                        LogLevel.Verbose);
                    return hit;
                }

                return null;
            }
        }

        public Vector3Int
            GetLowestPointInDirection(
                Vector2Int vector2Int) //This is written like this to enable cubys with other shapes than cubes
        {
            Vector3 pos = TriggerRelay.transform.rotation * transform.localPosition;
            return GLUtil.Vector3ToInt(pos);
        }

        public bool EdgeGrounded(Vector2Int vector2Int)
        {
            RaycastHit hit;
            if (!Physics.Raycast(transform.position, Vector3.down + new Vector3(vector2Int.x, 0, vector2Int.y), out hit,
                    Mathf.Sqrt(2.0f), CoKnos.RayCastForBounds))
                return false;
            GameObject hitGameObject = hit.collider.gameObject;
            if (IsAttachedToPlayer)
                if (hitGameObject.GetComponent<SlimeCube>() &&
                    !hitGameObject.GetComponent<SlimeCube>().IsAttachedToPlayer)
                    return true;
            if (TriggerRelay != null)
                if (hitGameObject.GetComponent<SlimeCube>() &&
                    hitGameObject.GetComponent<SlimeCube>().TriggerRelay != TriggerRelay)
                    return true;
            if (hitGameObject.GetComponent<Obstacle>())
                return true;
            return false;
        }


        public void RelayHoldAll()
        {
            TriggerRelay.HoldAll();
        }

        #endregion

        public void OnClick()
        {
            Player player = null;
            AdvDebug.Log($"Clicked on {this}", LogType.Click, LogLevel.Debug);
            if (ClickRelay != null)
            {
                ClickRelay.RelayClick(this);
            }
            else
            {
                List<RaycastHit> hits = new List<RaycastHit>();
                hits.Clear();
                for (int i = 0; i < 6; i++)
                {
                    RaycastHit hit;
                    if (Physics.Raycast(transform.position, GLUtil.Dir6Gen(i), out hit,
                            1)) //TODO Test this
                    {
                        hits.Add(hit);
                    }
                }

                foreach (RaycastHit raycastHit in hits)
                {
                    if (raycastHit.transform.TryGetComponent<SlimeCube>(out SlimeCube slimeCube))
                    {
                        if (slimeCube.IsAttachedToPlayer)
                        {
                            player = (Player)slimeCube.TriggerRelay;
                            break;
                        }
                    }

                    if (raycastHit.transform.TryGetComponent<Player>(out Player playeri))
                    {
                        player = playeri;
                    }
                }

                if (player != null)
                {
                    player.AttachMe(this);
                }

                foreach (RaycastHit hit in hits)
                {
                    AdvDebug.Log($"Hit {hit.rigidbody}", LogType.RayCast, LogLevel.Verbose);
                }
            }
        }

        bool _isHovered = false;
        int HoverDecay = 0;
        public void OnHover()
        {
            if (ClickRelay != null)
            {
                ClickRelay.RelayHover(this);
            }
            else
            {
                SetHover();
            }
        }

        public void SetHover()
        {
            if(!_isHovered)
                _shape.colour = CoKnos.SlimeHovered;
            _isHovered = true;
            HoverDecay = 2;
        }
        private void Update()
        {
            if (_isHovered)
            {
                HoverDecay--;
                if (HoverDecay <= 0)
                {
                    _isHovered = false;
                    HoverDecay = 0;
                    _shape.colour = CoKnos.SlimeColor;
                }
            }
        }

        public string GetInfo()
        {
            return $"TriggerRelay set to {TriggerRelay}";
        }
    }
}