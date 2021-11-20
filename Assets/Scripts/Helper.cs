using UnityEngine;

namespace Fairies
{
    public static class Helper
    {
        private static int GetItemType(this Item item) => Mathf.FloorToInt((int)item / 10f);
        public static bool IsSameTypeWith(this Item itemA, Item itemB) => itemA.GetItemType() == itemB.GetItemType();
    }

    public enum Actor
    {
        grandma,
        doctor,
        priest,
    }

    /// <summary>
    /// One item Type (ie. ingredient) takes up an entire decimal
    /// </summary>
    public enum Item
    {
        // Tutorial
        sideritis_clove = 100,

        // Intro
        chamomile_clove = 200,
        honey_jar = 210,
        eucalyptus_branch = 220,

        // Cough
        oil_cup = 300,
        salt_hand = 310, salt_pinch = 311,

        // Fever
        alcohol_cup = 400,
        glass_jar = 410,
        basil_clove = 420,
        apiganos_clove = 430,

        // Delirium
        vitriol_chunk = 500,
        soda_chunk = 510,
        gunpowder_hand = 520,
        garlic_clove = 530,
        mint_clove = 540,
        sage_clove = 550,

        // Climax
        artemisian_flower = 600,
        bone_turtle = 610,
        hay_clove = 620,
    }
}