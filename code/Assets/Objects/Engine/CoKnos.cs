using Objects.Render;
using UnityEngine;

namespace Objects.Engine
{
    /// <summary>
    /// Has static variables that are used in multiple places
    /// </summary>
    public static class CoKnos
    {
        public const float BlendStrengthConnceted = 0.1f;
        public const float BlendStrengthDisConnceted = 0.015f;
        public static LayerMask RayCastForBounds= LayerMask.GetMask("Clickable","Default");
        public static int SlimeBlockConnectedLayer = 4;
        public static int SlimeBlockDisConnectedLayer = 5;
        public static AudioHandler AudioHandler { get; set; }
        
        public static Color SlimeColor = new Color(01f, 0.0f, 0.0f, 1f);
        public static Color SlimeHovered = new Color(1f, 0.5f, 0.5f, 1f);
        public static Player.Player Player { get; set; }
        public static NotDestroyHandler NotDestroyHandler;
        public static AnimationMorphHandler animationMorphHandler;
        public delegate void OnSceneLoad(string sceneName);
        public static OnSceneLoad OnSceneLoadEvent;
        
        public delegate void OnSceneCompletion(string sceneName);
        public static OnSceneCompletion OnSceneCompletionEvent;
    }
}