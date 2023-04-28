using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using RimWorld.Planet;

namespace zed_0xff.CPS
{
    class Cache : WorldComponent
    {
        private static Dictionary<int, Dictionary<IntVec3, Building_ThePit>> mapPosPits = new Dictionary<int, Dictionary<IntVec3, Building_ThePit>>();

        public Cache(World w) : base(w)
        {
            // clear caches whenever a game is created or loaded.
            mapPosPits.Clear();
        }

        public static void Add(Building_ThePit pit){
            if( Prefs.DevMode ) Log.Message("[d] CPS: add " + pit);
            if( !mapPosPits.ContainsKey(pit.Map.uniqueID) ){
                mapPosPits.Add(pit.Map.uniqueID, new Dictionary<IntVec3, Building_ThePit>());
            }
            foreach (IntVec3 cell in pit.OccupiedRect()){
                mapPosPits[pit.Map.uniqueID][cell] = pit;
            }
        }

        // should be faster than ThingGrid.ThingAt<T>
        // null-safe
        public static Building_ThePit Get(IntVec3 pos, Map map){
            if( pos == null || map == null )
                return null;

            if( !mapPosPits.ContainsKey(map.uniqueID) )
                return null;

            Building_ThePit t = null;
            if (mapPosPits[map.uniqueID].TryGetValue(pos, out t)){
                return t; // might also be null
            }

            return null;
        }

        public static void Remove(Building_ThePit pit){
            if( Prefs.DevMode ) Log.Message("[d] CPS: removing " + pit);
            if( mapPosPits.ContainsKey(pit.Map.uniqueID) ){
                foreach (IntVec3 cell in pit.OccupiedRect()){
                    mapPosPits[pit.Map.uniqueID][cell] = null;
                }
            }
        }
    }
}
