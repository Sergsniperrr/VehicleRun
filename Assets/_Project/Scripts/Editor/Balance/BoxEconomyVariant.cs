using System;
using _Project.Core.Economy;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace _Project.Editor.Balance
{
    [Serializable]
    public class BoxEconomyVariant
    {
        private const string FreeBoxIntervalProperty = "_freeBoxIntervalSeconds";
        private const string BasePricesProperty = "_boxBasePrices";
        private const long MinBasePrice = 1;
        private const string GrowthProperty = "_boxPriceGrowth";
        private const string FirstRankGrowthProperty = "_firstRankBoxPriceGrowth";
        private const string IsGrowthLimitedProperty = "_isBoxPriceGrowthLimited";
        private const string GrowthPurchaseLimitProperty = "_boxPriceGrowthPurchaseLimit";

        [SerializeField] private string _name;
        [SerializeField] private float _freeBoxIntervalSeconds;
        [SerializeField] private double _boxBasePriceScale;
        [SerializeField] private double _boxPriceGrowth;
        [SerializeField] private double _firstRankBoxPriceGrowth;
        [SerializeField] private bool _isBoxPriceGrowthLimited;
        [SerializeField] private int _boxPriceGrowthPurchaseLimit;

        public string Name => _name;

        public EconomyConfig CreateEconomy(EconomyConfig source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            EconomyConfig economy = Object.Instantiate(source);
            SerializedObject serializedEconomy = new SerializedObject(economy);
            serializedEconomy.FindProperty(FreeBoxIntervalProperty).floatValue = _freeBoxIntervalSeconds;
            ScaleBasePrices(serializedEconomy.FindProperty(BasePricesProperty));
            serializedEconomy.FindProperty(GrowthProperty).doubleValue = _boxPriceGrowth;
            serializedEconomy.FindProperty(FirstRankGrowthProperty).doubleValue = _firstRankBoxPriceGrowth;
            serializedEconomy.FindProperty(IsGrowthLimitedProperty).boolValue = _isBoxPriceGrowthLimited;
            serializedEconomy.FindProperty(GrowthPurchaseLimitProperty).intValue = _boxPriceGrowthPurchaseLimit;
            serializedEconomy.ApplyModifiedPropertiesWithoutUndo();

            return economy;
        }

        private void ScaleBasePrices(SerializedProperty basePrices)
        {
            for (int i = 0; i < basePrices.arraySize; i++)
            {
                SerializedProperty basePrice = basePrices.GetArrayElementAtIndex(i);
                double scaledPrice = Math.Round(basePrice.longValue * _boxBasePriceScale);
                basePrice.longValue = Math.Max(MinBasePrice, (long)scaledPrice);
            }
        }
    }
}
