using Buildings;
using ElementCommons;
using Types;
using UI;
using Units;
using Unity.Entities;

namespace Combat
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
    public partial struct VictoryConditionSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerTagComponent>();
        }

        public void OnUpdate(ref SystemState state)
        {
            if (SystemAPI.HasSingleton<GameOverTag>())
                return;

            foreach (RefRO<PlayerManualExitTag> exitTag in
                     SystemAPI.Query<RefRO<PlayerManualExitTag>>())
            {
                TeamType exitingTeam = exitTag.ValueRO.ExitingTeam;
                TeamType winnerTeam  = exitingTeam == TeamType.Blue ? TeamType.Red : TeamType.Blue;
                CreateGameOverSingleton(ref state, winnerTeam);
                return;
            }

            if (!SystemAPI.HasSingleton<DestroyEntityTag>())
                return;

            bool blueHasWorker     = false;
            bool redHasWorker      = false;
            bool blueHasTownCenter = false;
            bool redHasTownCenter  = false;

            foreach ((RefRO<UnitTypeComponent> unitType, RefRO<ElementTeamComponent> team) in
                     SystemAPI.Query<RefRO<UnitTypeComponent>, RefRO<ElementTeamComponent>>()
                              .WithNone<DestroyEntityTag>())
            {
                if (unitType.ValueRO.Type != UnitType.Worker)
                    continue;

                if (team.ValueRO.Team == TeamType.Blue)
                    blueHasWorker = true;
                else if (team.ValueRO.Team == TeamType.Red)
                    redHasWorker = true;
            }

            foreach ((RefRO<BuildingTypeComponent> buildingType, RefRO<ElementTeamComponent> team) in
                     SystemAPI.Query<RefRO<BuildingTypeComponent>, RefRO<ElementTeamComponent>>()
                              .WithNone<DestroyEntityTag>())
            {
                if (buildingType.ValueRO.Type != BuildingType.Center)
                    continue;

                if (team.ValueRO.Team == TeamType.Blue)
                    blueHasTownCenter = true;
                else if (team.ValueRO.Team == TeamType.Red)
                    redHasTownCenter = true;
            }

            TeamType eliminatedTeam = TeamType.None;

            if (!blueHasWorker && !blueHasTownCenter)
                eliminatedTeam = TeamType.Blue;
            else if (!redHasWorker && !redHasTownCenter)
                eliminatedTeam = TeamType.Red;

            if (eliminatedTeam == TeamType.None)
                return;

            TeamType winner = eliminatedTeam == TeamType.Blue ? TeamType.Red : TeamType.Blue;
            CreateGameOverSingleton(ref state, winner);
        }

        private void CreateGameOverSingleton(ref SystemState state, TeamType winnerTeam)
        {
            EntityCommandBuffer ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);
            Entity gameOverEntity   = ecb.CreateEntity();
            ecb.AddComponent(gameOverEntity, new GameOverTag { WinnerTeam = winnerTeam });
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}
