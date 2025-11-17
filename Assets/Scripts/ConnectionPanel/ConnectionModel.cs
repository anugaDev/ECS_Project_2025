using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
using UnityEngine.SceneManagement;

namespace ConnectionPanel
{
    public class ConnectionModel
    {
        private const int GAME_SCENE_INDEX = 1;
    
        private const string SERVER_WORLD_NAME = "SERVER_WORLD";
    
        private const string CLIENT_WORLD_NAME = "CLIENT_WORLD";
    
        private List<Action> _connectionActions;
        
        private string _address;
        
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
            SceneManager.LoadScene(GAME_SCENE_INDEX);
        }

        private void ShowServerError()
        {
            throw new ArgumentException("Connection Error: Unknown connection mode");
        }

        private void DestroyLocalSimulationWorld()
        {
            foreach (World world in World.All)
            {
                DisposeWorld(world);
            }
        }

        private void DisposeWorld(World world)
        {
            if (world.Flags != WorldFlags.Game)
            {
                return;
            }

            world.Dispose();
        }

        private void StartClient()
        {
            World clientWorld = ClientServerBootstrap.CreateClientWorld(CLIENT_WORLD_NAME);
            var networkEndpoint = NetworkEndpoint.Parse(_address, _port);
            ComponentType requiredComponents = ComponentType.ReadWrite<NetworkStreamDriver>();
            EntityQuery entityQuery = clientWorld.EntityManager.CreateEntityQuery(requiredComponents);
            entityQuery.GetSingletonRW<NetworkStreamDriver>().ValueRW.Connect(clientWorld.EntityManager, networkEndpoint);
            World.DefaultGameObjectInjectionWorld = clientWorld;
        }

        private void StartServer()
        {
            World serverWorld = ClientServerBootstrap.CreateServerWorld(SERVER_WORLD_NAME);
            ushort parsedPort = _port;
            NetworkEndpoint serverEndPoint = NetworkEndpoint.AnyIpv4.WithPort(parsedPort);
            ComponentType requiredComponents = ComponentType.ReadWrite<NetworkStreamDriver>();
            EntityQuery entityQuery = serverWorld.EntityManager.CreateEntityQuery(requiredComponents);
            entityQuery.GetSingletonRW<NetworkStreamDriver>().ValueRW.Listen(serverEndPoint);
        }
    }
}