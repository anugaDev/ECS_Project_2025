using System.Collections.Generic;
using Types;

namespace UI.Entities
{
    public class UnitUIGroupQueue
    {
        private int _buildingIndex;
        
        private Dictionary<UnitType, UITypeGroup> _unitsQueue;
    }
}