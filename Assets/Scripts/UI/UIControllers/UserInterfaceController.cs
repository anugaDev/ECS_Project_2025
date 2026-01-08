using UnityEngine;

namespace UI.UIControllers
{
    public class UserInterfaceController : MonoBehaviour
    {
        public static UserInterfaceController Instance; //TEMP

        [SerializeField]
        private SelectionBoxController _selectionBoxController;
        
        [SerializeField]
        private ActionDisplayController _actionDisplayerController;

        public ActionDisplayController ActionDisplayerController => _actionDisplayerController;

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