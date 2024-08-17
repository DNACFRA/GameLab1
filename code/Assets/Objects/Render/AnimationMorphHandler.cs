using System;
using System.Collections;
using Objects.Engine;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Objects.Render
{
    [RequireComponent(typeof(NotDestroyHandler))]
    public class AnimationMorphHandler : MonoBehaviour, IRelayStart
    {
        private GameObject gameObjectin;
        [SerializeField] private bool loadNextScene;
        private Shape shape;
        private Collider incollider;
        private bool LoadNextScene = false;
        private string sceneToLoad;

        public void RunRelayStart()
        {
            StartCoroutine(LoadSceneWithAnimation());
            CoKnos.animationMorphHandler = this;
        }

        public void MorphLoad(GameObject gameObjectin, Shape shape, Collider incollider, string sceneToLoad)
        {
            this.gameObjectin = gameObjectin;
            this.shape = shape;
            this.incollider = incollider;
            this.sceneToLoad = sceneToLoad;
            LoadNextScene = true;
        }

        IEnumerator LoadSceneWithAnimation()
        {
            while (true)
            {
                while (!LoadNextScene)
                    yield return null;
                Destroy(CoKnos.Player.gameObject);
                DontDestroyOnLoad(gameObjectin);
                var transform1 = Camera.allCameras[0].transform;
                CoKnos.Player.enabled = false;
                shape.blendStrength = 1f;
                incollider.enabled = false;
                shape.shapeType = Shape.ShapeType.Sphere;
                shape.layer = 50;
                shape.operation = Shape.Operation.Blend;
                Vector3 startPos = shape.transform.position;

                CanvasScaler canvasScaler = null;
                GameObject[] gameObjects = GameObject.FindGameObjectsWithTag("UIForFade");
                if (gameObjects.Length > 0)
                    canvasScaler = gameObjects[0].GetComponent<CanvasScaler>();
                for (float i = 0; i < 1; i += Time.deltaTime * 0.5f)
                {
                    shape.Scale = Vector3.Lerp(Vector3.one / 10, Vector3.one * 10, i);
                    shape.blendStrength = Mathf.Lerp(1, 0f, i);
                    shape.transform.position = Vector3.Lerp(startPos,
                        transform1.position + transform1.rotation * (Vector3.forward * 16), i);
                    
                    if (canvasScaler != null)
                        canvasScaler.referenceResolution = Vector2.Lerp(new Vector2(800, 600), new Vector2(200, 60), i);
                    yield return null;
                }

                AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneToLoad);

                // Wait until the asynchronous scene fully loads
                while (!asyncLoad.isDone)
                {
                    yield return null;
                }

                CoKnos.OnSceneLoadEvent.Invoke(sceneToLoad);
                var transform2 = Camera.allCameras[0].transform;
                canvasScaler = null;
                gameObjects = GameObject.FindGameObjectsWithTag("UIForFade");
                if (gameObjects.Length > 0)
                canvasScaler = gameObjects[0].GetComponent<CanvasScaler>();
                
                gameObjectin.transform.position =
                    transform2.position + transform2.rotation * (Vector3.forward * 16);
                Vector3 startPos2 = shape.transform.position;
                for (float i = 1; i > 0; i -= Time.deltaTime * 0.5f)
                {
                    shape.Scale = Vector3.Lerp(Vector3.zero / 10, Vector3.one * 10, i);
                    shape.blendStrength = Mathf.Lerp(1, 0f, i);
                    if (canvasScaler != null)
                        canvasScaler.referenceResolution = Vector2.Lerp(new Vector2(800, 600), new Vector2(200, 60), i);
                    yield return null;
                }

                Destroy(gameObjectin);
                LoadNextScene = false;
            }
        }
    }
}