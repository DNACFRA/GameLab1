using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Entities.Scripts;
using Entities.Scripts.Utils;
using Objects.EditorChanges;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using LogType = Entities.Scripts.Utils.LogType;

namespace Objects.Render
{
    public class Master : MonoBehaviour
    {
        [FormerlySerializedAs("raymarching")] [FormerlySerializedAs("raymarchingRender")]
        public ComputeShader rayMarching;

        Camera _cam;
        private RenderTexture _target;
        private List<ComputeBuffer> _buffersToDispose;
        private Light _lightSource;

        [SerializeField]
        private bool firstFrame = true;

        [SerializeField] private List<WhatToRun> whatToRun;
[SerializeField,ReadOnlyField] private int rmx = 8;
        [SerializeField,ReadOnlyField] private int rmy = 8;
        [SerializeField,ReadOnlyField] private int rmz = 1;
      
        [SerializeField] private int threadGroupSize = 1;
        [SerializeField,ReadOnlyField] private int lrmx = 1;
        [SerializeField,ReadOnlyField] private int lrmy = 1;
        [SerializeField,ReadOnlyField] private int lrmz = 1;
        [SerializeField,ReadOnlyField] private int MapSize = 32;
      

        [SerializeField] private GameObject readBackDataDisplayObject;

        [SerializeField] private int autobreak = 10;

        private SDF[] _readback = new SDF[] { };

        
        [SerializeField] private int voxResolution = 512;

         [SerializeField,ReadOnlyField] private RenderTexture StarsRT;
        [SerializeField] private Texture Stars;
        

        private enum WhatToRun
        {
            RayMarcher,
            Voxiliser,
            ColorStep,
            VoxelMarcher,
            LimitedRayMarcher,
            Nothing, // This for easier deselection in the inspector
            DataReadBackAnalyser,
            FindFaultyData,
        }

        private ComputeBuffer _readVoxelBuffer;

        [SerializeField] private bool ShaderReadBack;
        List<GameObject> readBackDisplay = new List<GameObject>();

        /// <summary>
        /// Initialize all the buffers and variables needed for the shaders
        /// Every Frame the Shaders are just overriden with the new data
        /// </summary>
        protected void Start()
        {
            _readVoxelBuffer = MakeSDFBuffer(voxResolution);
            IniStars();
        }

        private void IniStars()
        {
            if (StarsRT == null)
            {
                StarsRT = new RenderTexture(Stars.width, Stars.height, 0, RenderTextureFormat.ARGBFloat);
                Graphics.Blit(Stars, StarsRT);
            }
            
        }

        /// <summary>
/// Clear all Shaders and buffers on exit to avoid memory leaks
/// </summary>
        private void OnDestroy()
        {
            _readVoxelBuffer.Dispose();
        }
/// <summary>
/// Gets current camera and light source to use in Raymarch
/// </summary>
        private void InitLightCam()
        {
            _cam = Camera.current;
            _lightSource = FindObjectOfType<Light>();
        }
        
        

        public void RunVoxeliser()
        {
            SetShapeData(rayMarching, 1);
            ComputeBuffer needWriteSpace = new ComputeBuffer(threadGroupSize * lrmx * lrmy * lrmz, sizeof(int));
            needWriteSpace.SetData(new byte[] { });

            ComputeBuffer readPointer = new ComputeBuffer(1, sizeof(int));
            readPointer.SetData(new[] { 0 });

            ComputeBuffer writePointer = new ComputeBuffer(1, sizeof(int));
            writePointer.SetData(new[] { 1 });

            ComputeBuffer threadsNeeded = new ComputeBuffer(1, sizeof(int));
            threadsNeeded.SetData(new[] { 1 });
            int i = this.autobreak;
            rayMarching.SetBuffer(1, "WritePointer", writePointer);
            rayMarching.SetBuffer(1, "ThreadsNeeded", threadsNeeded);
            rayMarching.SetBuffer(1, "VoxelSDFBuffer", _readVoxelBuffer);
            rayMarching.SetBuffer(1, "NeedWriteSpace", needWriteSpace);
            rayMarching.SetBuffer(1, "ReadPointer", readPointer);
            rayMarching.SetInt("BufferLength", _readVoxelBuffer.count);
            rayMarching.SetInt("ThreadGroupsX", lrmx);
            rayMarching.SetInt("ThreadGroupsY", lrmy);

            int[] NeedCounter = new int[autobreak];
            while (true)
            {
                rayMarching.Dispatch(1, lrmx, lrmy, lrmz);
                int[] breakCondition = new int[1];
                threadsNeeded.GetData(breakCondition);
                NeedCounter[i % autobreak] = breakCondition[0];
                if (breakCondition[0] == 0 || --i < 1)
                    break;
            }


            //rayMarching.DispatchIndirect(1, _ReadvoxelBuffer, 0);
            readPointer.Dispose();
            writePointer.Dispose();
            threadsNeeded.Dispose();
            needWriteSpace.Dispose();
            if (ShaderReadBack)
            {
                _readback = new SDF[voxResolution];
                _readVoxelBuffer.GetData(_readback);
                // Debug.Log(_readback[0].sdf);
                float g = _readback[0].sdf;
            }
        }

