using UnityEngine;

namespace ConnectionPanel
{
    public class ConnectionPanelInstaller : MonoBehaviour
    {
        [SerializeField] 
        private ConnectionPanelView _view;

        public void Awake()
        {
            ConnectionModel model = new ConnectionModel();
            ConnectionPanelController controller = new ConnectionPanelController(model, _view);
        }
    }
}