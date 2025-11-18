using Client;
using Types;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Server
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct ServerProcessGameEntryRequestSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            EntityQueryBuilder builder = new EntityQueryBuilder(Allocator.Temp).WithAll<ClientTeamRequest, ReceiveRpcCommandRequest>();
            state.RequireForUpdate(state.GetEntityQuery(builder));
        }

        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);

            foreach ((ClientTeamRequest teamRequest, ReceiveRpcCommandRequest requestSource, Entity requestEntity) 
                     in SystemAPI.Query<ClientTeamRequest, ReceiveRpcCommandRequest>().WithEntityAccess())
            {
                entityCommandBuffer.DestroyEntity(requestEntity);
                entityCommandBuffer.AddComponent<NetworkStreamInGame>(requestSource.SourceConnection);

                TeamType teamRequestTeam = teamRequest.Team;
                //Assign other team on full
                int clientId = SystemAPI.GetComponent<NetworkId>(requestSource.SourceConnection).Value;
                Debug.Log($"team received {clientId} to the {teamRequest.Team} team");
            }
            entityCommandBuffer.Playback(state.EntityManager);
        }
    }
}