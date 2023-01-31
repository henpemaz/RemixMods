
using UnityEngine;

namespace SplitScreenCoop
{
    public partial class SplitScreenCoop
    {
        // Init unity camera 2
        private void Futile_Init(On.Futile.orig_Init orig, Futile self, FutileParams futileParams)
        {
            orig(self, futileParams);
            self._cameraHolder2 = new GameObject();
            self._cameraHolder2.transform.parent = self.gameObject.transform;
            self._camera2 = self._cameraHolder2.AddComponent<Camera>();
            self.InitCamera(self._camera2, 2);

            fcameras = new Camera[] { self.camera, self.camera2 };

            self.camera2.enabled = false;
            self.UpdateCameraPosition();
        }

        // CameraListeners need to keep up
        private void FScreen_ReinitRenderTexture(On.FScreen.orig_ReinitRenderTexture orig, FScreen self, int displayWidth)
        {
            orig(self, displayWidth);

            foreach (var l in cameraListeners)
            {
                l?.ReinitRenderTexture();
            }
        }

        // Apply better offsets in multicam mode
        private void Futile_UpdateCameraPosition(On.Futile.orig_UpdateCameraPosition orig, Futile self)
        {
            orig(self);

            for (int i = 0; i < fcameras.Length; i++)
            {
                if (fcameras[i] == null) continue;
                var offset = camOffsets[i];
                var x = (Futile.screen.originX - 0.5f) * -Futile.screen.pixelWidth * Futile.displayScaleInverse + Futile.screenPixelOffset.x + offset.x;
                var y = (Futile.screen.originY - 0.5f) * -Futile.screen.pixelHeight * Futile.displayScaleInverse - Futile.screenPixelOffset.y + offset.y;
                fcameras[i].transform.position = new Vector3(x, y, -10f);
            }
        }

        private void RoomCamera_DrawUpdate(On.RoomCamera.orig_DrawUpdate orig, RoomCamera self, float timeStacker, float timeSpeed)
        {
            var prev = curCamera;
            try
            {
                curCamera = self.cameraNumber;
                orig(self, timeStacker, timeSpeed);
            }
            finally
            {
                curCamera = prev;
            }
        }

        private void RoomCamera_MoveCamera_Room_int(On.RoomCamera.orig_MoveCamera_Room_int orig, RoomCamera self, Room newRoom, int camPos)
        {
            ConsiderColapsing(self.game);

            var prev = curCamera;
            try
            {
                curCamera = self.cameraNumber;
                orig(self, newRoom, camPos);
            }
            finally
            {
                curCamera = prev;
            }
        }

        private void RoomCamera_MoveCamera_int(On.RoomCamera.orig_MoveCamera_int orig, RoomCamera self, int camPos)
        {
            var prev = curCamera;
            try
            {
                curCamera = self.cameraNumber;
                orig(self, camPos);
            }
            finally
            {
                curCamera = prev;
            }
        }

        private void RoomCamera_Update(On.RoomCamera.orig_Update orig, RoomCamera self)
        {
            var prev = curCamera;
            try
            {
                curCamera = self.cameraNumber;
                orig(self);
            }
            finally
            {
                curCamera = prev;
            }
        }

        public delegate void delSetGlobalColor(string propertyName, Color vec);
        public void Shader_SetGlobalColor(delSetGlobalColor orig, string propertyName, Color vec)
        {
            orig(propertyName, vec);
            if (curCamera >= 0 && cameraListeners[curCamera] is CameraListener l)
            {
                l.ShaderColors[propertyName] = vec;
            }
        }

        public delegate void delSetGlobalVector(string propertyName, Vector4 vec);
        public void Shader_SetGlobalVector(delSetGlobalVector orig, string propertyName, Vector4 vec)
        {
            orig(propertyName, vec);
            if (curCamera >= 0 && cameraListeners[curCamera] is CameraListener l)
            {
                l.ShaderVectors[propertyName] = vec;
            }
        }

        public delegate void delSetGlobalFloat(string propertyName, float f);
        public void Shader_SetGlobalFloat(delSetGlobalFloat orig, string propertyName, float f)
        {
            orig(propertyName, f);
            if (curCamera >= 0 && cameraListeners[curCamera] is CameraListener l)
            {
                l.ShaderFloats[propertyName] = f;
            }
        }

        public delegate void delSetGlobalTexture(string propertyName, Texture t);
        public void Shader_SetGlobalTexture(delSetGlobalTexture orig, string propertyName, Texture t)
        {
            orig(propertyName, t);
            if (curCamera >= 0 && cameraListeners[curCamera] is CameraListener l)
            {
                l.ShaderTextures[propertyName] = t;
            }
        }
    }
}
