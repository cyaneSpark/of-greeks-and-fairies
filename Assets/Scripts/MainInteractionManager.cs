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
        public event EventHandler<bool> onPauseToggled;

        private void TryDeliver(Actor actor, Item item) =>
            onTryToDeliver?.Invoke(this, new IInteractionManager.DeliveryArgs(actor, item));

        public void TryDeliverObject(SelectEnterEventArgs args)
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
            else if (args.interactor.CompareTag("Priest"))
            {
                actor = Actor.priest;
            }
            else
            {
                Debug.LogError("WHO IS SENDING THIS? INVALID ACTOR TAG!", args.interactor);
                TryDeliver(actor, Item.INVALID);
            }

            if (item != null)
            {
                Debug.Log("SOMEONE THREW" + item.ID + "IN ME!");
                TryDeliver(actor, item.ID);
                item.transform.SetParent(args.interactor.transform.GetChild(0));
                item.GetComponent<Rigidbody>().isKinematic = true;
            }
            else
            {
                Debug.Log("SOMEONE THREW NOTHING IN ME!");
                TryDeliver(actor, Item.INVALID);
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

                    var targets = new List<XRBaseInteractable>();
                    XRSocketInteractor interactor = hands[actor].GetComponent<XRSocketInteractor>();
                    interactor.GetValidTargets(targets);
                    interactor.socketActive = false;
                    interactor.transform
                        .DOLocalRotate(
                            new Vector3(interactor.transform.eulerAngles.x, interactor.transform.eulerAngles.y, 45f),
                            0.5f).SetDelay(2f)
                        .OnComplete(
                            () =>
                            {
                                targets[0].transform.SetParent(null);
                                targets[0].transform.DOJump(new Vector3(0, 1, 0), 0.1f, 1, 2f)
                                    .OnComplete(() =>
                                    {
                                        interactor.socketActive = true;
                                        targets[0].GetComponent<Rigidbody>().isKinematic = false;
                                    });
                                interactor.transform.DOLocalRotate(
                                    new Vector3(interactor.transform.eulerAngles.x, interactor.transform.eulerAngles.y,
                                        0), 0.5f);
                            });
                    Debug.Log("THROWING IT OUT!");
                    break;
                case IInteractionManager.ActorMotion.Disappear:
                    hands[actor].transform.DOLocalMoveX(-0.8f, 0.8f).SetEase(Ease.OutExpo).OnComplete(() =>
                    {
                        LogInfo("ANIMATE {0} DISSAPEAR", actor);
                        hands[actor].SetActive(false);
                    });
                    break;
                case IInteractionManager.ActorMotion.Partial:
                    LogInfo("PARTIAL AGREEMENT {0}", actor);
                    break;
                default:
                    break;
            }
        }
    }
}