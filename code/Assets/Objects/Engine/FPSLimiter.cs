using UnityEngine;

namespace Objects.Engine
{
    [ExecuteInEditMode]
    public class FPSLimiter : MonoBehaviour
    {
        [SerializeField] private int targerFPS = 60;
    
        //Prevent Unity from setting fire to my PC when using in Editor 
        void Start()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = targerFPS;
        }
    }
}
