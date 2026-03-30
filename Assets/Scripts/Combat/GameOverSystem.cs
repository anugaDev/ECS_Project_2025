using Types;
using UI.UIControllers;
using Unity.Entities;

namespace Combat
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
    public partial struct GameOverSystem : ISystem
    {
        private bool _gameOverHandled;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameOverTag>();
            _gameOverHandled = false;
        }

        public void OnUpdate(ref SystemState state)
        {
            if (_gameOverHandled)
                return;

            if (!SystemAPI.HasSingleton<GameOverTag>())
                return;

            GameOverTag gameOverTag = SystemAPI.GetSingleton<GameOverTag>();

            TeamType localTeam = TeamType.None;
            foreach (RefRO<UI.PlayerTeamComponent> playerTeam in
                     SystemAPI.Query<RefRO<UI.PlayerTeamComponent>>()
                              .WithAll<UI.PlayerTagComponent, ElementCommons.OwnerTagComponent>())
            {
                localTeam = playerTeam.ValueRO.Team;
                break;
            }

            bool isVictory = (localTeam != TeamType.None) && (localTeam == gameOverTag.WinnerTeam);

            GameOverScreenController screenController = GameOverScreenController.Instance;
            if (screenController != null)
            {
                screenController.Show(isVictory);
            }

            _gameOverHandled = true;
        }
    }
}
