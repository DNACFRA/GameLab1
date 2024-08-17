using System.Collections;
using System.Collections.Generic;
using Objects.EditorChanges;
using Objects.Enviorment;
using Objects.Render;
using UnityEngine;

namespace Objects.Engine
{
    /// <summary>
    /// Keeps track of completed scenes
    /// </summary>
    public class Progression : MonoBehaviour, IRelayStart
    {
        // Start is called before the first frame update

        [SerializeField,ReadOnlyField]

        List<string> completedScenes = new List<string>();
        public void RunRelayStart()
        {
            CoKnos.OnSceneLoadEvent += OnScenLoaded;
            CoKnos.OnSceneCompletionEvent += OnSceneCompletion;
        }

        private void OnSceneCompletion(string scenename)
        {
            foreach (string scene in completedScenes)
            {
                if (scene == scenename)
                {
                    return;
                }
            }
            completedScenes.Add(scenename);
        }


        // Update is called once per frame
        void OnScenLoaded(string sceneName)
        {
            
            StartCoroutine(waitaframe());
            IEnumerator waitaframe()
            {
                yield return null;
                if (sceneName == "The Hub")
                {
                    Debug.Log("The Hub is loaded");
                    GameObject[] goals = GameObject.FindGameObjectsWithTag("Goal");
                    foreach (GameObject goal in goals)
                    {
                        Goal goalScript = goal.GetComponent<Goal>();
                        if (completedScenes.Contains(goalScript.sceneToLoad))
                        {
                            goalScript._shape.layer = 60;
                            goalScript._shape.blendStrength = 0.1f;
                            goalScript._shape.operation = Shape.Operation.Blend;
                        }
                    }
                }
            }
        }
    }
}
