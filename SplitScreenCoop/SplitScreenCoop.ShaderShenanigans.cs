
using System.Collections.Generic;
using UnityEngine;

namespace SplitScreenCoop
{
    public partial class SplitScreenCoop
    {
        //Envelop camera-related stuff that does shader.set calls so we know the calling camera index and can re-apply those in a sane way later
        //not 100% robust (currently we don't store "global" assignments that one camera might choose to overwrite or not)

        public void RoomCamera_MoveCamera_Room_int(On.RoomCamera.orig_MoveCamera_Room_int orig, RoomCamera self, Room newRoom, int camPos)
        {
            ConsiderColapsing(self.game, false); // this one is special

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

        public void RoomCamera_MoveCamera_int(On.RoomCamera.orig_MoveCamera_int orig, RoomCamera self, int camPos)
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

        public void RoomCamera_DrawUpdate(On.RoomCamera.orig_DrawUpdate orig, RoomCamera self, float timeStacker, float timeSpeed)
        {
            var prev = curCamera;
            try
            {
                curCamera = self.cameraNumber;
                orig(self, timeStacker, timeSpeed);
                OffsetHud(self);
            }
            finally
            {
                curCamera = prev;
            }
        }

        public void RoomCamera_Update(On.RoomCamera.orig_Update orig, RoomCamera self)
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

        public void RoomCamera_UpdateSnowLight(On.RoomCamera.orig_UpdateSnowLight orig, RoomCamera self)
        {
            if (cameraListeners[self.cameraNumber] is CameraListener l)
            {
                l.OnPreRender();
            }
            orig(self);
        }

        public delegate void delSetGlobalColor(int nameID, Color vec);
        public void Shader_SetGlobalColor(delSetGlobalColor orig, int nameID, Color vec)
        {
            orig(nameID, vec);
            if (curCamera >= 0 && cameraListeners[curCamera] is CameraListener l)
            {
                l.ShaderColors[nameID] = vec;
            }
            else if ((nameID == RainWorld.ShadPropMapCol || nameID == RainWorld.ShadPropMapWaterCol) && !(rainworldGameObject.processManager?.currentMainLoop is RainWorldGame game))
            {
                cameraListeners[0].ShaderColors[nameID] = vec;
            }
        }

        public delegate void delSetGlobalVectorArrayArray(int nameID, Vector4[] values);
        public void Shader_SetGlobalVectorArrayArray(delSetGlobalVectorArrayArray orig, int nameID, Vector4[] values)
        {
            orig(nameID, values);
            if (curCamera >= 0 && cameraListeners[curCamera] is CameraListener l)
            {
                l.ShaderVectorArrays[nameID] = values;
            }
        }
        public delegate void delSetGlobalVectorArrayList(int nameID, List<Vector4> values);
        public void Shader_SetGlobalVectorArrayList(delSetGlobalVectorArrayList orig, int nameID, List<Vector4> values)
        {
            orig(nameID, values);
            if (curCamera >= 0 && cameraListeners[curCamera] is CameraListener l)
            {
                l.ShaderVectorLists[nameID] = values;
            }
        }

        public delegate void delSetGlobalVector(int nameID, Vector4 vec);
        public void Shader_SetGlobalVector(delSetGlobalVector orig, int nameID, Vector4 vec)
        {
            orig(nameID, vec);
            if (curCamera >= 0 && cameraListeners[curCamera] is CameraListener l)
            {
                l.ShaderVectors[nameID] = vec;
            }
            else if (nameID == RainWorld.ShadPropMapPan && !(rainworldGameObject.processManager?.currentMainLoop is RainWorldGame game))
            {
                cameraListeners[0].ShaderVectors[nameID] = vec;
            }
        }

        public delegate void delSetGlobalFloat(int nameID, float f);
        public void Shader_SetGlobalFloat(delSetGlobalFloat orig, int nameID, float f)
        {
            orig(nameID, f);
            if (curCamera >= 0 && cameraListeners[curCamera] is CameraListener l && (nameID != RainWorld.ShadPropRain))
            {
                l.ShaderFloats[nameID] = f;
            }
        }

        public delegate void delSetGlobalInt(int nameID, int i);
        public void Shader_SetGlobalInt(delSetGlobalInt orig, int nameID, int i)
        {
            orig(nameID, i);
            if (curCamera >= 0 && cameraListeners[curCamera] is CameraListener l)
            {
                l.ShaderFloats[nameID] = i; // underlying handler is the same
            }
        }

        public delegate void delSetGlobalTexture(int nameID, Texture t);
        public void Shader_SetGlobalTexture(delSetGlobalTexture orig, int nameID, Texture t)
        {
            orig(nameID, t);
            if (curCamera >= 0 && cameraListeners[curCamera] is CameraListener l)
            {
                l.ShaderTextures[nameID] = t;
            }
            else if (nameID == RainWorld.ShadPropMapFogTexture && !(rainworldGameObject.processManager?.currentMainLoop is RainWorldGame game))
            {
                cameraListeners[0].ShaderTextures[nameID] = t;
            }
        }
    }
}
