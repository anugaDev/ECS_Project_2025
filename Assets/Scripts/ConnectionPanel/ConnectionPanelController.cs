using UnityEngine;

namespace ConnectionPanel
{
    public class ConnectionPanelController
    {
        private readonly ConnectionPanelView _view;
        
        private readonly ConnectionModel _model;

        public ConnectionPanelController(ConnectionModel model, ConnectionPanelView view)
        {
            _model = model;
            _view = view;
            InstallListeners();
        }

        private void InstallListeners()
        {
            _view.OnConnection += StartConnection;
        }

        private void StartConnection(int connectionId)
        {
            string address = _view.GetAddress();
            _model.SetAddress(address);
            
            ushort parsedPort = _view.GetParsedPort();
            _model.SetPort(parsedPort);

            _model.StartConnection(connectionId);
            
            int team = _view.GetTeamValue();
            _model.SetTeam(team);
        }
    }
}