using System;
using System.Collections.Generic;
using DeepEqual.Syntax;
using Loom.ZombieBattleground.Common;
using Loom.ZombieBattleground.Data;
using Loom.ZombieBattleground.Helpers;
using Loom.ZombieBattleground.Protobuf;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using AbilityData = Loom.ZombieBattleground.Data.AbilityData;
using Card = Loom.ZombieBattleground.Data.Card;
using PictureTransform = Loom.ZombieBattleground.Data.PictureTransform;
using Deck = Loom.ZombieBattleground.Data.Deck;

namespace Loom.ZombieBattleground.Test
{
    [Category("EditQuickSubset")]
    public class DataTest
    {
        [Test]
        public void MouldIdSerialization()
        {
            MouldId original = new MouldId(long.MaxValue);
            string serialized = JsonConvert.SerializeObject(original);
            MouldId deserialized = JsonConvert.DeserializeObject<MouldId>(serialized);
            Assert.AreEqual(original, deserialized);
        }
        [Test]
        public void DeckProtobufSerialization()
        {
            Deck original = new Deck(
                new DeckId(1),
                new OverlordId(2),
                "deck name",
                new List<DeckCardData>
                {
                    new DeckCardData(new MouldId(1), 3),
                    new DeckCardData(new MouldId(2), 4)
                },
                Enumerators.Skill.HEALING_TOUCH,
                Enumerators.Skill.MEND
            );

            Deck deserialized = original.ToProtobuf().FromProtobuf();
            original.ShouldDeepEqual(deserialized);
        }

        [Test]
        public void CardProtobufSerialization()
        {
            Card card = new Card(
                new MouldId(123),
                "Foo",
                3,
                "description",
                "flavor",
                "awesomePicture",
                4,
                5,
                Enumerators.Faction.ITEM,
                "awesomeFrame",
                Enumerators.CardKind.CREATURE,
                Enumerators.CardRank.GENERAL,
                Enumerators.CardType.WALKER,
                new List<AbilityData>
                {
                    CreateAbilityData(true,
                        () => new List<AbilityData.ChoosableAbility>
                        {
                            new AbilityData.ChoosableAbility("choosable ability 1", CreateAbilityData(false, null), ""),
                            new AbilityData.ChoosableAbility("choosable ability 2", CreateAbilityData(false, null), "")
                        })
                },
                new PictureTransform(
                    new FloatVector3(0.3f, 0.4f, 0.5f),
                    FloatVector3.One
                ),
                Enumerators.UniqueAnimation.ShammannArrival,
                true
            );

            WorkingCard original = new WorkingCard(card, card, null, new Data.InstanceId(373));
            WorkingCard deserialized = original.ToProtobuf().FromProtobuf(null);
            original.ShouldDeepEqual(deserialized);
        }

        [Test]
        public void OverlordProtobufSerialization()
        {
            Protobuf.Overlord protobuf = new Protobuf.Overlord
            {
                OverlordId = 1,
                Icon = "icon",
                Name = "name",
                ShortDescription = "short desc",
                LongDescription = "long desc",
                Experience = 100500,
                Level = 373,
                Faction = Protobuf.Faction.Types.Enum.Life,
                Skills =
                {
                    new Skill
                    {
                        Id = 333,
                        Title = "title",
                        IconPath = "supericon",
                        Description = "desc",
                        Cooldown = 1,
                        InitialCooldown = 2,
                        Value = 3,
                        Damage = 4,
                        Count = 5,
                        Skill_ = Protobuf.OverlordSkill.Types.Enum.Freeze,
                        SkillTargets =
                        {
                            SkillTarget.Types.Enum.Opponent,
                            SkillTarget.Types.Enum.AllCards
                        },
                        TargetUnitSpecialStatus = UnitSpecialStatus.Types.Enum.Frozen,
                        TargetFactions =
                        {
                            Protobuf.Faction.Types.Enum.Fire,
                            Protobuf.Faction.Types.Enum.Life
                        },
                        Unlocked = true,
                        CanSelectTarget = true,
                        SingleUse = true
                    }
                },
                InitialDefense = 50
            };

            OverlordModel client = new OverlordModel(
                new OverlordId(1),
                "icon",
                "name",
                "short desc",
                "long desc",
                100500,
                373,
                Enumerators.Faction.LIFE,
                new List<Data.OverlordSkill>
                {
                    new Data.OverlordSkill(
                        333,
                        "title",
                        "supericon",
                        "desc",
                        1,
                        2,
                        3,
                        4,
                        5,
                        Enumerators.Skill.FREEZE,
                        new List<Enumerators.SkillTarget>
                        {
                            Enumerators.SkillTarget.OPPONENT,
                            Enumerators.SkillTarget.ALL_CARDS
                        },
                        Enumerators.UnitSpecialStatus.FROZEN,
                        new List<Enumerators.Faction>
                        {
                            Enumerators.Faction.FIRE,
                            Enumerators.Faction.LIFE
                        },
                        true,
                        true,
                        true
                    )
                },
                50
            );

            client.ShouldDeepEqual(protobuf.FromProtobuf());
        }

        private static AbilityData CreateAbilityData(bool includeChoosableAbility, Func<List<AbilityData.ChoosableAbility>> choosableAbilityFunc)
        {
            List<AbilityData.ChoosableAbility> choosableAbilities = new List<AbilityData.ChoosableAbility>();
            if (includeChoosableAbility)
            {
                choosableAbilities = choosableAbilityFunc();
            }

            return
                new AbilityData(
                    Enumerators.AbilityType.RAGE,
                    Enumerators.AbilityActivity.ACTIVE,
                    Enumerators.AbilityTrigger.IN_HAND,
                    new List<Enumerators.Target>
                    {
                        Enumerators.Target.ITSELF,
                        Enumerators.Target.PLAYER
                    },
                    Enumerators.Stat.DAMAGE,
                    Enumerators.Faction.TOXIC,
                    Enumerators.AbilityEffect.TARGET_ROCK,
                    Enumerators.AttackRestriction.ONLY_DIFFERENT,
                    Enumerators.CardType.WALKER,
                    Enumerators.UnitSpecialStatus.FROZEN,
                    Enumerators.CardType.HEAVY,
                    1,
                    2,
                    3,
                    "nice name",
                    4,
                    5,
                    6,
                    new List<AbilityData.VisualEffectInfo>
                    {
                        new AbilityData.VisualEffectInfo(Enumerators.VisualEffectType.Impact, "path1"),
                        new AbilityData.VisualEffectInfo(Enumerators.VisualEffectType.Moving, "path2")
                    },
                    Enumerators.GameMechanicDescription.Death,
                    Enumerators.Faction.LIFE,
                    Enumerators.AbilitySubTrigger.AllAllyUnitsInPlay,
                    choosableAbilities,
                    7,
                    8,
                    Enumerators.CardKind.CREATURE,
                    new List<Enumerators.GameMechanicDescription>
                    {
                        Enumerators.GameMechanicDescription.Death,
                        Enumerators.GameMechanicDescription.Aura
                    }
                );
        }
    }
}
