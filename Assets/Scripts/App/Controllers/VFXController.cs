// Copyright (c) 2018 - Loom Network. All rights reserved.
// https://loomx.io/



using DG.Tweening;
using LoomNetwork.CZB.Common;
using LoomNetwork.Internal;
using System;
using UnityEngine;

namespace LoomNetwork.CZB
{
    public class VFXController : IController
    {
        private ISoundManager _soundManager;
        private ITimerManager _timerManager;
        private ILoadObjectsManager _loadObjectsManager;
        private IGameplayManager _gameplayManager;

        private ParticlesController _particlesController;

        public Sprite FeralFrame { get; set; }
        public Sprite HeavyFrame { get; set; }

        public void Init()
        {
            _timerManager = GameClient.Get<ITimerManager>();
            _soundManager = GameClient.Get<ISoundManager>();
            _loadObjectsManager = GameClient.Get<ILoadObjectsManager>();
            _gameplayManager = GameClient.Get<IGameplayManager>();
            _particlesController = _gameplayManager.GetController<ParticlesController>();

            FeralFrame = _loadObjectsManager.GetObjectByPath<Sprite>("Images/UnitFrames/feral");
            HeavyFrame = _loadObjectsManager.GetObjectByPath<Sprite>("Images/UnitFrames/heavy");
        }

        public void Dispose()
        {
        }

        public void Update()
        {
        }

        public void PlayAttackVFX(Enumerators.CardType type, Vector3 target, int damage)
        {
            GameObject effect;
            GameObject vfxPrefab;
            target = Utilites.CastVFXPosition(target);

            if (type == Enumerators.CardType.FERAL)
            {
                vfxPrefab = _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/FeralAttackVFX");
                effect = GameObject.Instantiate(vfxPrefab);
                effect.transform.position = target;
                _soundManager.PlaySound(Enumerators.SoundType.FERAL_ATTACK, Constants.CREATURE_ATTACK_SOUND_VOLUME, false, false, true);

                _particlesController.RegisterParticleSystem(effect, true, 5f);

                if (damage > 3 && damage < 7)
                {
                    _timerManager.AddTimer((a) =>
                    {
                        effect = GameObject.Instantiate(vfxPrefab);
                        effect.transform.position = target;
                        effect.transform.localScale = new Vector3(-1, 1, 1);
                        _particlesController.RegisterParticleSystem(effect, true, 5f);


                    }, null, 0.5f, false);
                }
                if (damage > 6)
                {
                    _timerManager.AddTimer((a) =>
                    {
                        effect = GameObject.Instantiate(vfxPrefab);
                        effect.transform.position = target - Vector3.right;
                        effect.transform.eulerAngles = Vector3.forward * 90;

                        _particlesController.RegisterParticleSystem(effect, true, 5f);

                    }, null, 1.0f, false);
                }
                //GameClient.Get<ITimerManager>().AddTimer((a) =>
                //{
                //    _soundManager.PlaySound(Enumerators.SoundType.FERAL_ATTACK, Constants.CREATURE_ATTACK_SOUND_VOLUME, false, false, true);
                //}, null, 0.75f, false);
            }
            else if (type == Enumerators.CardType.HEAVY)
            {
                var soundType = Enumerators.SoundType.HEAVY_ATTACK_1;
                var prefabName = "Prefabs/VFX/HeavyAttackVFX";
                if (damage > 4)
                {
                    prefabName = "Prefabs/VFX/HeavyAttack2VFX";
                    soundType = Enumerators.SoundType.HEAVY_ATTACK_2;
                }
                vfxPrefab = _loadObjectsManager.GetObjectByPath<GameObject>(prefabName);
                effect = GameObject.Instantiate(vfxPrefab);
                effect.transform.position = target;

                _particlesController.RegisterParticleSystem(effect, true, 5f);

                _soundManager.PlaySound(soundType, Constants.CREATURE_ATTACK_SOUND_VOLUME, false, false, true);
                /* GameClient.Get<ITimerManager>().AddTimer((a) =>
                     {
                     }, null, 0.75f, false);*/
            }
            else
            {
                vfxPrefab = _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/WalkerAttackVFX");
                effect = GameObject.Instantiate(vfxPrefab);
                effect.transform.position = target;

                _particlesController.RegisterParticleSystem(effect, true, 5f);

                if (damage > 4)
                {
                    _timerManager.AddTimer((a) =>
                    {
                        effect = GameObject.Instantiate(vfxPrefab);
                        effect.transform.position = target;

                        effect.transform.localScale = new Vector3(-1, 1, 1);
                        _particlesController.RegisterParticleSystem(effect, true, 5f);


                    }, null, 0.5f, false);
                    //  GameClient.Get<ITimerManager>().AddTimer((a) =>
                    //  {
                    _soundManager.PlaySound(Enumerators.SoundType.WALKER_ATTACK_2, Constants.CREATURE_ATTACK_SOUND_VOLUME, false, false, true);
                    // }, null, 0.75f, false);
                }
                else
                {
                    //    GameClient.Get<ITimerManager>().AddTimer((a) =>
                    //   {
                    _soundManager.PlaySound(Enumerators.SoundType.WALKER_ATTACK_1, Constants.CREATURE_ATTACK_SOUND_VOLUME, false, false, true);
                    //     }, null, 0.75f, false);
                }
            }

        }

