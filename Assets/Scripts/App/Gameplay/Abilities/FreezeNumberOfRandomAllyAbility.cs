using System.Collections.Generic;
using Loom.ZombieBattleground.Common;
using Loom.ZombieBattleground.Data;
using Loom.ZombieBattleground.Helpers;
using UnityEngine;

namespace Loom.ZombieBattleground
{
    public class FreezeNumberOfRandomAllyAbility : AbilityBase
    {
        public int Value { get; }

        public int Turns { get; }

        public FreezeNumberOfRandomAllyAbility(Enumerators.CardKind cardKind, AbilityData ability)
            : base(cardKind, ability)
        {
            Value = ability.Value;
            Turns = ability.Turns;
        }

        public override void Activate()
        {
            base.Activate();

            if (AbilityCallType != Enumerators.AbilityCallType.ENTRY)
                return;

            VfxObject = LoadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/FrozenVFX");

            Action();
        }

        public override void Action(object info = null)
        {
            base.Action(info);

            List<object> allies = new List<object>();

            allies.AddRange(PlayerCallerOfAbility.BoardCards);
            allies.Remove(AbilityUnitViewOwner);
            allies.Add(PlayerCallerOfAbility);

            allies = InternalTools.GetRandomElementsFromList(allies, Value);

            for (int i = 0; i < allies.Count; i++)
            {
                switch (allies[i])
                {
                    case Player player:
                        player.Stun(Enumerators.StunType.FREEZE, Turns);
                        CreateVfx(player.AvatarObject.transform.position, true, 5f);
                        break;
                    case BoardUnitView unit:
                        unit.Model.Stun(Enumerators.StunType.FREEZE, Turns);
                        CreateVfx(unit.Transform.position, true, 5f);
                        break;
                }
            }
        }
    }
}
