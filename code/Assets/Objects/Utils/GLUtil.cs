using System;
using System.ComponentModel;
using System.Xml;
using UnityEngine;

namespace Entities.Scripts.Utils
{
    public static class GLUtil
    {
        public enum Direction
        {
            Right,
            Up,
            Left,
            Down
        }

        public static GLUtil.Direction Vector2Enum(Vector2 vector2)
        {
            int temp = Mathf.RoundToInt(Mathf.Atan2(vector2.y, vector2.x)/2/Mathf.PI*4);
            return (Direction)(temp < 0 ? temp + 4 : temp);
        }

        
        
        public static Vector2Int Vector2Int(Vector2 vector2)
        {
            switch (Vector2Enum(vector2))
            {
                case Direction.Right: return new Vector2Int(1, 0);
                case Direction.Up: return new Vector2Int(0, 1);
                case Direction.Left: return new Vector2Int(-1, 0);
                case Direction.Down: return new Vector2Int(0, -1);
                default: throw new WarningException("Error this that");
            }
        }
//Snaps the Rotation to the nearest 90°
        public static Quaternion SnapRotation(Quaternion inQuaternion)
        {
            Vector3 euler = inQuaternion.eulerAngles;
            euler.x = Mathf.Round(euler.x / 90) * 90;
            euler.y = Mathf.Round(euler.y / 90) * 90;
            euler.z = Mathf.Round(euler.z / 90) * 90;
            return Quaternion.Euler(euler);
        }
        public static Vector3Int Vector3ToInt(Vector3 vector3)
        {
            return new Vector3Int(Mathf.RoundToInt(vector3.x), Mathf.RoundToInt(vector3.y),
                Mathf.RoundToInt(vector3.z));
        }
        
        public static void SnapTransform(Transform transform)
        {
            transform.position = Vector3ToInt(transform.position);
            transform.rotation = SnapRotation(transform.rotation);
        }

        public static Vector3 Dir8Gen(int j)
        {
            // ReSharper disable twice PossibleLossOfFraction
            return new Vector3((j % 2) - 0.5f, ((j / 2) % 2) - 0.5f, ((j / 4) % 2) - 0.5f)*2;
        }

        
        public static Vector3 Dir6Gen(int index)
            {
                switch (index)
                {
                    case 0:
                        return Vector3.forward.normalized;
                    case 1:
                        return Vector3.back.normalized;
                    case 2:
                        return Vector3.left.normalized;
                    case 3:
                        return Vector3.right.normalized;
                    case 4:
                        return Vector3.up.normalized;
                    case 5:
                        return Vector3.down.normalized;
                    default:
                        AdvDebug.LogError("Invalid index. Must be between 0 and 5.");
                        return Vector3.zero;
                }
            
        }

    }
}