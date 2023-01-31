using System.Collections.Generic;
using UnityEngine;

namespace SplitScreenCoop
{
    public partial class SplitScreenCoop
    {
        public class CameraListener : MonoBehaviour
        {
            public RoomCamera roomCamera;
            public RenderTexture renderTexture;
            public Dictionary<string, Color> ShaderColors = new Dictionary<string, Color>();
            public Dictionary<string, Vector4> ShaderVectors = new Dictionary<string, Vector4>();
            public Dictionary<string, float> ShaderFloats = new Dictionary<string, float>();
            public Dictionary<string, Texture> ShaderTextures = new Dictionary<string, Texture>();
            private Rect sourceRect;
            private Rect targetRect;
            private bool mapped;
            private bool _skip;
            private int srcX;
            private int srcY;
            private int srcWidth;
            private int srcHeight;
            private int dstX;
            private int dstY;

            public bool skip
            {
                get => _skip; internal set
                {
                    _skip = value;
                    if (roomCamera != null)
                    {
                        sLogger.LogWarning("CameraListener attached to roomcamera #" + roomCamera?.cameraNumber + "  set skip " + value);
                        fcameras[roomCamera.cameraNumber].targetTexture = _skip ? Futile.screen.renderTexture : this.renderTexture;
                    }
                }
            }

            void OnPreRender()
            {
                foreach (var kv in ShaderColors) Shader.SetGlobalColor(kv.Key, kv.Value);
                foreach (var kv in ShaderVectors) Shader.SetGlobalVector(kv.Key, kv.Value);
                foreach (var kv in ShaderFloats) Shader.SetGlobalFloat(kv.Key, kv.Value);
                foreach (var kv in ShaderTextures) Shader.SetGlobalTexture(kv.Key, kv.Value);
            }

            void OnDestroy()
            {
                ShaderTextures.Clear();
                roomCamera = null;
                if (renderTexture != null)
                {
                    renderTexture.Release();
                    renderTexture.DiscardContents();
                    renderTexture = null;
                }
            }

            public void Destroy()
            {
                ShaderTextures.Clear();
                roomCamera = null;
                if (renderTexture != null)
                {
                    renderTexture.Release();
                    renderTexture.DiscardContents();
                    renderTexture = null;
                }
                Destroy(this);
            }

            internal void ReinitRenderTexture()
            {
                sLogger.LogWarning("CameraListener attached to roomcamera #" + roomCamera?.cameraNumber + "  ReinitRenderTexture");
                if (renderTexture != null)
                {
                    renderTexture.Release();
                    renderTexture.DiscardContents();
                }
                renderTexture = new RenderTexture(Futile.screen.renderTexture);
                if (mapped)
                {
                    SetMap(this.sourceRect, this.targetRect);
                }
            }

            internal void AttachTo(RoomCamera self)
            {
                roomCamera = self;
                sLogger.LogWarning("CameraListener attached to roomcamera #" + self.cameraNumber);
                ReinitRenderTexture();
                fcameras[self.cameraNumber].targetTexture = renderTexture;
            }

            internal void SetMap(Rect sourceRect, Rect targetRect)
            {
                sLogger.LogWarning("CameraListener attached to roomcamera #" + roomCamera?.cameraNumber + "  SetMap");
                if (renderTexture != null)
                {
                    var h = renderTexture.height;
                    var w = renderTexture.width;
                    srcX = Mathf.FloorToInt(w * sourceRect.x);
                    srcY = Mathf.FloorToInt(h * sourceRect.y);
                    srcWidth = Mathf.FloorToInt(w * sourceRect.width);
                    srcHeight = Mathf.FloorToInt(h * sourceRect.height);
                    dstX = Mathf.FloorToInt(w * targetRect.x);
                    dstY = Mathf.FloorToInt(h * targetRect.y);
                }
                this.sourceRect = sourceRect;
                this.targetRect = targetRect;

                mapped = true;
            }

            public void OnPostRender()
            {
                if (renderTexture != null && !_skip && mapped)
                {
                    //sLogger.LogWarning("CameraListener attached to roomcamera #" + roomCamera?.cameraNumber + "  Rendering");
                    Graphics.CopyTexture(renderTexture, 0, 0, srcX, srcY, srcWidth, srcHeight, Futile.screen.renderTexture, 0, 0, dstX, dstY);
                }
            }
        }
    }
}
