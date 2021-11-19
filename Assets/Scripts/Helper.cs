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
        garlic_head = 10, garlic_clove = 11, garlic_slice = 12,
        oil_cup = 20,
        tsipouro_cup = 30,
    }
}