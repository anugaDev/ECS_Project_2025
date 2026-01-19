using System.Collections.Generic;
using Types;

namespace UI.UIControllers
{
    public class UnitUIGroupQueue
    {
        private int _buildingIndex;
        
        private Dictionary<UnitType, UITypeGroup> _unitsQueue;
    }
}