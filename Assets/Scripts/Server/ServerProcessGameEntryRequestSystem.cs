using Client;
using PlayerInputs;
using Units;
using Types;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace Server
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct ServerProcessGameEntryRequestSystem : ISystem
    {
        private EntityCommandBuffer _entityCommandBuffer;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<UnitPrefabComponent>();
            EntityQueryBuilder builder = new EntityQueryBuilder(Allocator.Temp).WithAll<TeamRequest, ReceiveRpcCommandRequest>();
            state.RequireForUpdate(state.GetEntityQuery(builder));
        }

        public void OnUpdate(ref SystemState state)
        {
            _entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);
            Entity unitEntity = SystemAPI.GetSingleton<UnitPrefabComponent>().Unit;
            foreach ((TeamRequest teamRequest, ReceiveRpcCommandRequest requestSource, Entity requestEntity) 
                     in SystemAPI.Query<TeamRequest, ReceiveRpcCommandRequest>().WithEntityAccess())
            {
                _entityCommandBuffer.DestroyEntity(requestEntity);
                _entityCommandBuffer.AddComponent<NetworkStreamInGame>(requestSource.SourceConnection);

                SetTeam(teamRequest);
                int clientId = SystemAPI.GetComponent<NetworkId>(requestSource.SourceConnection).Value;
                DebugTeam(clientId, teamRequest);
                
                Entity unit = InstantiateUnit(unitEntity, clientId, teamRequest.Team,requestSource);
                LinkedEntityGroup linkedEntityGroup = new LinkedEntityGroup();
                linkedEntityGroup.Value = unit;
                _entityCommandBuffer.AppendToBuffer(requestSource.SourceConnection, linkedEntityGroup);
            }
            _entityCommandBuffer.Playback(state.EntityManager);
        }

        private void DebugTeam(int clientId, TeamRequest teamRequest)
        {
            Debug.Log($"team received {clientId} to the {teamRequest.Team} team");
        }

        private void SetTeam(TeamRequest teamRequest)
        {
            TeamType teamRequestTeam = teamRequest.Team;
            //Assign other team on full
        }

        private Entity InstantiateUnit(Entity unitEntity, int clientId, TeamType team,
            ReceiveRpcCommandRequest requestSource)
        {
            Entity newUnit = _entityCommandBuffer.Instantiate(unitEntity);
            
            _entityCommandBuffer.SetName(newUnit,"BaseUnit");
            LocalTransform localTransform = GetUnitPosition(team);
            _entityCommandBuffer.SetComponent(newUnit, localTransform);
            _entityCommandBuffer.SetComponent(newUnit, GetGhostOwner(clientId));
            _entityCommandBuffer.SetComponent(newUnit, GetTargetPosition(localTransform));
            _entityCommandBuffer.SetComponent(newUnit, GetTeamComponent(team));
            return newUnit;
        }

        private UnitTargetPositionComponent GetTargetPosition(LocalTransform localTransform)
        {
            return new UnitTargetPositionComponent
            {
                Value = localTransform.Position
            };
        }

        private UnitTeamComponent GetTeamComponent(TeamType team)
        {
            return new UnitTeamComponent{Team = team};
        }

        private GhostOwner GetGhostOwner(int clientId)
        {
            return new GhostOwner
            {
                NetworkId = clientId
            };
        }

        private LocalTransform GetUnitPosition(TeamType team)
        {
            float3 unitPosition;
            if (team is TeamType.Red)
            {
                unitPosition = new float3(50f, 1f, 50);
            }
            else
            {
                unitPosition = new float3(-50f, 1f, -50);
            }
            return LocalTransform.FromPosition(unitPosition);
        }
    }
}