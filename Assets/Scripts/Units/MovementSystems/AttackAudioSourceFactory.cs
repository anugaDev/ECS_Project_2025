using System.Collections.Generic;
using Audio;
using Types;

namespace Units.MovementSystems
{
    public class AttackAudioSourceFactory
    {
        private Dictionary<UnitType, AudioSourceEnum> _unitToAttackAudioDictionary;

        public AttackAudioSourceFactory()
        {
            _unitToAttackAudioDictionary = new Dictionary<UnitType, AudioSourceEnum>
            {
                [UnitType.Worker] = AudioSourceEnum.SwordSwing,
                [UnitType.Warrior] = AudioSourceEnum.SwordSwing,
                [UnitType.Archer] = AudioSourceEnum.ArcherShot,
                [UnitType.Ballista] = AudioSourceEnum.ArcherShot
            };
        }

        public AudioSourceEnum Get(UnitType unitType)
        {
            return _unitToAttackAudioDictionary[unitType];
        }
    }
}