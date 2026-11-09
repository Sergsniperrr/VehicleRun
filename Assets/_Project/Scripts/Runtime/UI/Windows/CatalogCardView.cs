using System;
using _Project.Core.Vehicles;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.UI.Windows
{
    public class CatalogCardView : MonoBehaviour
    {
        private static readonly Color LockedBadgeColor = new Color(0.45f, 0.45f, 0.45f);

        [SerializeField] private Image _icon;
        [SerializeField] private Image _rankBadge;
        [SerializeField] private TMP_Text _rankValue;
        [SerializeField] private TMP_Text _caption;
        [SerializeField] private TMP_Text _income;

        public int Rank { get; private set; }

        public void ShowVehicle(VehicleDefinition definition, string vehicleName, string income)
        {
            ApplyRank(definition);
            _icon.color = Color.white;
            _rankBadge.color = definition.BadgeColor;
            _caption.text = vehicleName;
            _income.gameObject.SetActive(true);
            _income.text = income;
        }

        public void ShowSilhouette(VehicleDefinition definition, string condition)
        {
            ApplyRank(definition);
            _icon.color = Color.black;
            _rankBadge.color = LockedBadgeColor;
            _caption.text = condition;
            _income.gameObject.SetActive(false);
        }

        private void ApplyRank(VehicleDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            Rank = definition.Rank;
            _icon.sprite = definition.GarageSprite;
            _rankValue.text = definition.Rank.ToString();
        }
    }
}
