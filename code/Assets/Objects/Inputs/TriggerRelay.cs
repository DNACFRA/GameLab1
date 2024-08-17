using System;
using Entities.Scripts.Utils;
using UnityEngine;

namespace Objects.Inputs
{
    public interface TriggerRelay
    {
        Transform transform
        {
            get
            {
                AdvDebug.LogWarning("Triggerrelay just got asked for transform, this shouldnt happen ever");
                throw new NotImplementedException();
            }
        }

        public abstract void OnTriggerEnter(Collider other);

        public void HoldAll();
    }
}