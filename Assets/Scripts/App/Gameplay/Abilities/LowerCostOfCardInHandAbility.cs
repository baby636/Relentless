using Loom.ZombieBattleground.Common;
using Loom.ZombieBattleground.Data;
using System.Collections.Generic;

namespace Loom.ZombieBattleground
{
    public class LowerCostOfCardInHandAbility : AbilityBase
    {
        public int Value;

        public LowerCostOfCardInHandAbility(Enumerators.CardKind cardKind, AbilityData ability)
            : base(cardKind, ability)
        {
            Value = ability.Value;
        }

        public override void Activate()
        {
            base.Activate();

            if (AbilityCallType != Enumerators.AbilityCallType.ENTRY)
                return;

            Action();

            AbilitiesController.ThrowUseAbilityEvent(MainWorkingCard, new List<BoardObject>(), AbilityData.AbilityType, Protobuf.AffectObjectType.Card);
        }

        public override void Action(object info = null)
        {
            base.Action(info);

            CardsController.LowGooCostOfCardInHand(PlayerCallerOfAbility, null, Value);
        }
    }
}
