using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ConnectionPanel
{
    public class ConnectionPanelView : MonoBehaviour
    {
        [SerializeField] 
        private TMP_Dropdown _connectionDropDown;

        [SerializeField] 
        private TMP_InputField _addressField;

        [SerializeField] 
        private TMP_Dropdown _teamDrpDown;

        [SerializeField] 
        private TMP_InputField _portField;
    
        [SerializeField]
        private Text _buttonText;

        [SerializeField] 
        private Button _button;

        private ButtonTextFactory _buttonTextFactory;
        
        private int _teamValue;

        public event Action<int> OnConnection;

        private void OnEnable()
        {
            _buttonTextFactory = new ButtonTextFactory();
            AddListeners();
            OnConnectionDropDownSelection(_connectionDropDown.value);
        }

        private void AddListeners()
        {
            _connectionDropDown.onValueChanged.AddListener(OnConnectionDropDownSelection);
            _teamDrpDown.onValueChanged.AddListener(OnTeamSelected);
            _button.onClick.AddListener(OnButtonClicked);
        }

        private void OnButtonClicked()
        {
            OnConnection?.Invoke(_connectionDropDown.value);
        }

        private void OnDisable()
        {
            _connectionDropDown.onValueChanged.RemoveAllListeners();
            _teamDrpDown.onValueChanged.RemoveAllListeners();
        }

        private void OnConnectionDropDownSelection(int connectionValue)
        {
            _button.enabled = true;
            _buttonText.text = _buttonTextFactory.GetButtonText(connectionValue);
        }

        private void OnTeamSelected(int teamValue)
        { 
            _teamValue = teamValue;
        }
    
        public ushort GetParsedPort()
        {
            return ushort.Parse(_portField.text);
        }

        public string GetAddress()
        {
            return _addressField.text;
        }

        public int GetTeamValue()
        {
            return _teamValue;
        }
    }
}