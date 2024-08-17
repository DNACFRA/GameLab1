using System;
using System.Collections.Generic;
using Entities.Scripts.Utils;
using Objects.Engine;
using Objects.Inputs;
using Unity.VisualScripting;
using UnityEngine;

namespace Objects.Player.SubObjects
{
    /// <summary>
    /// This is representation of a / multiple SlimeCubes that have been detached from the Player, they will act as a single Object and not as single entities
    /// This is no GameObject, but just a Class, a bit scuffed at times, but it works
    /// </summary>
    public class MergedSlimeCubes : IClickRelay, TriggerRelay
    {
        public readonly List<SlimeCube> MySlimeCubes;

        public MergedSlimeCubes(List<SlimeCube> inSlimes)
        {
            MySlimeCubes = inSlimes;
            foreach (SlimeCube mySlimeCube in MySlimeCubes)
            {
                mySlimeCube.ClickRelay = this;
                mySlimeCube.TriggerRelay = this;
            }

            EndCheck();
        }

        public bool IsGrounded
        {
            get
            {
                foreach (SlimeCube t in MySlimeCubes)
                {
                    if (t.IsGrounded)
                        return true;
                }

                return false;
            }
        }

        private float _speed = 0f;
        private const float Acceleration = 10f;

        private void FallIni()
        {
            UpdateHandler.CallMeOnUpdateList += FallUpdate;
            _speed = 0;
        }

        private void FallUpdate(float deltaTime)
        {
            _speed += deltaTime * Acceleration;
            foreach (SlimeCube mySlimeCube in MySlimeCubes)
            {
                mySlimeCube.transform.position -= new Vector3(0, _speed * deltaTime, 0);
            }
        }

        private void FallEnd()
        {
            UpdateHandler.CallMeOnUpdateList -= FallUpdate;
            foreach (SlimeCube mySlimeCube in MySlimeCubes)
            {
                Vector3 position = mySlimeCube.transform.position;
                mySlimeCube.transform.position = new Vector3(position.x, Mathf.CeilToInt(position.y), position.z);
            }
        }

        public void HoldAll()
        {
            try
            {
                UpdateHandler.CallMeOnUpdateList -= FallUpdate;
            }
            catch (Exception e)
            {
                Console.WriteLine(e + " FallUpdate already removed from UpdateList");
            }
        }

        private void EndCheck()
        {
            if (!IsGrounded)
            {
                FallIni();
            }
            else
            {
                // actionAble = true; TODO
            }

            foreach (SlimeCube slim in MySlimeCubes)
            {
                GLUtil.SnapTransform(slim.transform);
            }
        }
/// <summary>
/// A Slime Cube has been clicked, now we need to check if we the Player can merge with us
/// </summary>
/// <param name="clickedSlimeCube">The Clicked SlimeCube</param>
        public void RelayClick(SlimeCube clickedSlimeCube)
        {
            Player player = null;
            List<RaycastHit> hits = new List<RaycastHit>();

            for (int i = 0; i < 6; i++)
            {
                RaycastHit? hit;
                hit = clickedSlimeCube.RayCastFromThis(GLUtil.Dir6Gen(i), 1, false); //TODO Test this
                if (hit != null)
                {
                    hits.Add((RaycastHit)hit);
                }
            }

            foreach (RaycastHit raycastHit in hits)
            {
                if (raycastHit.transform.TryGetComponent<SlimeCube>(out SlimeCube slimey))
                {
                    if (slimey.IsAttachedToPlayer)
                    {
                        player = (Player)slimey.TriggerRelay;
                        break;
                    }
                }

                if (raycastHit.transform.TryGetComponent(out Player playeri))
                {
                    player = playeri;
                }
            }

            if (player != null)

            {
                player.AttachMe(clickedSlimeCube, this);
            }
        }

public void RelayHover(SlimeCube slimeCube)
{
    
    slimeCube.SetHover();
    return;
    foreach (SlimeCube mySlimeCube in MySlimeCubes)
    {
        mySlimeCube.SetHover();
    }
    
}

/// <summary>
        /// If i Collide with something, i will stop falling
        /// Called by the owned Slimecubes
        /// </summary>
        /// <param name="other">The Thing i collided with</param>
        public void OnTriggerEnter(Collider other)
        {
            FallEnd();
        }
    }
}