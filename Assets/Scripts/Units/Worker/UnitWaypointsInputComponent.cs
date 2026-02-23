using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

namespace Units.Worker
{
    /// <summary>
    /// Records NavMesh path waypoints as input data so the server receives them
    /// and can follow the same path as the client (no NavMesh needed server-side).
    ///
    /// Written by NavMeshPathfindingSystem in GhostInputSystemGroup so the values
    /// are captured in the input record for each tick and delivered to the server
    /// via the NetCode command buffer.
    ///
    /// Max 8 waypoints covers the vast majority of NavMesh paths in an RTS.
    /// </summary>
    [GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
    public struct UnitWaypointsInputComponent : IInputComponentData
    {
        [GhostField] public int WaypointCount; // 0 = no active path
        [GhostField] public float3 W0;
        [GhostField] public float3 W1;
        [GhostField] public float3 W2;
        [GhostField] public float3 W3;
        [GhostField] public float3 W4;
        [GhostField] public float3 W5;
        [GhostField] public float3 W6;
        [GhostField] public float3 W7;

        public float3 GetWaypoint(int index) => index switch
        {
            0 => W0, 1 => W1, 2 => W2, 3 => W3,
            4 => W4, 5 => W5, 6 => W6, _ => W7
        };

        public void SetWaypoint(int index, float3 value)
        {
            switch (index)
            {
                case 0: W0 = value; break; case 1: W1 = value; break;
                case 2: W2 = value; break; case 3: W3 = value; break;
                case 4: W4 = value; break; case 5: W5 = value; break;
                case 6: W6 = value; break; default: W7 = value; break;
            }
        }
    }
}
