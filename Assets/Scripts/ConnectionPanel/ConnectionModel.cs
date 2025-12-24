using System;
using System.Collections.Generic;
using Client;
using Types;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
using UnityEngine.SceneManagement;

namespace ConnectionPanel
{
    public class ConnectionModel
    {
        private List<Action> _connectionActions;

        private World _clientWorld;

        private string _address;

        private TeamType _team;

        private ushort _port;

        public ConnectionModel()
        {
            _connectionActions = new List<Action>
            {
                HostServer,
                StartServer,
                StartClient
            };
        }

        public void SetAddress(string address)
        {
            _address = address;
        }

        public void SetPort(ushort port)
        {
            _port = port;
        }

        private void HostServer()
        {
            StartServer();
            StartClient();
        }
    
        public void StartConnection(int connectionValue)
        {
            LoadNewGame();
        
            if (_connectionActions.Count <= connectionValue)
            {
                ShowServerError();
            }

            _connectionActions[connectionValue]?.Invoke();
        }

        private void LoadNewGame()
        {
            DestroyLocalSimulationWorld();
            SceneManager.LoadScene(GlobalParameters.GAME_SCENE_INDEX);
        }

        private void ShowServerError()
        {
            throw new ArgumentException("Connection Error: Unknown connection mode");
        }

        private void DestroyLocalSimulationWorld()
        {
            foreach (var world in World.All)
            {
                if (world.Flags == WorldFlags.Game)
                {
                    world.Dispose();
                    break;
                }
            }
        }

        private void StartServer()
        {
            World serverWorld = ClientServerBootstrap.CreateServerWorld(GlobalParameters.SERVER_WORLD_NAME);
            NetworkEndpoint serverEndPoint = NetworkEndpoint.AnyIpv4.WithPort(_port);
            { 
                EntityQuery entityQuery = serverWorld.EntityManager.CreateEntityQuery
                    (ComponentType.ReadWrite<NetworkStreamDriver>()); 
                entityQuery.GetSingletonRW<NetworkStreamDriver>().ValueRW.Listen(serverEndPoint);
            }
        }

        private void StartClient()
        {
            _clientWorld = ClientServerBootstrap.CreateClientWorld(GlobalParameters.CLIENT_WORLD_NAME);
            NetworkEndpoint networkEndpoint = NetworkEndpoint.Parse(_address, _port);
            {
                EntityQuery entityQuery = _clientWorld.EntityManager.CreateEntityQuery
                    (ComponentType.ReadWrite<NetworkStreamDriver>());
                entityQuery.GetSingletonRW<NetworkStreamDriver>().ValueRW.Connect(_clientWorld.EntityManager, networkEndpoint);
            }
            
            World.DefaultGameObjectInjectionWorld = _clientWorld;
        }

        public void SetTeam(int team)
        {
            _team = (TeamType)team;
            Entity teamRequestEntity = _clientWorld.EntityManager.CreateEntity();
            _clientWorld.EntityManager.AddComponentData(teamRequestEntity, GetClientRequest());
        }

        private ClientTeamRequest GetClientRequest()
        {
            return new ClientTeamRequest
            {
                Value = _team,
            };
        }
    }
}