        protected void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            InitLightCam();
            _buffersToDispose = new List<ComputeBuffer>();


            if (whatToRun.Contains(WhatToRun.RayMarcher))
            {
                InitRenderTexture();
                SetShapeData(rayMarching);
                SetSceneInfo(rayMarching);
                rayMarching.SetTexture(0, "Source", source);
                rayMarching.SetTexture(0, "Destination", _target);
                
                int threadGroupsX =
                    Mathf.CeilToInt(_cam.pixelWidth / (float)rmx); //TODO This is probably stupid
                int threadGroupsY = Mathf.CeilToInt(_cam.pixelHeight / (float)rmy);
                rayMarching.Dispatch(0, threadGroupsX, threadGroupsY, 1);
                Graphics.Blit(_target, destination);
            }
            else if (whatToRun.Contains(WhatToRun.ColorStep))
            {
                InitRenderTexture();
                SetShapeData(rayMarching, 3);
                SetSceneInfo(rayMarching);
                rayMarching.SetTexture(3, "Source", source);
                rayMarching.SetTexture(3, "Destination", _target);
                int threadGroupsX =
                    Mathf.CeilToInt(_cam.pixelWidth / (float)rmx); //TODO This is probably stupid
                int threadGroupsY = Mathf.CeilToInt(_cam.pixelHeight / (float)rmy);
                rayMarching.Dispatch(3, threadGroupsX, threadGroupsY, 1);
                Graphics.Blit(_target, destination);
            }
            else
            {
                Graphics.Blit(source, destination);
            }


            if (whatToRun.Contains(WhatToRun.Voxiliser))
            {
                RunVoxeliser();
            }


            if (whatToRun.Contains(WhatToRun.DataReadBackAnalyser))
            {
                foreach (GameObject o in readBackDisplay)
                {
                    Destroy(o);
                }

                foreach (SDF sdf in _readback)
                {
                    if (sdf.mipMapLevel < 3)
                        continue;
                    GameObject o = Instantiate(readBackDataDisplayObject);
                    o.transform.position = sdf.position;
                    float vectemp = MapSize / Mathf.Pow(2, sdf.mipMapLevel + 1);
                    o.transform.localScale = new Vector3(vectemp, vectemp, vectemp);
                    readBackDisplay.Add(o);
                }
            }

            if (whatToRun.Contains(WhatToRun.FindFaultyData))
            {
                int expectedUpdates = _readback[0].HasBeenUpdated;
                StringBuilder str = new StringBuilder();
                for (int i = 0; i < _readback.Length; i++)
                {
                    SDF sdf = _readback[i];
                    if (sdf.HasBeenUpdated != expectedUpdates) str.Append($" - {i}U: {sdf.HasBeenUpdated} ");
                    if (sdf.parent < 0) str.Append($" - {i}P: {sdf.parent}");
                    if (sdf.child1 >= voxResolution) str.Append($" - {i}C1: {sdf.child1}");
                }

                AdvDebug.Log($"Expected {expectedUpdates} " + str.ToString(), LogType.Raymarch, LogLevel.Inform);
            }


