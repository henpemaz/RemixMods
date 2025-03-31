using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

namespace SplitScreenCoop
{
    public partial class SplitScreenCoop
    {
        /// <summary>
        /// Monobehavior that listen to camera events, so we can pre and post
        /// We store per-room-camera shader values and apply them per-unity-camera
        /// In split mode, Unity camera renders to an individual renderTexture and blits to the main tex post
        /// </summary>
        public class CameraListener : MonoBehaviour
        {
            public Camera fcamera;
            public Display display;
            public RenderTexture renderTexture;
            public RenderTexture tempTex;
            public Dictionary<int, Color> ShaderColors = new Dictionary<int, Color>();
            public Dictionary<int, Vector4> ShaderVectors = new Dictionary<int, Vector4>();
            public Dictionary<int, Vector4[]> ShaderVectorArrays = new Dictionary<int, Vector4[]>();
            public Dictionary<int, List<Vector4>> ShaderVectorLists = new Dictionary<int, List<Vector4>>();
            public Dictionary<int, float> ShaderFloats = new Dictionary<int, float>();
            public Dictionary<int, Texture> ShaderTextures = new Dictionary<int, Texture>();
            public Rect sourceRect;
            public Rect targetRect;
            public int srcX;
            public int srcY;
            public int srcWidth;
            public int srcHeight;
            public int dstX;
            public int dstY;
            public int dstWidth;
            public int dstHeight;


            public bool _direct = true;
            /// <summary>
            /// bypass intermediate rendertexture and blit
            /// </summary>
            public bool direct
            {
                get => _direct; set
                {
                    _direct = value;
                    Retarget();
                }
            }

            public bool mirrorMain
            {
                set
                {
                    if (display != Display.main)
                    {
                        fcamera.enabled = !value;
                        display.Extras().MapToTexture(value ? Futile.screen.renderTexture : display.Extras().renderTexture);
                    }
                }
            }

            public void Retarget()
            {
                fcamera.targetTexture = _direct ? display.Extras().renderTexture : this.renderTexture;
            }


            /// <summary>
            /// Effectively ctor
            /// </summary>
            public void AttachTo(Camera fcamera, Display display)
            {
                this.fcamera = fcamera;
                this.display = display;
                ReinitRenderTexture();
                Retarget();
            }

            public void ReinitRenderTexture()
            {
                if (renderTexture != null)
                {
                    renderTexture.Release();
                    renderTexture.DiscardContents();
                    renderTexture = null;
                }
                display.Extras().ReinitRenderTexture();
                renderTexture = new RenderTexture(Futile.screen.renderTexture);
                tempTex = new RenderTexture(renderTexture);
                SetMap(this.sourceRect, this.targetRect);
            }
            
            /// <summary>
            /// Camera.rect but for our custom blit
            /// </summary>
            public void SetMap(Rect sourceRect, Rect targetRect)
            {
                var h = renderTexture.height;
                var w = renderTexture.width;
                srcX = Mathf.FloorToInt(w * sourceRect.x);
                srcY = Mathf.FloorToInt(h * sourceRect.y);
                srcWidth = Mathf.FloorToInt(w * sourceRect.width);
                srcHeight = Mathf.FloorToInt(h * sourceRect.height);
                dstX = Mathf.FloorToInt(w * targetRect.x);
                dstY = Mathf.FloorToInt(h * targetRect.y);
                dstWidth = Mathf.FloorToInt(w * targetRect.width);
                dstHeight = Mathf.FloorToInt(h * targetRect.height);
                this.sourceRect = sourceRect;
                this.targetRect = targetRect;
            }

            
            /// <summary>
            /// Apply shader vars from this roomcamera
            /// </summary>
            public void OnPreRender()
            {
                foreach (var kv in ShaderColors.ToArray())
                    Shader.SetGlobalColor(kv.Key, kv.Value);
                foreach (var kv in ShaderVectors.ToArray())
                    Shader.SetGlobalVector(kv.Key, kv.Value);
                foreach (var kv in ShaderVectorArrays.ToArray())
                    Shader.SetGlobalVectorArray(kv.Key, kv.Value);
                foreach (var kv in ShaderVectorLists.ToArray())
                    Shader.SetGlobalVectorArray(kv.Key, kv.Value);
                foreach (var kv in ShaderFloats.ToArray())
                    Shader.SetGlobalFloat(kv.Key, kv.Value);
                foreach (var kv in ShaderTextures.ToArray())
                    Shader.SetGlobalTexture(kv.Key, kv.Value);
            }

            /// <summary>
            /// Blit into display texture
            /// </summary>
            public void OnPostRender()
            {
                if (!_direct)
                {
                    if (srcWidth != dstWidth)
                    {
                        var scale = sourceRect.width / targetRect.width;
                        Graphics.Blit(renderTexture, tempTex, new Vector2(scale, scale), new Vector2(0, 0));
                        Graphics.CopyTexture(tempTex, 0, 0, 0, 0, dstWidth, dstHeight, Futile.screen.renderTexture, 0, 0, dstX, dstY);
                    }
                    else
                    {
                        Graphics.CopyTexture(renderTexture, 0, 0, srcX, srcY, srcWidth, srcHeight, Futile.screen.renderTexture, 0, 0, dstX, dstY);
                    }
                }
            }

            public void OnDestroy()
            {
                ShaderTextures.Clear();
                fcamera = null;
                display = null;
                if (renderTexture != null)
                {
                    renderTexture.Release();
                    renderTexture.DiscardContents();
                    renderTexture = null;
                    tempTex.Release();
                    tempTex.DiscardContents();
                    tempTex = null;
                }
            }

            internal void BindToDisplay(Display display)
            {
                this.display = display;
                Retarget();
            }
        }
    }
}
