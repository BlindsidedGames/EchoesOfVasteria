        public class DiscipleGenerationRecord
        {
            public Dictionary<string, double> StoredResources = new();
            public Dictionary<string, double> TotalCollected = new();
            public float Progress;
            public double LastGenerationTime;
        }

        [HideReferenceObjectPicker]
        public class QuestRecord
        {
            public bool Completed;
            public Dictionary<string, double> KillProgress; // enemy name -> count; may be null until used
            public double DistanceTravelProgress;
            public int BuffCastBaseline;
            public bool BuffCastBaselineSet;
            public Dictionary<string, int> BuffCastProgress; // BuffRecipe.name -> count; may be null until used
            public int CriticalBaseline;
            public bool CriticalBaselineSet;
            public double ResourcesBaseline;
            public bool ResourcesBaselineSet;
            public int TasksBaseline;
            public bool TasksBaselineSet;
            public double CauldronMixProgress;
            public long CompletedTimestamp;
        }

        [HideReferenceObjectPicker]
        public class RunRecord
        {
            public int RunNumber;
            public string MapType;
            public float Duration;
            public float Distance;
            public int TasksCompleted;
            public double ResourcesCollected;
            public double BonusResourcesCollected;
            public int EnemiesKilled;
            public double DamageDealtDouble; // migrated primary
            public double DamageTakenDouble;  // migrated primary
            [Obsolete("Use DamageDealtDouble")] public float DamageDealt; // legacy
            [Obsolete("Use DamageTakenDouble")] public float DamageTaken; // legacy
            public bool Died;
            public bool Reaped;
            public bool Abandoned;

            // Effective read-only accessors for migrated values with legacy fallback
            public double DamageDealtAsDouble
            {
                get
                {
                    if (DamageDealtDouble != 0) return DamageDealtDouble;
#pragma warning disable 618
                    return DamageDealt != 0 ? DamageDealt : 0;
#pragma warning restore 618
                }
            }

            public double DamageTakenAsDouble
            {
                get
                {
                    if (DamageTakenDouble != 0) return DamageTakenDouble;
#pragma warning disable 618
                    return DamageTaken != 0 ? DamageTaken : 0;
#pragma warning restore 618
                }
            }
        }

        [HideReferenceObjectPicker]
        public class GeneralStats
        {
            public double DistanceTravelledDouble; // migrated primary
            [Obsolete("Use DistanceTravelledDouble")] public float DistanceTravelled; // legacy
            public float HighestDistance;
            public int TotalKills;
            public int SlimesKilled;
            public int TasksCompleted;
            public int Deaths;
            public double DamageDealtDouble; // migrated primary
            public double DamageTakenDouble;  // migrated primary
            [Obsolete("Use DamageDealtDouble")] public float DamageDealt; // legacy
            [Obsolete("Use DamageTakenDouble")] public float DamageTaken; // legacy
            public int TimesReaped;
            public int BuffsCast;
            public int CriticalHits;
            public double TotalResourcesGathered;

            // Records for the most recent runs. Limited to the last 50.
            public List<RunRecord> RecentRuns = new();
            public float LongestRun;
            public float ShortestRun;
            public float AverageRun;
            public float MaxRunDistance = 50f;
            public int NextRunNumber = 1;

            // Effective read-only accessors for migrated values with legacy fallback
            public double DistanceTravelledAsDouble
            {
                get
                {
                    if (DistanceTravelledDouble != 0) return DistanceTravelledDouble;
#pragma warning disable 618
                    return DistanceTravelled != 0 ? DistanceTravelled : 0;
#pragma warning restore 618
                }
            }

            public double DamageDealtAsDouble
            {
                get
                {
                    if (DamageDealtDouble != 0) return DamageDealtDouble;
#pragma warning disable 618
                    return DamageDealt != 0 ? DamageDealt : 0;
#pragma warning restore 618
                }
            }

            public double DamageTakenAsDouble
            {
                get
                {
                    if (DamageTakenDouble != 0) return DamageTakenDouble;
#pragma warning disable 618
                    return DamageTaken != 0 ? DamageTaken : 0;
#pragma warning restore 618
                }
            }
        }

        [HideReferenceObjectPicker]
        public class MapStatistics
        {
            public double StepsDouble;       // migrated primary
            public double LongestTrekDouble; // migrated primary
            [Obsolete("Use StepsDouble")] public float Steps;              // legacy
            [Obsolete("Use LongestTrekDouble")] public float LongestTrek;        // legacy
            public int TasksCompleted;
            public double ResourcesGathered;
            public int Kills;
            public double DamageDealtDouble; // migrated primary
            public int Deaths;
            public double DamageTakenDouble; // migrated primary
            [Obsolete("Use DamageDealtDouble")] public float DamageDealt;        // legacy
            [Obsolete("Use DamageTakenDouble")] public float DamageTaken;        // legacy

            // Effective read-only accessors for migrated values with legacy fallback
            public double StepsAsDouble
            {
                get
                {
                    if (StepsDouble != 0) return StepsDouble;
#pragma warning disable 618
                    return Steps != 0 ? Steps : 0;
#pragma warning restore 618
                }
            }

            public double LongestTrekAsDouble
            {
                get
                {
                    if (LongestTrekDouble != 0) return LongestTrekDouble;
#pragma warning disable 618
                    return LongestTrek != 0 ? LongestTrek : 0;
#pragma warning restore 618
                }
            }

            public double DamageDealtAsDouble
            {
                get
                {
                    if (DamageDealtDouble != 0) return DamageDealtDouble;
#pragma warning disable 618
                    return DamageDealt != 0 ? DamageDealt : 0;
#pragma warning restore 618
                }
            }

            public double DamageTakenAsDouble
            {
                get
                {
                    if (DamageTakenDouble != 0) return DamageTakenDouble;
#pragma warning disable 618
                    return DamageTaken != 0 ? DamageTaken : 0;
#pragma warning restore 618
                }
            }
        }


        [HideReferenceObjectPicker]
        public class ForgeStats
        {
            // Top-level totals
            public int TotalCrafts;
            public int TotalEquippedFromCraft;
            public int TotalSalvaged;
            public int TotalAutocraftSessions;
            public int TotalCraftUntilUpgradeSessions;
            public int TotalFailedCraftAttempts;

            // Resource usage and returns
            public Dictionary<string, double> ResourcesSpent = new(); // ingots/cores/chunks/crystals spends
            public Dictionary<string, double> ResourcesGainedFromSalvage = new();
            public Dictionary<string, double> CoresSpentByCore = new(); // coreName -> cores spent
            public Dictionary<string, double> IngotsSpentByCore = new(); // coreName -> ingots spent

            // Distributions (what was crafted)
            public Dictionary<string, int> CraftsByCore = new();
            public Dictionary<string, int> CraftsBySlot = new();
            public Dictionary<string, int> CraftsByRarity = new();
            public Dictionary<string, Dictionary<string, int>> RarityCountsByCore = new(); // core -> rarity -> count
            public Dictionary<string, Dictionary<string, int>> SlotCountsByCore = new(); // core -> slot -> count
            public Dictionary<int, int> AffixCountDistribution = new();

            // Upgrade outcomes
            public Dictionary<string, int> UpgradesBySlot = new();
            public Dictionary<string, int> UpgradesByRarity = new();
            public int CraftsSinceLastUpgrade;
            public int MaxCraftsBetweenUpgrades;
            public int TotalUpgradeEvents;
            public int CumulativeCraftsBetweenUpgrades;
            public float AverageCraftsPerUpgrade; // derived but cached for convenience
            public Dictionary<string, FloatAgg> UpgradeScoreDeltaBySlot = new(); // slot -> {sum,count}

            // Affix/stat roll quality
            public Dictionary<string, StatAgg> StatRolls = new(); // statId -> agg
            public Dictionary<string, Dictionary<string, StatAgg>> StatRollsByRarity = new(); // rarity -> statId -> agg
            public Dictionary<string, Dictionary<string, StatAgg>> StatRollsBySlot = new(); // slot -> statId -> agg
            public Dictionary<string, int> HighRollsByStat = new(); // statId -> count above threshold
            public float HighRollTopPercentThreshold = 0.9f; // default top 10%

            public Dictionary<string, double>
                CumulativeStatTotalsByStat = new(); // statId -> running sum across all crafts

            public Dictionary<string, float> HighestRollByStat = new(); // statId -> highest single affix roll value

            // Ivan progression (forge mastery)
            public int IvanLevelAtCraft;
            public float IvanXpAtCraft;
            public double IvanXpGainedTotal;
            public int IvanLevelUpsFromCrafts;
            public Dictionary<string, double> IvanXpByCore = new();
            public Dictionary<string, double> IvanXpByRarity = new();

            // Autocraft specifics
            public int AutocraftCrafts;

            public Dictionary<string, int>
                AutocraftStopReasons = new(); // {Upgraded,OutOfResources,Cancelled,MaxIterations}

            public Dictionary<string, int> AutocraftBestRarityTierBySlot = new(); // slot -> highest rarity tier index

            // Salvage specifics
            public Dictionary<string, int> SalvagesByRarity = new();
            public Dictionary<string, int> SalvagesByCore = new();
            public int SalvageItems; // number of items salvaged
            public int SalvageEntries; // total individual entries awarded across all salvages
            public Dictionary<string, ResourceAgg> SalvageYieldPerResource = new(); // resName -> {sum,count}

            // Conversion actions (forge side-panels)
            public int IngotConversions; // actions performed
            public int CoreConversions; // actions performed
            public double CrystalCrafted; // total units produced (legacy total)
            public double ChunksCrafted; // total units produced (legacy total)
            public Dictionary<string, double> ConversionSpentByResource = new();
            public Dictionary<string, double> CrystalsCraftedByResource = new();
            public Dictionary<string, double> ChunksCraftedByResource = new();
            public Dictionary<string, double> IngotsCraftedByResource = new();
            public Dictionary<string, double> CoresCraftedByResource = new();

            // Best single-piece scores
            public Dictionary<string, float> BestPieceScoreBySlot = new(); // slot -> highest score (absolute)
            public Dictionary<string, float> BestPieceScoreByCore = new(); // coreName -> highest score (absolute)
            public Dictionary<string, float> MinPieceScoreByCore = new(); // coreName -> min observed piece score
            public Dictionary<string, float> MaxPieceScoreByCore = new(); // coreName -> max observed piece score
            public Dictionary<string, float> BestPieceScoreByRarity = new(); // rarityName -> highest score

            // Best absolute piece score by slot (independent of currently equipped item)
            public Dictionary<string, float> BestAbsolutePieceScoreBySlot = new(); // slot -> highest absolute score

            // Best absolute piece scores by grouping
            public Dictionary<string, float> BestAbsolutePieceScoreByCore = new(); // coreName -> highest absolute score
            public Dictionary<string, float> BestAbsolutePieceScoreByRarity = new(); // rarityName -> highest absolute score

            // Slot mapping for the best absolute scores above
            public Dictionary<string, string> BestAbsolutePieceSlotByCore = new(); // coreName -> slot name of best
            public Dictionary<string, string> BestAbsolutePieceSlotByRarity = new(); // rarityName -> slot name of best

            // Per-slot totals
            public Dictionary<string, int> EquipsBySlot = new();
            public Dictionary<string, int> SalvagesBySlot = new();
            public Dictionary<string, int> CraftsBySlotTotals = new();

            [HideReferenceObjectPicker]
            public class StatAgg
