using BepInEx;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Security.Permissions;
using UnityEngine;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace MapWarp
{
    [BepInPlugin("com.henpemaz.mapwarp", "Map Warp", "0.1.0")]
    public class MapWarp : BaseUnityPlugin
    {
        public void OnEnable()
        {
            Logger.LogInfo("Enabled");
            On.RainWorld.OnModsInit += OnModsInit;
        }

        private void OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            //self.setup.startScreen = false;
            //On.RainWorld.Update += RainWorld_Update;


            orig(self);

            // Create ui in Dev view
            On.DevInterface.MapPage.NewMode += MapPage_NewMode;
            // accept buttons
            On.DevInterface.MapPage.Signal += MapPage_Signal;

            // reload rooms that are missing data
            On.DevInterface.MapObject.Update += MapObject_Update;

            // Click
            On.DevInterface.RoomPanel.Update += RoomPanel_Update;

            // Teleportation enabler!
            On.ShortcutHandler.TeleportingCreatureArrivedInRealizedRoom += ShortcutHandler_TeleportingCreatureArrivedInRealizedRoom;
            On.World.GetAbstractRoom_int += World_GetAbstractRoom_int;

            // Bugfixxes
            // On.AbstractCreature.Realize += AbstractCreature_Realize;
            On.VirtualMicrophone.NewRoom += VirtualMicrophone_NewRoom;
        }

        private void RainWorld_Update(On.RainWorld.orig_Update orig, RainWorld self)
        {
            try
            {
                orig(self);
            }
            catch (System.Exception e)
            {
                Logger.LogError(e);
                throw;
            }
        }

        // reload rooms that are missing data
        private void MapObject_Update(On.DevInterface.MapObject.orig_Update orig, DevInterface.MapObject self)
        {
            if (self.roomPrep == null && self.roomReps != null && self.roomLoaderIndex >= 0 && self.roomLoaderIndex < self.roomReps.Length)
            {
                if (self.roomReps[self.roomLoaderIndex]?.mapTex != null) //texture found
                {
                    self.roomReps[self.roomLoaderIndex].mapTex = null;
                    self.roomReps[self.roomLoaderIndex].texture = null;
                    foreach (var node in (self.world.game.devUI.activePage as DevInterface.MapPage).subNodes)
                    {
                        if (node is DevInterface.RoomPanel panel && panel.roomRep == self.roomReps[self.roomLoaderIndex])
                        {
                            panel.miniMap.fSprites[0].element = Futile.atlasManager.GetElementWithName("pixel");
                            //panel.Refresh();
                            break;
                        }
                    }
                    HeavyTexturesCacheExtensions.ClearAtlas("MapTex_" + self.roomReps[self.roomLoaderIndex].room.name);
                }
            }
            // prevent IndexOutOfRangeException
            if (self.world.game?.cameras[0].room?.world == self.world)
            {
                orig(self);
            }
        }

        // Create ui in Dev view
        private void MapPage_NewMode(On.DevInterface.MapPage.orig_NewMode orig, DevInterface.MapPage self)
        {
            orig(self);

            if (!self.canonView)
            {
                var regions = Menu.FastTravelScreen.GetRegionOrder();
                Vector2 curpos = new Vector2(120f, 560f);
                self.modeSpecificNodes.Add(new DevInterface.DevUILabel(self.owner, "regions", self, curpos, 60f, "Regions:"));
                self.subNodes.Add(self.modeSpecificNodes[self.modeSpecificNodes.Count - 1]);
                curpos.x += 20;
                curpos.y -= 30;

                for (int i = 0; i < regions.Count; i++)
                {
                    string region = regions[i];
                    self.modeSpecificNodes.Add(new DevInterface.Button(self.owner, "region_" + region, self, curpos, 40f, region));
                    self.subNodes.Add(self.modeSpecificNodes[self.modeSpecificNodes.Count - 1]);
                    curpos.y -= 20;
                }

                // Hold C on dev page ? reload all textures
                if (Input.GetKey("c"))
                {
                    foreach (var r in self.map.roomReps)
                    {
                        r.mapTex = null;
                        r.texture = null;
                        HeavyTexturesCacheExtensions.ClearAtlas("MapTex_" + r.room.name);
                    }
                }
            }
        }

        // Allow moving to a new world
        World oldWorld;
        World newWorld;
        private AbstractRoom World_GetAbstractRoom_int(On.World.orig_GetAbstractRoom_int orig, World self, int room)
        {
            AbstractRoom val = orig(self, room);
            if (oldWorld != null && val == null)
            {
                val = orig(oldWorld, room);
                if (newWorld != null && val == null)
                {
                    val = orig(newWorld, room);
                }
            }
            
            return val;
        }

        private void MapPage_Signal(On.DevInterface.MapPage.orig_Signal orig, DevInterface.MapPage self, DevInterface.DevUISignalType type, DevInterface.DevUINode sender, string message)
        {
            orig(self, type, sender, message);

            if (type != DevInterface.DevUISignalType.ButtonClick) return;
            if (string.IsNullOrEmpty(sender.IDstring) || !sender.IDstring.StartsWith("region")) return;

            var target = sender.IDstring.Substring(7);

            if(target == self.map.world.name)
            {
                Debug.Log("MapWarp: staying in " + target);
                return;
            }
            Debug.Log("MapWarp: switching map to " + target);

            oldWorld = self.owner.game.world;
            self.owner.game.overWorld.LoadWorld(target, self.owner.game.overWorld.PlayerCharacterNumber, false);
            newWorld = self.owner.game.world;

            // from gate switching Overworld.worldloaded
            if (self.owner.game.roomRealizer != null)
            {
                self.owner.game.roomRealizer = new RoomRealizer(self.owner.game.roomRealizer.followCreature, newWorld);
            }

            for (int k = 0; k < self.owner.game.Players.Count; k++)
            {
                if (self.owner.game.Players[k].realizedCreature != null && (self.owner.game.Players[k].realizedCreature as Player).objectInStomach != null)
                {
                    (self.owner.game.Players[k].realizedCreature as Player).objectInStomach.world = newWorld;
                }
            }
            self.owner.game.shortcuts.transportVessels.Clear();
            self.owner.game.shortcuts.betweenRoomsWaitingLobby.Clear();
            self.owner.game.shortcuts.borderTravelVessels.Clear();

            for (int num = 0; num < newWorld.game.cameras.Length; num++)
            {
                newWorld.game.cameras[num].hud.ResetMap(new HUD.Map.MapData(newWorld, newWorld.game.rainWorld));
                if (newWorld.game.cameras[num].hud.textPrompt.subregionTracker != null)
                {
                    newWorld.game.cameras[num].hud.textPrompt.subregionTracker.lastShownRegion = 0;
                }
            }

            oldWorld.regionState.AdaptRegionStateToWorld(-1, -1);
            oldWorld.regionState.world = null;
            newWorld.rainCycle.cycleLength = oldWorld.rainCycle.cycleLength;
            newWorld.rainCycle.timer = oldWorld.rainCycle.timer;
            
            Debug.Log("MapWarp: moving players to new world");
            MovePlayers(newWorld.abstractRooms[0], 0);
            Debug.Log("MapWarp: loading room in new world");
            while (newWorld.loadingRooms.Count > 0 && !newWorld.loadingRooms[0].done) newWorld.loadingRooms[0].Update();
            Debug.Log("MapWarp: room in new world realized ? " + (newWorld.abstractRooms[0].realizedRoom != null));
            Debug.Log("MapWarp: ticking shortcuts. count is " + self.owner.game.shortcuts.betweenRoomsWaitingLobby.Count);
            newWorld.game.shortcuts.Update(); // tick and place
            Debug.Log("MapWarp: shortcuts ticked. count is " + self.owner.game.shortcuts.betweenRoomsWaitingLobby.Count);

            Debug.Log("MapWarp: updating world references");
            foreach (var p in newWorld.game.Players)
            {

                List<AbstractPhysicalObject> allConnectedObjects = p.GetAllConnectedObjects(); // The catch: includes self
                for (int i = 0; i < allConnectedObjects.Count; i++)
                {
                    var obj = allConnectedObjects[i];
                    if (obj.world == newWorld) continue; // already moved
                    obj.world = newWorld;
                    if (obj is AbstractCreature cA) // creature
                    {
                        if (cA.creatureTemplate.AI)
                        {
                            cA.abstractAI.lastRoom = newWorld.firstRoomIndex;
                            cA.abstractAI.NewWorld(newWorld);
                            cA.InitiateAI();
                            cA.abstractAI.RealAI?.NewRoom(newWorld.abstractRooms[0].realizedRoom);
                        }
                    }
                }
            }

            // keeping the map up nullrefs
            Debug.Log("MapWarp: resetting devui");
            self.owner.ClearSprites();
            self.owner.game.devUI = null;
            // dirty fix
            self.owner.game.devUI = new DevInterface.DevUI(self.owner.game);
            self.owner.game.devUI.SwitchPage(3);
            self.owner.game = null;

            Debug.Log("MapWarp: done with world switching");

            oldWorld = null;
            newWorld = null;
        }

        private void RoomPanel_Update(On.DevInterface.RoomPanel.orig_Update orig, DevInterface.RoomPanel self)
        {
            orig(self);

            if (self.miniMap != null && !self.CanonView && self.owner.mouseClick && self.MouseOver)
            {
                if(self.owner.game?.devUI != self.owner)
                {
                    // a region switch happened, ignore spur click
                    return;
                }

                Debug.Log("MapWarp clicked on room " + self.miniMap.roomRep.room.name);
                int nodeclicked = -1;
                Vector2 mousePos = self.owner.mousePos - self.miniMap.absPos;
                for (int i = 0; i < self.miniMap.nodeSquarePositions.Length; i++)
                {
                    Vector2 delta = mousePos - self.miniMap.nodeSquarePositions[i];
                    if (delta.x > -8 && delta.x < 8 && delta.y > -8 && delta.y < 8)
                    {
                        nodeclicked = i;
                    }
                }
                if (nodeclicked > -1)
                {
                    Debug.Log("MapWarp clicked on node " + nodeclicked);
                    MovePlayers(self.miniMap.roomRep.room, nodeclicked);
                }
                else
                {
                    if (self.miniMap.MouseOver)
                    {
                        Debug.Log("MapWarp clicked on geometry at " + mousePos);
                        MovePlayers(self.miniMap.roomRep.room, new WorldCoordinate(self.miniMap.roomRep.room.index, Mathf.FloorToInt(mousePos.x/2), Mathf.FloorToInt(mousePos.y/2), 0));
                    }
                }
            }
        }

        private void VirtualMicrophone_NewRoom(On.VirtualMicrophone.orig_NewRoom orig, VirtualMicrophone self, Room room)
        {
            /// <summary>
            /// Fix gate noises following the player through rooms
            /// copied over from ExtendedGates
            /// </summary>
            orig(self, room);
            for (int i = self.soundObjects.Count - 1; i >= 0; i--)
            {
                if (self.soundObjects[i] is VirtualMicrophone.PositionedSound) // Doesn't make sense that this carries over
                {
                    // I was going to do somehtin supercomplicated like test if controller as loop was in the same room but screw it
                    //VirtualMicrophone.ObjectSound obj = (self.soundObjects[i] as VirtualMicrophone.ObjectSound);
                    //if (obj.controller != null && )
                    self.soundObjects[i].Destroy();
                    self.soundObjects.RemoveAt(i);
                }
            }
        }

        private void ShortcutHandler_TeleportingCreatureArrivedInRealizedRoom(On.ShortcutHandler.orig_TeleportingCreatureArrivedInRealizedRoom orig, ShortcutHandler self, ShortcutHandler.TeleportationVessel tVessel)
        {
            try
            {
                orig(self, tVessel);
            }
            catch (System.NullReferenceException)
            {
                if (!(tVessel.creature is ITeleportingCreature))
                {
                    WorldCoordinate arrival = tVessel.destination;
                    if (!arrival.TileDefined)
                    {
                        arrival = tVessel.room.realizedRoom.LocalCoordinateOfNode(tVessel.entranceNode);
                        arrival.abstractNode = tVessel.entranceNode;
                    }

                    tVessel.creature.abstractCreature.pos = arrival;
                    tVessel.creature.SpitOutOfShortCut(arrival.Tile, tVessel.room.realizedRoom, true);
                }
            }
        }

        private ShortcutHandler.Vessel RemoveFromVessels(ShortcutHandler sch, Creature crit)
        {
            ShortcutHandler.Vessel vessel = null;
            for (int i = 0; i < sch.transportVessels.Count; i++)
            {
                List<AbstractPhysicalObject> allConnectedObjects = sch.transportVessels[i].creature.abstractCreature.GetAllConnectedObjects();
                for (int j = 0; j < allConnectedObjects.Count; j++)
                {
                    if (allConnectedObjects[j].realizedObject != null && allConnectedObjects[j].realizedObject == crit)
                    {
                        vessel = sch.transportVessels[i];
                        sch.transportVessels.RemoveAt(i);
                        return vessel;
                    }
                }
            }
            for (int i = 0; i < sch.borderTravelVessels.Count; i++)
            {
                List<AbstractPhysicalObject> allConnectedObjects = sch.borderTravelVessels[i].creature.abstractCreature.GetAllConnectedObjects();
                for (int j = 0; j < allConnectedObjects.Count; j++)
                {
                    if (allConnectedObjects[j].realizedObject != null && allConnectedObjects[j].realizedObject == crit)
                    {
                        vessel = sch.borderTravelVessels[i];
                        sch.borderTravelVessels.RemoveAt(i);
                        return vessel;
                    }
                }
            }
            for (int i = 0; i < sch.betweenRoomsWaitingLobby.Count; i++)
            {
                List<AbstractPhysicalObject> allConnectedObjects = sch.betweenRoomsWaitingLobby[i].creature.abstractCreature.GetAllConnectedObjects();
                for (int j = 0; j < allConnectedObjects.Count; j++)
                {
                    if (allConnectedObjects[j].realizedObject != null && allConnectedObjects[j].realizedObject == crit)
                    {
                        vessel = sch.betweenRoomsWaitingLobby[i];
                        sch.betweenRoomsWaitingLobby.RemoveAt(i);
                        return vessel;
                    }
                }
            }
            return null;
        }

        // In-region moving
        private void MovePlayers(AbstractRoom room, int nodeIndex)
        {
            WorldCoordinate dest = new WorldCoordinate(room.index, -1, -1, nodeIndex);
            if (room.realizedRoom != null)
            {
                dest = room.realizedRoom.LocalCoordinateOfNode(nodeIndex);
                dest.abstractNode = nodeIndex; // silly isnt it
            }
            MovePlayers(room, dest);
        }
        
        private void MovePlayers(AbstractRoom room, WorldCoordinate dest)
        {
            if (room.offScreenDen) return; // no can do, player shouldnt ever never abstractize or they lose their tummy contents
            if (room.realizedRoom == null) room.world.ActivateRoom(room); // prevent non-camera-followed players from abstractizing
            foreach (var p in room.world.game.Players)
            {
                if (p.realizedCreature != null)
                {
                    if (p.realizedCreature.inShortcut) // remove from pipe, aalready removed from room.
                    {
                        Debug.Log("MapWarp: player in shortcuts");
                        var vessel = RemoveFromVessels(room.world.game.shortcuts, p.realizedCreature);
                        if(vessel == null) room.world.game.shortcuts.CreatureTeleportOutOfRoom(p.realizedCreature, new WorldCoordinate(), dest);// vessels removed during region-switching, start position lost
                        else room.world.game.shortcuts.CreatureTeleportOutOfRoom(p.realizedCreature, new WorldCoordinate() { room=vessel.room.index, abstractNode=vessel.entranceNode }, dest);
                    }
                    else if (p.realizedCreature.room != null)
                    {
                        Debug.Log("MapWarp: player in room");
                        // from: Creature.SuckedIntoShortCut
                        // cleans out connected objects *and self* from room.
                        Room realizedRoom = p.realizedCreature.room;
                        WorldCoordinate origin = p.pos;
                        List<AbstractPhysicalObject> allConnectedObjects = p.GetAllConnectedObjects(); // The catch: includes self
                        for (int i = 0; i < allConnectedObjects.Count; i++)
                        {
                            if (allConnectedObjects[i].realizedObject != null)
                            {
                                if (allConnectedObjects[i].realizedObject is Creature crit)
                                {
                                    crit.inShortcut = true;
                                }
                                realizedRoom.RemoveObject(allConnectedObjects[i].realizedObject);
                                realizedRoom.CleanOutObjectNotInThisRoom(allConnectedObjects[i].realizedObject);
                            }
                        }
                        room.world.game.shortcuts.CreatureTeleportOutOfRoom(p.realizedCreature, origin, dest);
                    }
                    else
                    {
                        Debug.Log("MapWarp: player not in shortcuts nor in room!");
                    }
                }
            }
        }
    }

    public static class HeavyTexturesCacheExtensions
    {
        public static void ClearAtlas(string atlas)
        {
            HeavyTexturesCache.futileAtlasListings.Remove(atlas);
            Futile.atlasManager.UnloadAtlas(atlas);
        }
    }
}