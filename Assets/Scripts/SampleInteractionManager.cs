using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fairies.Dev
{
    public class SampleInteractionManager : BaseBehaviour, IInteractionManager
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

        public Item dummyItem;

        public event EventHandler<IInteractionManager.DeliveryArgs> onTryToDeliver;

        private void Update()
        {
            if (Input.GetKeyUp(KeyCode.G))
                TryDeliver(Actor.grandma, dummyItem);
            else if (Input.GetKeyUp(KeyCode.D))
                TryDeliver(Actor.doctor, dummyItem);
            else if (Input.GetKeyUp(KeyCode.P))
                TryDeliver(Actor.priest, dummyItem);
        }

        private void TryDeliver(Actor actor, Item item) =>
            onTryToDeliver?.Invoke(this, new IInteractionManager.DeliveryArgs(actor, item));
        public void HandleActorMotion(Actor actor, IInteractionManager.ActorMotion motion)
        {
            if (actor == Actor.INVALID)
            {
                LogError("INVALID ACTOR");
                return;
            }

            switch (motion)
            {
                case IInteractionManager.ActorMotion.Appear:
                    LogInfo("ANIMATE {0} APPEAR", actor);
                    hands[actor].SetActive(true);
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
    }
}