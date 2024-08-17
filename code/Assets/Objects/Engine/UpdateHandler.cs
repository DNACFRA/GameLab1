using System;
using UnityEngine;

namespace Objects.Engine
{
    /// <summary>
    /// Just handles some Update Calls, DuctTape for now
    /// </summary>
    public class UpdateHandler : MonoBehaviour
    {
        public delegate void CallMeOnUpdate(float deltatime);

        public static CallMeOnUpdate CallMeOnUpdateList;

        private void Update()
        {
            CallMeOnUpdateList?.Invoke(Time.deltaTime);
        }
    }
}