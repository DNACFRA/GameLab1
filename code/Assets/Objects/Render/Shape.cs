using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

namespace Objects.Render
{
    public class Shape : MonoBehaviour
    {
        public enum ShapeType
        {
            Sphere,
            Cube,
            Aab, //Axis Aligned Box
        };

        public enum Operation
        {
            None,
            Blend,
            Cut,
            Mask
        }

        public ShapeType shapeType;
        public Operation operation;
        public Color colour = Color.white;
        [Range(0, 1)] public float blendStrength;
        public int layer;
        public int Parent;
        public Shape[] children;

        public Vector3 Position => transform.position;

        public Vector4 Rotation
        {
            get
            {
                var rotation = transform.rotation;
                return new Vector4(rotation.x, rotation.y, rotation.z, rotation.w);
            }
        }

        public Vector3 Scale
        {
            get
            {
                Vector3 parentScale = Vector3.one;
                if(false)
                if (transform.parent != null && transform.parent.GetComponent<Shape>() != null)
                {
                    parentScale = transform.parent.GetComponent<Shape>().Scale;
                }

                return Vector3.Scale(transform.localScale / 2f, parentScale);
            }
            set => transform.localScale = value * 2f;
        }

        public Vector4 InverseRotation
        {
            get
            {

                var rotation = Quaternion.Inverse(transform.rotation);
                return new Vector4(rotation.x, rotation.y, rotation.z, rotation.w);

            }
        }
    }
}
/*MIT License

Copyright (c) 2020 Dominik Zimny                                                        //But modified by me

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

//TODO how to if this is the right way to do licence