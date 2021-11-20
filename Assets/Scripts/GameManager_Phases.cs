using System;
using System.Collections.Generic;

namespace Fairies
{
    public partial class GameManager
    {
        private Actor ClipToSpeaker(string storyBeatName)
        {
            string[] parts = storyBeatName.Split('_');
            Actor result = default;

            if (parts.Length != 2 || !Enum.TryParse(parts[1], out result))
                LogError("Invalid name ; {0}", storyBeatName);

            return result;
        }

        private string GetStoryBeatPath(Phase phase) => phase.ToString();
        private Dictionary<Phase, List<SingleRequest>> requests = new Dictionary<Phase, List<SingleRequest>>()
        {
            { Phase.tutorial, new List<SingleRequest>()
            {
                new SingleRequest(Actor.grandma, Item.sideritis_clove, 30, true), // Tutorial has no limit
            } },
            { Phase.intro, new List<SingleRequest>()
            {
                new SingleRequest(Actor.priest, Item.chamomile_clove),
                new SingleRequest(Actor.grandma, Item.honey_jar),
                new SingleRequest(Actor.doctor, Item.eucalyptus_branch),
            } },
            { Phase.cough, new List<SingleRequest>()
            {
                new SingleRequest(Actor.priest, Item.oil_cup),
                new SingleRequest(Actor.grandma, Item.salt_hand),
                new SingleRequest(Actor.doctor, Item.salt_pinch),
            } },
            { Phase.fever, new List<SingleRequest>()
            {
                new SingleRequest(Actor.doctor, Item.alcohol_cup),
                new SingleRequest(Actor.priest, Item.basil_clove),
                new SingleRequest(Actor.grandma, Item.basil_clove),

                new SingleRequest(Actor.grandma, Item.apiganos_clove),
            } },
            { Phase.delirium, new List<SingleRequest>()
            {
                new SingleRequest(Actor.doctor, Item.soda_chunk),
                new SingleRequest(Actor.grandma, Item.gunpowder_hand),
                new SingleRequest(Actor.priest, Item.mint_clove),

                new SingleRequest(Actor.doctor, Item.vitriol_chunk),
                new SingleRequest(Actor.grandma, Item.garlic_clove),
                new SingleRequest(Actor.priest, Item.sage_clove),
            } },
            { Phase.climax, new List<SingleRequest>()
            {
                new SingleRequest(Actor.doctor, Item.artemisian_flower),
                new SingleRequest(Actor.grandma, Item.bone_turtle),
                new SingleRequest(Actor.priest, Item.hay_clove),
            } },
            { Phase.outro, new List<SingleRequest>()
            {
                // No requests
            } },
        };

        private enum Phase
        {
            /// <summary>
            /// Act 1A
            /// Grandma welcomes and goes through the tutorial request.
            /// Light wind, distant thunder
            /// Grandpa has no voice.
            /// Ends with doctor & priest in the house.
            /// Duration :: 2 minutes
            /// </summary>
            tutorial,

            /// <summary>
            /// Act 1B
            /// "Cinematic" (introductions)
            /// Each character requests a basic thing to get the player accustomed to their voice
            /// Could be a general purpose medicine, no real pressure
            /// Duration :: 3 minutes
            /// </summary>
            intro,

            /// <summary>
            /// Act 2A
            /// Doctor notices the fever , they all start requesting low-prio, easy things
            /// Wind is picking up, closer thunders
            /// Duration :: 3 minutes
            /// </summary>
            cough,

            /// <summary>
            /// Act 2B
            /// Seizure marks the midpoint, and they all have a single urgent request.
            /// Physically hard to complete 2, impossible to complete all 3
            /// Their attitudes also change
            /// The thunderstorm brews up and is audible
            /// Duration :: 1 minute
            /// </summary>
            fever,

            /// <summary>
            /// Act 3A
            /// Grandpa's fever rises
            /// Overlapping requests, slightly more complex (ie. 2 ingredients)
            /// Rain starts pouring
            /// Duration :: 5 minutes
            /// </summary>
            delirium,

            /// <summary>
            /// Act 3B
            /// Wind intensifies (windows trembling)
            /// They all have a final 3-ingredient urgent request
            /// Physically impossible to complete more than 1
            /// Ends with thunder striking grandpa breath and silence
            /// Duration :: 1 minute
            /// </summary>
            climax,

            /// <summary>
            /// Closing scene
            /// 
            /// Thunder quiets down, birds fly about, sun starts showing
            /// 
            /// “Thank the Heavens!” exclaims the priest ; 
            /// “What heavens - it was my potion that saved him before pneumonia took over”
            /// 
            /// “I’m just glad you’re okay..” says grandma hugging him, and proceeds to slapping him 
            /// “And don’t you ever go chasing fairies again ! You’re too old for this stuff !”
            /// 
            /// “Now get up and go greet your grandkid, they came a long way to see you” pointing to you
            /// and as the grandpa sighs and the bed creaks by his getting up, the scene fades.
            /// 
            /// Cut to credits
            /// 
            /// Duration :: 1 minute
            /// </summary>
            outro
        }

    }
}