            void VoxelAssistedMarchers(int kernel)
            {
                if (firstFrame)
                {
                    firstFrame = false;
                    return;
                }

                InitRenderTexture();
                rayMarching.SetBuffer(kernel, "VoxelSDFBuffer", _readVoxelBuffer);
                SetShapeData(rayMarching, kernel);
                SetSceneInfo(rayMarching);
                rayMarching.SetTexture(kernel, "Source", source);
                rayMarching.SetTexture(kernel, "Destination", _target);
                rayMarching.SetTexture(kernel, "Stars", StarsRT);
                int threadGroupsX =
                    Mathf.CeilToInt(_cam.pixelWidth / (float)rmx); //TODO This is probably stupid
                int threadGroupsY = Mathf.CeilToInt(_cam.pixelHeight / (float)rmy);
                rayMarching.Dispatch(kernel, threadGroupsX, threadGroupsY, 1);
                Graphics.Blit(_target, destination);
            }

            if (whatToRun.Contains(WhatToRun.VoxelMarcher))
            {
                VoxelAssistedMarchers(2);
            }

            if (whatToRun.Contains(WhatToRun.LimitedRayMarcher))
            {
                VoxelAssistedMarchers(4);
            }

            foreach (var buffer in _buffersToDispose)
            {
                buffer.Dispose();
            }
        }

/// <summary>
/// Test function to check for Errors in Voxelisation Process
/// (which where found with this)
/// </summary>
        public void CeckFullWalkability()
        {
            int count;
            List<uint> checkList = new List<uint>();
            AdvDebug.Log($"Found Total of {Walkstep(0, 0)}");

            int Walkstep(uint point, uint caller)
            {
                SDF sdf = _readback[point];
                if (checkList.Contains(point))
                {
                    AdvDebug.LogWarning($"Duplicate Child {point} found at {caller}");
                    return 0;
                }
                else
                {
                    checkList.Add(point);
                }

                if (sdf is { child1: 0, child2: 0, child3: 0, child4: 0, child5: 0, child6: 0, child7: 0, child8: 0 })
                {
                    return 1;
                }

                return Walkstep(sdf.child1, point) +
                       Walkstep(sdf.child2, point) +
                       Walkstep(sdf.child3, point) +
                       Walkstep(sdf.child4, point) +
                       Walkstep(sdf.child5, point) +
                       Walkstep(sdf.child6, point) +
                       Walkstep(sdf.child7, point) +
                       Walkstep(sdf.child8, point);
            }
        }

/// <summary>
/// Makes a Computer Buffer for the SDF Struct
/// </summary>
/// <param name="size">Size of the SDFBuffer</param>
/// <returns>SDFBuffer</returns>
        protected ComputeBuffer MakeSDFBuffer(int size)
        {
            SDF[] buffer = new SDF[size];
            uint[] mask = new uint[4];
            mask[0] = 0xFFFFFFFF;
            mask[1] = 0xFFFFFFFF;
            mask[2] = 0xFFFFFFFF;
            mask[3] = 0xFFFFFFFF;
            uint[] child = new uint[8];
            child[0] = 0;
            child[1] = 0;
            child[2] = 0;
            child[3] = 0;
            child[4] = 0;
            child[5] = 0;
            child[6] = 0;
            child[7] = 0;


            buffer[0] = new SDF()
            {
                parent = -1,
                sdf = -1,
                position = Vector3.zero,
                isInside = 0,
                mipMapLevel = 0,

                Mask1 = mask[0],
                Mask2 = mask[1],
                Mask3 = mask[2],
                Mask4 = mask[3],

                child1 = child[0],
                child2 = child[1],
                child3 = child[2],
                child4 = child[3],
                child5 = child[4],
                child6 = child[5],
                child7 = child[6],
                child8 = child[7],

                HasBeenUpdated = 0,
            };
            ComputeBuffer computeBuffer = new ComputeBuffer(buffer.Length, SDF.GetSize());
            computeBuffer.SetData(buffer);
            //BuffersToDispose.Add(computeBuffer);
            return computeBuffer;
        }

