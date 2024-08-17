#if (UNITY_EDITOR)
using System.Collections.Generic;
using Entities.Scripts.Utils;
using JetBrains.Annotations;
using Objects.Engine;
using Objects.Enviorment;
using Objects.Player.SubObjects;
using Objects.Render;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;
using UnityEngine.SceneManagement;
using LogType = Entities.Scripts.Utils.LogType;
using Object = UnityEngine.Object;

// This a collection of Unity engine edits, which wont be used in the build
namespace Objects.EditorChanges
{
    
    [System.AttributeUsage(System.AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public class ReadOnlyFieldAttribute : PropertyAttribute
    {
    }

    [UsedImplicitly, CustomPropertyDrawer(typeof(ReadOnlyFieldAttribute))]
    public class ReadOnlyFieldAttributeDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            => EditorGUI.GetPropertyHeight(property, label, true);

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            GUI.enabled = false;
            EditorGUI.PropertyField(position, property, label, true);
            GUI.enabled = true;
        }
    }


    //makes _UndoStack visible in the inspector by turning it into an array
    [CustomEditor(typeof(Player.Player))]
    class PlayerEditor : Editor
    {
        //get the player object
        private Player.Player Player => (Player.Player)target;

        public override void OnInspectorGUI()
        {
            if (Player.Currentaction != null)
                EditorGUILayout.LabelField(Player.Currentaction.Log());
            DrawDefaultInspector();
            if (GUILayout.Button("LogUndoStack"))
                Player.LogUndoStack();
            //read the array from the player object and display in the inspector what kind of classes are in it
            foreach (var undo in Player.UndoArray)
            {
                EditorGUILayout.LabelField(undo.Log()); //OPTIMIZE this, it's not pretty and is missing a lot of info
            }
        }
    }


    [CustomEditor(typeof(SlimeCube))]
    class SlimeEditor : Editor
    {
        private SlimeCube SlimeCube => (SlimeCube)target;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if (GUILayout.Button("Print Info"))
                AdvDebug.Log(SlimeCube.GetInfo());
        }
    }

/// <summary>
/// Editor Changes for Debugging of the Master Class
/// </summary>
    [CustomEditor(typeof(Master))]
    class MasterEditor : Editor
    {
        private Master _master => (Master)target;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if (GUILayout.Button("Run Voxeliser"))
            {
                _master.RunVoxeliser();
            }

            if (GUILayout.Button("CheckFull Readback Walk-ability"))
            {
                _master.CeckFullWalkability();
            }
        }
    }



    [EditorTool("GridSnap", typeof(SnapAble))]
    class HexSnapTool : EditorTool, IDrawSelectedHandles
    {
        public void OnDrawHandles()
        {
            //foreach (Object o in targets)
            Object o = target;
            {
                if (o is SnapAble snapAble)
                {
                    AdvDebug.Log($"Snap-able something something {snapAble}", LogType.InspectorEdits, LogLevel.Verbose);
                }
                else
                {
                    return;
                }


                EditorGUI.BeginChangeCheck(); //Called to activate Check for changes
                Vector3 vector3 = Handles.PositionHandle(snapAble.Position, Quaternion.identity);


                if (EditorGUI.EndChangeCheck()) // Have there been changes since The call o fBeginChangeCheck
                {
                    AdvDebug.Log(
                        $"Snap-able currently at: {snapAble.gameObject.transform.position}, handles have moved to: " +
                        $"{vector3}", LogType.InspectorEdits, LogLevel.Verbose);
                    Vector3 diff = vector3 - (snapAble).transform.position;
                    foreach (Object o1 in targets)
                    {
                        if (o1 is SnapAble s1)
                        {
                            //Undo.RecordObject(snapAble.gameObject, "Set Cube Positions");

                            s1.transform.position = GLUtil.Vector3ToInt(s1.transform.position += diff);
                        }
                    }

                    //Undo.RecordObject(snapAble.gameObject, "Set Cube Positions");

                    //snapAble.gameObject.transform.position = GLUtil.Vector3ToInt(vector3);
                }
            }
        }

        public override void OnToolGUI(EditorWindow window)
        {
            if (!(window is SceneView))
                return;

            Handles.BeginGUI();
            using (new GUILayout.HorizontalScope())
            {
                using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    // testToggle = EditorGUILayout.Toggle("TestToggle", testToggle);
                    // To animate platforms we need the Scene View to repaint at fixed intervals, so enable `alwaysRefresh`
                    // and scene FX (need both for this to work). In older versions of Unity this is called `materialUpdateEnabled`
/*
                    if (GUILayout.Button("Test Button")) 
                        AdvDebug.Log("Test Button pressed");
                        */
                }

                GUILayout.FlexibleSpace();
            }

            Handles.EndGUI();
        }
    }
}
#else
using UnityEngine;

namespace Objects.EditorChanges
{
public class ReadOnlyFieldAttribute : PropertyAttribute
        {
        }
}
#endif