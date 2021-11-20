using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Fairies;
using UnityEngine;
using static DG.Tweening.DOTween;

namespace Fairies
{
    public class MainInteractionManager : BaseBehaviour, IInteractionManager
    {
        public GameObject hand_grandma, hand_doctor, hand_priest;
        private Dictionary<Actor, GameObject> hands = new Dictionary<Actor, GameObject>();

        private void Awake()
        {
            hands.Add(Actor.grandma, hand_grandma);
            hands.Add(Actor.doctor, hand_doctor);
            hands.Add(Actor.priest, hand_priest);

            foreach (GameObject gO in hands.Values)
                gO.SetActive(false);
        }

        public void HandleActorMotion(Actor actor, IInteractionManager.ActorMotion motion)
        {
            switch (motion)
            {
                case IInteractionManager.ActorMotion.Appear:
                    hands[actor].SetActive(true);
                    hands[actor].transform.DOLocalMoveZ(-0.4f, 1);
                    break;
                case IInteractionManager.ActorMotion.Reject:
                    LogInfo("ANIMATE {0} REJECT", actor);
                    break;
                case IInteractionManager.ActorMotion.Disappear:
                    LogInfo("ANIMATE {0} DISAPPEAR", actor);
                    hands[actor].SetActive(false);
                    break;
                default:
                    break;
            }
        }

        public event EventHandler<IInteractionManager.DeliveryArgs> onTryToDeliver;
    }
}