        public void CreateVFX(Enumerators.SetType setType, Vector3 position, bool autoDestroy = true, float delay = 3f)
        {
            GameObject prefab = null;

            switch (setType)
            {
                case Enumerators.SetType.WATER:
                    prefab = _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/FireBolt_ImpactVFX");
                    break;
                case Enumerators.SetType.TOXIC:
                    prefab = _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/Toxic_ImpactVFX");
                    break;
                case Enumerators.SetType.FIRE:
                    prefab = _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/FireBolt_ImpactVFX");
                    break;
                case Enumerators.SetType.LIFE:
                    prefab = _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/HealingTouchVFX");
                    break;
                case Enumerators.SetType.EARTH:
                    prefab = _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/HealingTouchVFX"); // todo improve particle
                    break;
                case Enumerators.SetType.AIR:
                    prefab = _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/PushVFX");
                    break;
                default:
                    break;
            }

            var particle = MonoBehaviour.Instantiate(prefab);
            particle.transform.position = Utilites.CastVFXPosition(position + Vector3.forward);
            _particlesController.RegisterParticleSystem(particle, autoDestroy, delay);
        }


        public void CreateVFX(GameObject prefab, Vector3 position, bool autoDestroy = true, float delay = 3f)
        {
            var particle = MonoBehaviour.Instantiate(prefab);
            particle.transform.position = Utilites.CastVFXPosition(position + Vector3.forward);
            _particlesController.RegisterParticleSystem(particle, autoDestroy, delay);
        }

        public void CreateSkillVFX(Enumerators.SetType setType, Vector3 from, object target, Action<object> callbackComplete)
        {
            GameObject prefab = null;

            switch (setType)
            {
                case Enumerators.SetType.WATER:
                    prefab = _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/FreezeVFX");
                    break;
                case Enumerators.SetType.TOXIC:
                    prefab = _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/ToxicAttackVFX");
                    break;
                case Enumerators.SetType.FIRE:
                    prefab = _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/FireBoltVFX");
                    break;
                case Enumerators.SetType.LIFE:
                    prefab = _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/HealingTouchVFX");
                    break;
                case Enumerators.SetType.EARTH:
                    prefab = _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/HealingTouchVFX"); // todo improve particle
                    break;
                case Enumerators.SetType.AIR:
                    prefab = _loadObjectsManager.GetObjectByPath<GameObject>("Prefabs/VFX/Skills/PushVFX");
                    break;
                default:
                    break;
            }
 
            if (target == null)
                return;

            var particleSystem = MonoBehaviour.Instantiate(prefab);
            particleSystem.transform.position = Utilites.CastVFXPosition(from + Vector3.forward);

            if (target is Player)
            {
                particleSystem.transform.DOMove(Utilites.CastVFXPosition((target as Player).AvatarObject.transform.position), .5f).OnComplete(() =>
                {
                    callbackComplete(target);

                    if (particleSystem != null)
                        MonoBehaviour.Destroy(particleSystem);
                });
            }
            else if (target is BoardUnit)
            {
                particleSystem.transform.DOMove(Utilites.CastVFXPosition((target as BoardUnit).transform.position), .5f).OnComplete(() =>
                {
                    callbackComplete(target);

                    if (particleSystem != null)
                        MonoBehaviour.Destroy(particleSystem);
                });
            }
        }
    }
}