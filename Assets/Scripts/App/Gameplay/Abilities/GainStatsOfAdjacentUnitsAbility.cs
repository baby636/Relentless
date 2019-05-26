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
                RestoreGainedStats(AbilityUnitOwner, _addedDamage, _addedDefense);
                ResetStoredStats();
            }
        }

        protected override void BoardChangedHandler(int count)
        {
            base.BoardChangedHandler(count);

            if(AbilityUnitOwner.IsUnitActive && !AbilityUnitOwner.IsDead && AbilityUnitOwner.CurrentDefense > 0 && LastAuraState)
            {
                int oldAddedDefense = _addedDefense;
                int oldAddedDamage = _addedDamage;
                GainStats(AbilityUnitOwner, BattlegroundController.GetAdjacentUnitsToUnit(AbilityUnitOwner));
                RestoreGainedStats(AbilityUnitOwner, oldAddedDamage, oldAddedDefense);
            }
        }

        private void GainStats(CardModel boardUnit, List<CardModel> boardUnits)
        {
            _addedDefense = 0;
            _addedDamage = 0;

            foreach (CardModel cardModel in boardUnits)
            {
                _addedDefense += cardModel.CurrentDefense;
                _addedDamage += cardModel.CurrentDamage;
            }

            boardUnit.BuffedDefense += _addedDefense;
            boardUnit.AddToCurrentDefenseHistory(_addedDefense, Enumerators.ReasonForValueChange.AbilityBuff);
            boardUnit.BuffedDamage += _addedDamage;
            boardUnit.AddToCurrentDamageHistory(_addedDamage, Enumerators.ReasonForValueChange.AbilityBuff);
        }

        private void RestoreGainedStats(CardModel card, int addedDamage, int addedDefense)
        {
            card.BuffedDefense -= addedDefense;
            card.AddToCurrentDefenseHistory(-addedDefense, Enumerators.ReasonForValueChange.AbilityBuff);
            card.BuffedDamage -= addedDamage;
            card.AddToCurrentDamageHistory(-addedDamage, Enumerators.ReasonForValueChange.AbilityBuff);
        }

        private void ResetStoredStats ()
        {
            _addedDamage = 0;
            _addedDefense = 0;
        }
    }
}
