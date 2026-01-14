using Client;
using PlayerInputs;
using Units;
using Types;
using UI;
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
        private const float DEFAULT_UNIT_OFFSET = 5f;

        private EntityCommandBuffer _entityCommandBuffer;

        private int _currentUnitIndex;
        
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerPrefabComponent>();
            state.RequireForUpdate<UnitPrefabComponent>();
            EntityQueryBuilder builder = new EntityQueryBuilder(Allocator.Temp).WithAll<TeamRequest, ReceiveRpcCommandRequest>();
            state.RequireForUpdate(state.GetEntityQuery(builder));
        }

        public void OnUpdate(ref SystemState state)
        {
            _entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);
            Entity unitEntity = SystemAPI.GetSingleton<UnitPrefabComponent>().Worker;
            Entity playerPrefab = SystemAPI.GetSingleton<PlayerPrefabComponent>().Entity;

            foreach ((TeamRequest teamRequest, ReceiveRpcCommandRequest requestSource, Entity requestEntity)
                     in SystemAPI.Query<TeamRequest, ReceiveRpcCommandRequest>().WithEntityAccess())
            {
                _entityCommandBuffer.DestroyEntity(requestEntity);
                _entityCommandBuffer.AddComponent<NetworkStreamInGame>(requestSource.SourceConnection);
                int networkId = SystemAPI.GetComponent<NetworkId>(requestSource.SourceConnection).Value;
                DebugTeam(networkId, teamRequest);
                InstantiateInitialUnits(unitEntity, networkId, teamRequest, requestSource);
                Entity spawnPlayer = SpawnPlayer(networkId, teamRequest, playerPrefab, requestSource);
                LinkedEntityGroup linkedEntityGroup = new LinkedEntityGroup();
                linkedEntityGroup.Value = spawnPlayer;
                _entityCommandBuffer.AppendToBuffer(requestSource.SourceConnection, linkedEntityGroup);
            }
            _entityCommandBuffer.Playback(state.EntityManager);
        }

        private Entity SpawnPlayer(int networkId, TeamRequest teamRequest,
            Entity playerPrefab, ReceiveRpcCommandRequest requestSource)
        {
            Entity connection = requestSource.SourceConnection;
            Entity player = _entityCommandBuffer.Instantiate(playerPrefab);
            _entityCommandBuffer.SetName(player,"Player");
            _entityCommandBuffer.SetComponent(player, LocalTransform.FromPosition(float3.zero));
            _entityCommandBuffer.SetComponent(connection, new CommandTarget{targetEntity = player});
            _entityCommandBuffer.SetComponent(player, GetGhostOwner(networkId));
            _entityCommandBuffer.SetComponent(player, new PlayerTeamComponent{Team = teamRequest.Team});
            return player;
        }

        private void InstantiateInitialUnits(Entity unitEntity, int clientId, TeamRequest teamRequest,
            ReceiveRpcCommandRequest requestSource)
        {
            for (int i = 0; i < GlobalParameters.INITIAL_UNITS; i++)
            {
                _currentUnitIndex = i;
                Entity unit = InstantiateUnit(unitEntity, clientId, teamRequest.Team);
                LinkedEntityGroup linkedEntityGroup = new LinkedEntityGroup();
                linkedEntityGroup.Value = unit;
                _entityCommandBuffer.AppendToBuffer(requestSource.SourceConnection, linkedEntityGroup);
            }
        }

        private void DebugTeam(int clientId, TeamRequest teamRequest)
        {
            Debug.Log($"team received {clientId} to the {teamRequest.Team} team");
        }

        private Entity InstantiateUnit(Entity unitEntity, int clientId, TeamType team)
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

        private EntityTeamComponent GetTeamComponent(TeamType team)
        {
            return new EntityTeamComponent{Team = team};
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
            float unitOffset = _currentUnitIndex * DEFAULT_UNIT_OFFSET;
            if (team is TeamType.Red)
            {
                unitPosition = new float3(50f + unitOffset, GlobalParameters.DEFAULT_SCENE_HEIGHT, 50 + unitOffset);
            }
            else
            {
                unitPosition = new float3(-50f - unitOffset, GlobalParameters.DEFAULT_SCENE_HEIGHT, -50- unitOffset);
            }
            return LocalTransform.FromPosition(unitPosition);
        }
    }
}