        struct SDF
        {
            public int parent;
            public float sdf;
            public Vector3 position;
            public int isInside;
            public uint mipMapLevel;
            public uint Mask1;
            public uint Mask2;
            public uint Mask3;
            public uint Mask4;
            public uint child1;
            public uint child2;
            public uint child3;
            public uint child4;
            public uint child5;
            public uint child6;
            public uint child7;
            public uint child8;
            public int HasBeenUpdated; //TRI just to test for if sync actually works


            public static int GetSize()
            {
                return sizeof(int) + sizeof(float) + 3 * sizeof(float) + sizeof(int) + sizeof(uint) + 4 * sizeof(uint) +
                       8 * sizeof(uint) + sizeof(int);
            }
        }
/// <summary>
/// Sets relevant Information for the Compute Shader
/// </summary>
/// <param name="computeShader">The ComputeShader this Information is given</param>
private void SetSceneInfo(ComputeShader computeShader)
        {
            bool lightIsDirectional = _lightSource.type == LightType.Directional;
            computeShader.SetMatrix("_CameraToWorld", _cam.cameraToWorldMatrix);
            computeShader.SetMatrix("_CameraInverseProjection", _cam.projectionMatrix.inverse);
            computeShader.SetVector("_Light",
                (lightIsDirectional) ? _lightSource.transform.forward : _lightSource.transform.position);
            computeShader.SetBool("positionLight", !lightIsDirectional);
        }

/// <summary>
/// Collects all Shapes and Sets them to the Compute Shader
/// </summary>
/// <param name="computeShader"> Compute Shader this is set to</param>
/// <param name="kernelIndex"> Kernel of the Process</param>
private void SetShapeData(ComputeShader computeShader, int kernelIndex = 0)
        {
            List<Shape> allShapes = new List<Shape>(FindObjectsOfType<Shape>());
            List<Shape> orderedShapes = new List<Shape>();
            foreach (Shape shape in  allShapes)
            {
                if(shape.enabled == true)
                    orderedShapes.Add(shape);
            }
            
            

            ShapeData[] shapeData = new ShapeData[orderedShapes.Count];
            orderedShapes.Sort((a, b) => a.layer.CompareTo(b.layer));
            for (int i = 0; i < orderedShapes.Count; i++)
            {
                var s = orderedShapes[i];
                Vector3 col = new Vector3(s.colour.r, s.colour.g, s.colour.b);
                shapeData[i] = new ShapeData()
                {
                    position = s.Position,
                    scale = s.Scale, 
                    colour = col,
                    shapeType = (int)s.shapeType,
                    operation = (int)s.operation,
                    blendStrength = s.blendStrength * 3,
                    Parent = s.Parent,
                    rotation = s.Rotation,
                    inverseRotation = s.InverseRotation
                };
            }

            ComputeBuffer shapeBuffer = new ComputeBuffer(orderedShapes.Count, ShapeData.GetSize());
            shapeBuffer.SetData(shapeData);

            computeShader.SetBuffer(kernelIndex, "shapes", shapeBuffer);
            computeShader.SetInt("numShapes", orderedShapes.Count);


            _buffersToDispose.Add(shapeBuffer); //TODO this seems important
        }

/// <summary>
/// Struct for Representation of Shapes in the Compute Shader
/// </summary>
        struct ShapeData
        {
            public Vector3 position;
            public Vector3 scale;
            public Vector3 colour;
            public int shapeType;
            public int operation;
            public float blendStrength;
            public int Parent;
            public Vector4 rotation;
            public Vector4 inverseRotation;

            public static int GetSize()
            {
                return sizeof(float) * 10 + sizeof(int) * 3 + sizeof(float) * 4 * 2;
            }
        }
/// <summary>
/// Makes sure the RenderTexture is the same size as the Camera
/// </summary>

        protected void InitRenderTexture()
        {
            if (_target != null && _target.width == _cam.pixelWidth && _target.height == _cam.pixelHeight) return;
            if (_target != null)
            {
                _target.Release();
            }

            _target = new RenderTexture(_cam.pixelWidth, _cam.pixelHeight, 0, RenderTextureFormat.ARGBFloat,
                RenderTextureReadWrite.Linear)
            {
                enableRandomWrite = true
            };
            _target.Create();
        }
    }
}

#region License
//Following is a license of the parts of the Code i used, be it pretty edited


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

#endregion