using System;
using System.Collections.Generic;
using _Project.Core.Configs;
using _Project.Core.Economy;
using _Project.Core.Garage;
using _Project.Core.Localization;
using _Project.Core.Track;
using _Project.Core.Turbo;
using _Project.Core.Vehicles;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace _Project.Tests.EditMode.Configs
{
    public class GameConfigFixture : IDisposable
    {
        private static readonly long[] CoinsPerLap = { 4, 8, 17, 37, 77, 163, 346, 733, 1554, 3301, 7020, 14946 };
        private static readonly long[] BoxBasePrices = { 80, 1200, 5700, 16000, 43000, 110000, 290000, 770000, 1900000, 5000000, 14000000, 33000000 };
        private static readonly float[] TrackSpeeds = { 3.965f, 4.361f, 4.515f, 4.564f, 4.825f, 5.014f, 5.197f, 5.397f, 5.6f, 5.8f, 6f, 6.2f };
        private static readonly (int Rank, int Capacity)[] GarageSteps = { (3, 9), (5, 12), (7, 15) };
        private static readonly (int Rank, int Capacity)[] TrackSteps = { (4, 4), (8, 6), (12, 8) };

        private const int Version = 1;
        private const long StartCoins = 60;
        private const int StartVehicleRank = 1;
        private const int StartVehicleCount = 2;
        private const int BoxRankLag = 5;
        private const double BoxPriceGrowth = 1.175;
        private const double FirstRankBoxPriceGrowth = 1.07;
        private const bool IsBoxPriceGrowthLimited = false;
        private const int BoxPriceGrowthPurchaseLimit = 20;
        private const float FreeBoxIntervalSeconds = 20f;
        private const float OfflineMinAwaySeconds = 60f;
        private const float OfflineMaxAwaySeconds = 14400f;
        private const double OfflineIncomeRate = 0.5;
        private const int GarageColumns = 3;
        private const int GarageRows = 5;
        private const int GarageInitialOpenSlots = 6;
        private const float StraightHalfLength = 3f;
        private const float TurnRadius = 4.4f;
        private const float StartDistance = 5.59f;
        private const int TrackInitialCapacity = 2;
        private const int TurboUnlockRank = 3;
        private const float TurboSpeedMultiplier = 2f;
        private const float TurboFreeDurationSeconds = 30f;
        private const float TurboFreeCooldownSeconds = 180f;
        private const float TurboTapBoostSeconds = 2f;
        private const float SpritePixelsPerUnit = 100f;

        private readonly List<Object> _createdObjects = new List<Object>();

        public GameConfigFixture()
        {
            Localization = Create<LocalizationConfig>();
            Vehicles = Create<VehicleCatalogConfig>();
            Economy = Create<EconomyConfig>();
            Garage = Create<GarageConfig>();
            Track = Create<TrackConfig>();
            Turbo = Create<TurboConfig>();
            Config = Create<GameConfig>();

            FillLocalization();
            FillVehicles();
            FillEconomy();
            FillGarage();
            FillTrack();
            FillTurbo();
            FillConfig();
        }

        public LocalizationConfig Localization { get; }
        public VehicleCatalogConfig Vehicles { get; }
        public EconomyConfig Economy { get; }
        public GarageConfig Garage { get; }
        public TrackConfig Track { get; }
        public TurboConfig Turbo { get; }
        public GameConfig Config { get; }

        public void Change(Object target, string propertyPath, Action<SerializedProperty> change)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyPath);

            if (property == null)
            {
                throw new ArgumentException(nameof(propertyPath));
            }

            change(property);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        public void Dispose()
        {
            foreach (Object createdObject in _createdObjects)
            {
                Object.DestroyImmediate(createdObject);
            }

            _createdObjects.Clear();
        }

        private T Create<T>() where T : ScriptableObject
        {
            T instance = ScriptableObject.CreateInstance<T>();
            _createdObjects.Add(instance);

            return instance;
        }

        private Sprite CreateSprite()
        {
            Texture2D texture = Texture2D.whiteTexture;
            Rect rect = new Rect(0, 0, texture.width, texture.height);
            Sprite sprite = Sprite.Create(texture, rect, Vector2.zero, SpritePixelsPerUnit);
            _createdObjects.Add(sprite);

            return sprite;
        }

        private string GetNameKey(int index)
        {
            return $"vehicle.test_{index}.name";
        }

        private void FillLocalization()
        {
            SerializedObject localization = new SerializedObject(Localization);
            SerializedProperty entries = localization.FindProperty("_entries");
            entries.arraySize = CoinsPerLap.Length;

            for (int i = 0; i < CoinsPerLap.Length; i++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("_key").stringValue = GetNameKey(i);
                entry.FindPropertyRelative("_russian").stringValue = $"Машина {i}";
                entry.FindPropertyRelative("_english").stringValue = $"Vehicle {i}";
            }

            localization.ApplyModifiedPropertiesWithoutUndo();
        }

        private void FillVehicles()
        {
            SerializedObject catalog = new SerializedObject(Vehicles);
            SerializedProperty vehicles = catalog.FindProperty("_vehicles");
            vehicles.arraySize = CoinsPerLap.Length;

            for (int i = 0; i < CoinsPerLap.Length; i++)
            {
                SerializedProperty vehicle = vehicles.GetArrayElementAtIndex(i);
                vehicle.FindPropertyRelative("_id").stringValue = $"test_{i}";
                vehicle.FindPropertyRelative("_rank").intValue = i + 1;
                vehicle.FindPropertyRelative("_nameKey").stringValue = GetNameKey(i);
                vehicle.FindPropertyRelative("_coinsPerLap").longValue = CoinsPerLap[i];
                vehicle.FindPropertyRelative("_trackSpeed").floatValue = TrackSpeeds[i];
                vehicle.FindPropertyRelative("_garageSprite").objectReferenceValue = CreateSprite();
                vehicle.FindPropertyRelative("_trackSprite").objectReferenceValue = CreateSprite();
            }

            catalog.ApplyModifiedPropertiesWithoutUndo();
        }

        private void FillEconomy()
        {
            SerializedObject economy = new SerializedObject(Economy);
            economy.FindProperty("_startCoins").longValue = StartCoins;
            economy.FindProperty("_startVehicleRank").intValue = StartVehicleRank;
            economy.FindProperty("_startVehicleCount").intValue = StartVehicleCount;
            economy.FindProperty("_boxRankLag").intValue = BoxRankLag;
            FillBoxBasePrices(economy.FindProperty("_boxBasePrices"));
            economy.FindProperty("_boxPriceGrowth").doubleValue = BoxPriceGrowth;
            economy.FindProperty("_firstRankBoxPriceGrowth").doubleValue = FirstRankBoxPriceGrowth;
            economy.FindProperty("_isBoxPriceGrowthLimited").boolValue = IsBoxPriceGrowthLimited;
            economy.FindProperty("_boxPriceGrowthPurchaseLimit").intValue = BoxPriceGrowthPurchaseLimit;
            economy.FindProperty("_freeBoxIntervalSeconds").floatValue = FreeBoxIntervalSeconds;
            economy.FindProperty("_offlineMinAwaySeconds").floatValue = OfflineMinAwaySeconds;
            economy.FindProperty("_offlineMaxAwaySeconds").floatValue = OfflineMaxAwaySeconds;
            economy.FindProperty("_offlineIncomeRate").doubleValue = OfflineIncomeRate;
            economy.ApplyModifiedPropertiesWithoutUndo();
        }

        private void FillBoxBasePrices(SerializedProperty pricesProperty)
        {
            pricesProperty.arraySize = BoxBasePrices.Length;

            for (int i = 0; i < BoxBasePrices.Length; i++)
            {
                pricesProperty.GetArrayElementAtIndex(i).longValue = BoxBasePrices[i];
            }
        }

        private void FillGarage()
        {
            SerializedObject garage = new SerializedObject(Garage);
            garage.FindProperty("_columns").intValue = GarageColumns;
            garage.FindProperty("_rows").intValue = GarageRows;
            FillSchedule(garage.FindProperty("_openSlots"), GarageInitialOpenSlots, GarageSteps);
            garage.ApplyModifiedPropertiesWithoutUndo();
        }

        private void FillTrack()
        {
            SerializedObject track = new SerializedObject(Track);
            track.FindProperty("_straightHalfLength").floatValue = StraightHalfLength;
            track.FindProperty("_turnRadius").floatValue = TurnRadius;
            track.FindProperty("_startDistance").floatValue = StartDistance;
            FillSchedule(track.FindProperty("_capacity"), TrackInitialCapacity, TrackSteps);
            track.ApplyModifiedPropertiesWithoutUndo();
        }

        private void FillTurbo()
        {
            SerializedObject turbo = new SerializedObject(Turbo);
            turbo.FindProperty("_unlockRank").intValue = TurboUnlockRank;
            turbo.FindProperty("_speedMultiplier").floatValue = TurboSpeedMultiplier;
            turbo.FindProperty("_freeDurationSeconds").floatValue = TurboFreeDurationSeconds;
            turbo.FindProperty("_freeCooldownSeconds").floatValue = TurboFreeCooldownSeconds;
            turbo.FindProperty("_tapBoostSeconds").floatValue = TurboTapBoostSeconds;
            turbo.ApplyModifiedPropertiesWithoutUndo();
        }

        private void FillSchedule(SerializedProperty schedule, int initial, (int Rank, int Capacity)[] steps)
        {
            schedule.FindPropertyRelative("_initial").intValue = initial;

            SerializedProperty stepsProperty = schedule.FindPropertyRelative("_steps");
            stepsProperty.arraySize = steps.Length;

            for (int i = 0; i < steps.Length; i++)
            {
                SerializedProperty step = stepsProperty.GetArrayElementAtIndex(i);
                step.FindPropertyRelative("_rank").intValue = steps[i].Rank;
                step.FindPropertyRelative("_capacity").intValue = steps[i].Capacity;
            }
        }

        private void FillConfig()
        {
            SerializedObject config = new SerializedObject(Config);
            config.FindProperty("_version").intValue = Version;
            config.FindProperty("_vehicles").objectReferenceValue = Vehicles;
            config.FindProperty("_economy").objectReferenceValue = Economy;
            config.FindProperty("_garage").objectReferenceValue = Garage;
            config.FindProperty("_track").objectReferenceValue = Track;
            config.FindProperty("_turbo").objectReferenceValue = Turbo;
            config.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
