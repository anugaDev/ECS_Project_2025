using ElementCommons;
using Types;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIControllers
{
    public class GameMenuController : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject _rootPanel;

        [Header("Buttons")]
        [SerializeField] private Button _exitButton;

        [SerializeField] private Button _resumeButton;

        private void Awake()
        {
            if (_rootPanel != null)
                _rootPanel.SetActive(false);

            if (_exitButton != null)
                _exitButton.onClick.AddListener(OnExitClicked);

            if (_resumeButton != null)
                _resumeButton.onClick.AddListener(OnResumeClicked);
        }

        private void OnDestroy()
        {
            if (_exitButton != null)
                _exitButton.onClick.RemoveListener(OnExitClicked);

            if (_resumeButton != null)
                _resumeButton.onClick.RemoveListener(OnResumeClicked);
        }

        public void SetVisible(bool visible)
        {
            if (_rootPanel != null)
                _rootPanel.SetActive(visible);
        }

        private void OnResumeClicked() => SetVisible(false);

        private void OnExitClicked()
        {
            World defaultWorld = World.DefaultGameObjectInjectionWorld;
            if (defaultWorld == null)
            {
                return;
            }

            EntityManager em = defaultWorld.EntityManager;

            EntityQuery localPlayerQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<PlayerTagComponent>(),
                ComponentType.ReadOnly<OwnerTagComponent>(),
                ComponentType.ReadOnly<PlayerTeamComponent>()
            );

            if (localPlayerQuery.IsEmpty)
            {
                localPlayerQuery.Dispose();
                return;
            }

            Entity playerEntity = localPlayerQuery.GetSingletonEntity();
            TeamType localTeam  = em.GetComponentData<PlayerTeamComponent>(playerEntity).Team;
            localPlayerQuery.Dispose();

            EntityQuery networkQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<Unity.NetCode.NetworkId>(),
                ComponentType.ReadOnly<Unity.NetCode.NetworkStreamInGame>()
            );

            if (networkQuery.IsEmpty)
            {
                networkQuery.Dispose();
                SetVisible(false);
                return;
            }

            Entity networkEntity = networkQuery.GetSingletonEntity();
            networkQuery.Dispose();

            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
            Entity rpcEntity = ecb.CreateEntity();
            ecb.AddComponent(rpcEntity, new PlayerManualExitTag { ExitingTeam = localTeam });
            ecb.AddComponent(rpcEntity, new Unity.NetCode.SendRpcCommandRequest { TargetConnection = networkEntity });
            ecb.Playback(em);
            ecb.Dispose();
            SetVisible(false);
        }
    }
}
