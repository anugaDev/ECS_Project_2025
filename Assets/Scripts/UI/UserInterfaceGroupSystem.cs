using System;
using System.Collections.Generic;
using System.Linq;
using ElementCommons;
using ScriptableObjects;
using Types;
using UI.Entities;
using UI.UIControllers;
using Units;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace UI
{
    [UpdateAfter(typeof(TransformSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial class UserInterfaceGroupSystem : SystemBase
    {
        private const float DEFAULT_FILL_AMOUNT = 0F;
        
        private SelectionActionsDisplayController selectionActionsController;

        private List<UnitUIGroupQueue> _trackedElementQueue;

        private SelectedGroupDisplayController _selectionGroupsController;
        
        private Dictionary<SelectableElementType, Action<Entity, bool>> _selectableToAction;
        
        private Dictionary<UnitType, int> _currentUnitSelection;

        protected override void OnCreate()
        {
            RequireForUpdate<UnitsConfigurationComponent>();
            RequireForUpdate<OwnerTagComponent>();
            InitializeSelectionDictionary();
            InitializeActionDictionary();
        }

        private void InitializeActionDictionary()
        {
            _selectableToAction = new Dictionary<SelectableElementType, Action<Entity, bool>>
            {
                [SelectableElementType.Building] = SetBuildingQueue,
                [SelectableElementType.Unit] = SetUnitGroup,
            };
        }

        private void InitializeSelectionDictionary()
        {
            _currentUnitSelection = new Dictionary<UnitType, int>
            {
                [UnitType.Archer] = 0,
                [UnitType.Ballista] = 0,
                [UnitType.Worker] = 0,
                [UnitType.Warrior] = 0
            };
        }

        private void SetUnitGroup(Entity entity, bool isSelected)
        {
            UnitType unitType = SystemAPI.GetComponent<UnitTypeComponent>(entity).Type;
            int currentSelectionCount = _currentUnitSelection[unitType];
            currentSelectionCount = GetCurrentSelectionCount(currentSelectionCount, isSelected);
            _currentUnitSelection[unitType] = currentSelectionCount;
        }

        private static int GetCurrentSelectionCount(int currentSelectionCount, bool isSelected)
        {
            if (!isSelected)
            {
                return GetNegativeSelectionCount(currentSelectionCount);
            }
            
            return currentSelectionCount + 1;
        }

        private static int GetNegativeSelectionCount(int currentSelectionCount)
        {
            if(currentSelectionCount <= 0)
            {
                return 0;
            }

            return currentSelectionCount - 1;
        }

        private void SetBuildingQueue(Entity entity, bool isSelected)
        {
            if(!isSelected)
            {
                return;
            }
            
            
        }

        protected override void OnStartRunning()
        {
            InitializeController();
            base.OnStartRunning();
        }

        private void InitializeController()
        {
            UnitsScriptableObject configuration = SystemAPI.ManagedAPI.GetSingleton<UnitsConfigurationComponent>().Configuration;
            _selectionGroupsController = UserInterfaceController.Instance.SelectedGroupController;
            _selectionGroupsController.SetUnitsGroups(configuration);
        }

        protected override void OnUpdate()
        {
            GetSelectedElements();
        }

        private void GetSelectedElements()
        {
            EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);
            foreach ((ElementSelectionComponent selectionComponent, SelectableElementTypeComponent typeComponent, Entity entity) 
                     in SystemAPI.Query<ElementSelectionComponent, SelectableElementTypeComponent>().WithEntityAccess())
            {
                if (!selectionComponent.MustUpdateGroup)
                {
                    continue;
                }

                ElementSelectionComponent newSelectionComponent = selectionComponent;
                newSelectionComponent.MustUpdateGroup = false;
                entityCommandBuffer.SetComponent(entity, newSelectionComponent);
                _selectableToAction[typeComponent.Type]?.Invoke(entity, selectionComponent.IsSelected);
            }

            entityCommandBuffer.Playback(EntityManager);
            UpdateSelectedGroups();
        }

        private void UpdateSelectedGroups()
        {
            foreach (UnitType unitType in _currentUnitSelection.Keys)
            {
                _selectionGroupsController.SetGroupValue(unitType, _currentUnitSelection[unitType]);
                _selectionGroupsController.SetGroupFill(unitType, DEFAULT_FILL_AMOUNT);
            }
        }
    }
}