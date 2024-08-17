using System;
using System.Collections;
using Entities.Scripts.Utils;
using Objects.Engine;
using Objects.Render;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace Objects.Enviorment
{
    [RequireComponent(typeof(Collider))]
    public class Goal : MonoBehaviour
    {
        public Collider _collider;
        public Shape _shape;
        [SerializeField] public string sceneToLoad;
        private void Start()
        {
            _collider = GetComponent<Collider>();
            _shape = this.GetComponent<Shape>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.GetComponent<Player.Player>())
            {
                AdvDebug.Log($"{other.gameObject}, reached the goal");

                CoKnos.OnSceneCompletionEvent?.Invoke(SceneManager.GetActiveScene().name);
                CoKnos.animationMorphHandler
                    .MorphLoad(this.gameObject, _shape, _collider, sceneToLoad);
                //Loader.LoadScene(sceneToLoad);
            }
        }


    }
}