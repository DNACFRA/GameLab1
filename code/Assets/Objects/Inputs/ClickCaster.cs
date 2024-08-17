using Entities.Scripts.Utils;
using UnityEngine;
using UnityEngine.UIElements;
using LogType = Entities.Scripts.Utils.LogType;

namespace Objects.Inputs
{
    [RequireComponent(typeof(Camera))]
    public class ClickCaster : MonoBehaviour
    {
        Camera _cam;

        void Start()
        {
            _cam = GetComponent<Camera>();
        }

        // Update is called once per frame
        void Update()
        {
            

            
            Debug.Log("Clicked");
                Ray ray = _cam.ScreenPointToRay(Input.mousePosition);
                RaycastHit[] results = new RaycastHit[1];
                var size = Physics.RaycastNonAlloc(ray, results, float.MaxValue, LayerMask.GetMask("Clickable"));
                if(size==0)
                    return;
                IClickableObject clickableObject = results[0].rigidbody.GetComponent<IClickableObject>();
if(clickableObject!=null)
                clickableObject.OnHover();
                if (clickableObject != null)
                {
                    if(Input.GetMouseButtonDown(0))
                    clickableObject.OnClick();
                }
                else
                {
                    AdvDebug.LogWarning(
                        $"Object {results[0]} in layerMask, but not implementing the ClickableObject Interface");
                }
            
        }
    }
}