using UnityEngine;
using UnityEngine.Serialization;

namespace UI.UIControllers
{
    public class UserInterfaceController : MonoBehaviour
    {
        public static UserInterfaceController Instance; //TEMP

        [SerializeField]
        private SelectionBoxController _selectionBoxController;

        [SerializeField]
        private SelectionActionsDisplayController _selectionActionsDisplayerController;

        [SerializeField]
        private SelectedDetailsDisplayController _selectedDetailsController;

        public SelectionActionsDisplayController SelectionActionsDisplayerController => _selectionActionsDisplayerController;

        public SelectedDetailsDisplayController SelectedDetailsController => _selectedDetailsController;

        public SelectionBoxController SelectionBoxController => _selectionBoxController;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            _selectionBoxController.Disable();
        }
    }
}