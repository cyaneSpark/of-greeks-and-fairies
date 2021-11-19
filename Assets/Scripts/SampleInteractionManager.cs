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
        }

        public Func<Actor, Item, bool?> TryDeliver { get; set; }

        public void ToggleActor(Actor actor, bool on)
        {
            hands[actor].SetActive(on);
            LogInfo("Toggled {0} -> {1}", actor, on ? "ON" : "OFF");
        }

        public Item dummyItem;

        private void Update()
        {
            if (Input.GetKeyUp(KeyCode.G))
                TryDeliver(Actor.grandma, dummyItem);
            else if (Input.GetKeyUp(KeyCode.D))
                TryDeliver(Actor.doctor, dummyItem);
            else if (Input.GetKeyUp(KeyCode.P))
                TryDeliver(Actor.priest, dummyItem);
        }
    }
}