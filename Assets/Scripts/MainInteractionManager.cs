using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Fairies;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using static DG.Tweening.DOTween;

namespace Fairies
{
    public class MainInteractionManager : BaseBehaviour, IInteractionManager
    {
        public GameObject hand_grandma, hand_doctor, hand_priest;
        private Dictionary<Actor, GameObject> hands = new Dictionary<Actor, GameObject>();
        
        public event EventHandler<IInteractionManager.DeliveryArgs> onTryToDeliver;

        private void TryDeliver(Actor actor, Item item) =>
            onTryToDeliver?.Invoke(this, new IInteractionManager.DeliveryArgs(actor, item));
        
        public void Test(SelectEnterEventArgs args)
        {
            ItemMono item = args.interactable.GetComponent<ItemMono>();

            Actor actor = Actor.doctor;

            if (args.interactor.CompareTag("Doc"))
            {
                actor = Actor.doctor;

            }
            else if (args.interactor.CompareTag("Granny"))
            {
                actor = Actor.grandma;
            }
            else if(args.interactor.CompareTag("Priest"))
            {
                actor = Actor.priest;
            }
            else
            {
                Debug.LogError("WHO IS SENDING THIS? INVALID ACTOR TAG!", args.interactor);
                return;
            }
            
            if (item != null)
            {
                
                Debug.Log("SOMEONE THREW" + item.ID + "IN ME!");
            }
            else
            {
                Debug.Log("SOMEONE THREW NOTHING IN ME!");
            }
        }


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
                    hands[actor].transform.DOLocalMoveX(-0.4f, 0.8f).SetEase(Ease.InOutElastic);
                    break;
                case IInteractionManager.ActorMotion.Reject:
                    LogInfo("ANIMATE {0} REJECT", actor);
                    break;
                case IInteractionManager.ActorMotion.Disappear:
                    hands[actor].transform.DOLocalMoveX(-0.8f, 0.8f).SetEase(Ease.OutExpo).OnComplete(() =>
                    {
                        hands[actor].SetActive(false);
                    });
                    break;
                default:
                    break;
            }
        }

        
    }
}