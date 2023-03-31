using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

namespace SplitScreenCoop
{
    static class DisplayExtensions
    {
        static ConditionalWeakTable<Display, SplitScreenCoop.DisplayExtras> map = new();
        public static SplitScreenCoop.DisplayExtras Extras(this Display display)
        {
            return map.GetValue(display, (e) => new SplitScreenCoop.DisplayExtras(e));
        }
    }

    public partial class SplitScreenCoop
    {
        public class DisplayExtras
        {
            public Display display;
            public RenderTexture renderTexture;
            private RawImage rawImage;
            public DisplayExtras(Display display)
            {
                this.display = display;
                displayExtras.Add(this);
                if (display == Display.main)
                {
                    rawImage = Futile.instance._cameraImage;
                }
                else
                {
                    var canvasHolder = GameObject.Instantiate(Futile.instance._cameraImage.transform.parent.gameObject); // dupe
                    var dummyCamera = canvasHolder.AddComponent<Camera>(); // its 2023 and unity still has this sort of bugs
                    dummyCamera.targetDisplay = 1;
                    dummyCamera.cullingMask = 0;
                    var canvas = canvasHolder.GetComponent<Canvas>();
                    canvas.targetDisplay = 1;
                    sLogger.LogInfo(canvas.isActiveAndEnabled);
                    rawImage = canvasHolder.GetComponentInChildren<RawImage>();
                }
                ReinitRenderTexture();
            }

            public void DiscardRenderTexture()
            {
                if (renderTexture != null)
                {
                    renderTexture.Release();
                    renderTexture.DiscardContents();
                    renderTexture = null;
                }
            }

            public void ReinitRenderTexture()
            {
                DiscardRenderTexture();
                if (display == Display.main)
                {
                    renderTexture = Futile.screen.renderTexture;
                }
                else
                {
                    renderTexture = new RenderTexture(Futile.screen.renderTexture);
                    rawImage.texture = renderTexture;
                }
            }

            public void MapToTexture(RenderTexture renderTexture)
            {
                rawImage.texture = renderTexture;
            }
        }
    }
}
