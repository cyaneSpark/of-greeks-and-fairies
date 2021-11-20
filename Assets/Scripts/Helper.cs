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
        sideritis = 10,

        // Intro
        chamomile = 20,
        honey = 21,
        eucalyptus = 22,

        // Cough
        oil = 30,
        salt = 31,

        // Fever
        alcohol = 40,
        glass = 41,
        basil = 42,
        apiganos = 43,

        // Delirium
        vitriol = 50,
        soda = 51,
        gunpowder = 52,
        garlic = 53,
        mint = 54,
        sage = 55,

        // Climax
        artemisian = 60,
        turtle = 61,
        hay = 62,
    }
}