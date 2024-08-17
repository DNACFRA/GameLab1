using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace Objects.Engine
{
    public class CameraMove : MonoBehaviour
    {
    
        /// <summary>
        /// Debug script to move the camera around
        /// Excluded from build
        /// </summary>
#if UNITY_EDITOR
        // Start is called before the first frame update
        [FormerlySerializedAs("Move")] [SerializeField] private InputAction move;

        private void OnEnable()
        {
            move.Enable();
        }

        private void OnDisable()
        {
            move.Disable();
        }

        void Start()
        {
        
        }

        // Update is called once per frame
        void Update()
        {
            transform.position += move.ReadValue<Vector3>()* Time.deltaTime * 10;
        }
#endif
    
    }
}
