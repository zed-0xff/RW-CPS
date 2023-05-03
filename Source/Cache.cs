using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using RimWorld.Planet;

namespace zed_0xff.CPS;

class Cache : WorldComponent
{
    private static Dictionary<int, Dictionary<IntVec3, Building_Base>> mapPosHash = new Dictionary<int, Dictionary<IntVec3, Building_Base>>();

    public Cache(World w) : base(w)
    {
        // clear caches whenever a game is created or loaded.
        mapPosHash.Clear();
    }

    public static void Add(Building_Base b){
        Add(b, b.Map);
    }

    public static void Add(Building_Base b, Map map){
        if( Prefs.DevMode ) Log.Message("[d] CPS: add " + b);
        if( !mapPosHash.ContainsKey(map.uniqueID) ){
            mapPosHash.Add(map.uniqueID, new Dictionary<IntVec3, Building_Base>());
        }
        foreach (IntVec3 cell in b.OccupiedRect()){
            mapPosHash[map.uniqueID][cell] = b;
        }
    }

    // should be faster than ThingGrid.ThingAt<T>
    // null-safe
    public static Building_Base Get(IntVec3 pos, Map map){
        if( pos == null || map == null )
            return null;

        if( !mapPosHash.ContainsKey(map.uniqueID) )
            return null;

        Building_Base t = null;
        if (mapPosHash[map.uniqueID].TryGetValue(pos, out t)){
            return t; // might also be null
        }

        return null;
    }

    public static void Remove(Building_Base b){
        if( Prefs.DevMode ) Log.Message("[d] CPS: removing " + b);
        if( mapPosHash.ContainsKey(b.Map.uniqueID) ){
            foreach (IntVec3 cell in b.OccupiedRect()){
                mapPosHash[b.Map.uniqueID][cell] = null;
            }
        }
    }
}
