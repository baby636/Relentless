using Loom.ZombieBattleground.Common;
using Loom.ZombieBattleground.Data;
using System.Collections.Generic;

namespace Loom.ZombieBattleground
{
    public class GainStatsOfAdjacentUnitsAbility : AbilityBase
    {
        private int _addedDamage,
                    _addedDefense;

        public GainStatsOfAdjacentUnitsAbility(Enumerators.CardKind cardKind, AbilityData ability)
            : base(cardKind, ability)
        {
        }

        public override void Activate()
        {
            base.Activate();

            InvokeUseAbilityEvent();
        }

        protected override void ChangeAuraStatusAction(bool status)
        {
            base.ChangeAuraStatusAction(status);

            if (AbilityTrigger != Enumerators.AbilityTrigger.AURA)
                return;

            if (status)
            {
                GainStats(AbilityUnitOwner, BattlegroundController.GetAdjacentUnitsToUnit(AbilityUnitOwner));
            }
            else
            {
                RestoreGainedStats(AbilityUnitOwner);
            }
        }

        protected override void BoardChangedHandler(int count)
        {
            base.BoardChangedHandler(count);

            if(AbilityUnitOwner.IsUnitActive && !AbilityUnitOwner.IsDead)
            {
                RestoreGainedStats(AbilityUnitOwner);
                GainStats(AbilityUnitOwner, BattlegroundController.GetAdjacentUnitsToUnit(AbilityUnitOwner));
            }
        }

        private void GainStats(BoardUnitModel boardUnit, List<BoardUnitModel> boardUnits)
        {
            _addedDefense = 0;
            _addedDamage = 0;

            foreach (BoardUnitModel boardUnitModel in boardUnits)
            {
                _addedDefense += boardUnitModel.CurrentDefense;
                _addedDamage += boardUnitModel.CurrentDamage;
            }

            boardUnit.BuffedDefense += _addedDefense;
            boardUnit.AddToCurrentDefenseHistory(_addedDefense, Enumerators.ReasonForValueChange.AbilityBuff);
            boardUnit.BuffedDamage += _addedDamage;
            boardUnit.AddToCurrentDamageHistory(_addedDamage, Enumerators.ReasonForValueChange.AbilityBuff);
        }

        private void RestoreGainedStats(BoardUnitModel boardUnit)
        {
            boardUnit.BuffedDefense -= _addedDefense;
            boardUnit.AddToCurrentDefenseHistory(-_addedDefense, Enumerators.ReasonForValueChange.AbilityBuff);
            boardUnit.BuffedDamage -= _addedDamage;
            boardUnit.AddToCurrentDamageHistory(-_addedDamage, Enumerators.ReasonForValueChange.AbilityBuff);

            _addedDefense = 0;
            _addedDamage = 0;
        }
    }
}
