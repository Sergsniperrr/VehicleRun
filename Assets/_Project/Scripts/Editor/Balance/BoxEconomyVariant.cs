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
        private const string BoxRankLagProperty = "_boxRankLag";
        private const string FreeBoxIntervalProperty = "_freeBoxIntervalSeconds";
        private const string BasePriceProperty = "_boxBasePriceIncomeSeconds";
        private const string GrowthProperty = "_boxPriceGrowth";
        private const string FirstRankGrowthProperty = "_firstRankBoxPriceGrowth";
        private const string IsGrowthLimitedProperty = "_isBoxPriceGrowthLimited";
        private const string GrowthPurchaseLimitProperty = "_boxPriceGrowthPurchaseLimit";

        [SerializeField] private string _name;
        [SerializeField] private int _boxRankLag;
        [SerializeField] private float _freeBoxIntervalSeconds;
        [SerializeField] private double _boxBasePriceIncomeSeconds;
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
            serializedEconomy.FindProperty(BoxRankLagProperty).intValue = _boxRankLag;
            serializedEconomy.FindProperty(FreeBoxIntervalProperty).floatValue = _freeBoxIntervalSeconds;
            serializedEconomy.FindProperty(BasePriceProperty).doubleValue = _boxBasePriceIncomeSeconds;
            serializedEconomy.FindProperty(GrowthProperty).doubleValue = _boxPriceGrowth;
            serializedEconomy.FindProperty(FirstRankGrowthProperty).doubleValue = _firstRankBoxPriceGrowth;
            serializedEconomy.FindProperty(IsGrowthLimitedProperty).boolValue = _isBoxPriceGrowthLimited;
            serializedEconomy.FindProperty(GrowthPurchaseLimitProperty).intValue = _boxPriceGrowthPurchaseLimit;
            serializedEconomy.ApplyModifiedPropertiesWithoutUndo();

            return economy;
        }
    }
}
