using System;
using System.Collections.Generic;
using Titanhold.Progression;
using Titanhold.Run;

namespace Titanhold.Session
{
    public sealed class GameSessionRuntime
    {
        private readonly Dictionary<string, CharacterSnapshot>
            characterSnapshots = new(StringComparer.Ordinal);
        private readonly RunExperienceCurve runExperienceCurve;
        private RunResultSummary settledRunResult;
        private readonly int maximumParticipantCount;
        private readonly int runAbilitySlotCount;
        private string activeRunStateSessionId = string.Empty;

        public GameSessionRuntime(
            IItemDefinitionResolver itemDefinitions,
            RunConclusionRewardPolicy conclusionRewards,
            int maximumParticipantCount =
                GameSessionService.DefaultMaximumParticipantCount,
            RunExperienceCurve runExperienceCurve = null,
            int runAbilitySlotCount =
                RunAbilityLoadoutService.DefaultAbilitySlotCount,
            IRunStartingAbilityPoolResolver startingAbilityPools = null,
            IRunCombatResourceLoadoutResolver combatResourceLoadouts = null,
            IRunAbilityUnlockScheduleResolver abilityUnlockSchedules = null,
            IRunUpgradeDefinitionResolver runUpgrades = null,
            IRunUpgradeUnlockScheduleResolver upgradeUnlockSchedules = null)
        {
            ItemDefinitions = itemDefinitions ??
                throw new ArgumentNullException(nameof(itemDefinitions));
            ConclusionRewards = conclusionRewards ??
                throw new ArgumentNullException(nameof(conclusionRewards));
            if (runAbilitySlotCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(runAbilitySlotCount));

            this.maximumParticipantCount = maximumParticipantCount;
            this.runAbilitySlotCount = runAbilitySlotCount;
            this.runExperienceCurve = runExperienceCurve ??
                new RunExperienceCurve(Array.Empty<int>());
            GameSession = new GameSessionService(maximumParticipantCount);
            CharacterSnapshots = new CharacterSnapshotService();
            AccountCrystals = new AccountCrystalWallet();
            StartingAbilityPools = startingAbilityPools;
            CombatResourceLoadouts = combatResourceLoadouts;
            AbilityUnlockSchedules = abilityUnlockSchedules;
            RunUpgrades = runUpgrades;
            UpgradeUnlockSchedules = upgradeUnlockSchedules;
            if ((RunUpgrades == null) != (UpgradeUnlockSchedules == null))
            {
                throw new ArgumentException(
                    "Run upgrade definitions and schedules must be configured together.");
            }

            if (RunUpgrades != null && AbilityUnlockSchedules == null)
            {
                throw new ArgumentException(
                    "Coordinated run-level rewards require ability schedules.");
            }

            GameSession.StateChanged += HandleGameSessionStateChanged;
        }

        public GameSessionService GameSession { get; }
        public CharacterSnapshotService CharacterSnapshots { get; }
        public AccountCrystalWallet AccountCrystals { get; }
        public RunConclusionRewardPolicy ConclusionRewards { get; }
        public IItemDefinitionResolver ItemDefinitions { get; }
        public IRunStartingAbilityPoolResolver StartingAbilityPools { get; }
        public IRunCombatResourceLoadoutResolver CombatResourceLoadouts { get; }
        public IRunAbilityUnlockScheduleResolver AbilityUnlockSchedules { get; }
        public IRunUpgradeDefinitionResolver RunUpgrades { get; }
        public IRunUpgradeUnlockScheduleResolver UpgradeUnlockSchedules
        {
            get;
        }
        public int StoredCharacterCount => characterSnapshots.Count;
        public RunProgressionService ActiveRunProgression { get; private set; }
        public RunCombatResourceService ActiveRunCombatResources
        {
            get;
            private set;
        }
        public RunAbilityLoadoutService ActiveRunAbilityLoadout { get; private set; }
        public RunAbilityChoiceService ActiveRunAbilityChoices { get; private set; }
        public RunStartReadinessService ActiveRunStartReadiness { get; private set; }
        public RunStartingAbilitySelectionService
            ActiveRunStartingAbilitySelection { get; private set; }
        public RunLevelAbilitySelectionService
            ActiveRunLevelAbilitySelection { get; private set; }
        public RunUpgradeChoiceService ActiveRunUpgradeChoices
        {
            get;
            private set;
        }
        public RunLevelUpgradeSelectionService
            ActiveRunLevelUpgradeSelection { get; private set; }
        public RunLevelRewardSelectionService
            ActiveRunLevelRewardSelection { get; private set; }
        public RunUpgradeStatApplicationService
            ActiveRunUpgradeStatApplication { get; private set; }

        public event Action<string, CharacterSnapshot> CharacterSnapshotChanged;
        public event Action<string, RunProgressionService>
            ActiveRunProgressionChanged;
        public event Action<string, RunCombatResourceService>
            ActiveRunCombatResourcesChanged;
        public event Action<string, RunAbilityLoadoutService>
            ActiveRunAbilityLoadoutChanged;
        public event Action<string, RunAbilityChoiceService>
            ActiveRunAbilityChoicesChanged;
        public event Action<string, RunStartReadinessService>
            ActiveRunStartReadinessChanged;
        public event Action<string, RunStartingAbilitySelectionService>
            ActiveRunStartingAbilitySelectionChanged;
        public event Action<string, RunLevelAbilitySelectionService>
            ActiveRunLevelAbilitySelectionChanged;
        public event Action<string, RunUpgradeChoiceService>
            ActiveRunUpgradeChoicesChanged;
        public event Action<string, RunLevelUpgradeSelectionService>
            ActiveRunLevelUpgradeSelectionChanged;
        public event Action<string, RunLevelRewardSelectionService>
            ActiveRunLevelRewardSelectionChanged;
        public event Action<string, RunUpgradeStatApplicationService>
            ActiveRunUpgradeStatApplicationChanged;

        public bool TryGetActiveRunProgression(
            string runSessionId,
            out RunProgressionService progression)
        {
            string normalizedId = runSessionId?.Trim() ?? string.Empty;
            if (normalizedId.Length == 0 ||
                ActiveRunProgression == null ||
                !string.Equals(
                    normalizedId,
                    activeRunStateSessionId,
                    StringComparison.Ordinal))
            {
                progression = null;
                return false;
            }

            progression = ActiveRunProgression;
            return true;
        }

        public bool TryGetActiveRunCombatResources(
            string runSessionId,
            out RunCombatResourceService resources)
        {
            string normalizedId = runSessionId?.Trim() ?? string.Empty;
            if (normalizedId.Length == 0 ||
                ActiveRunCombatResources == null ||
                !string.Equals(
                    normalizedId,
                    activeRunStateSessionId,
                    StringComparison.Ordinal))
            {
                resources = null;
                return false;
            }

            resources = ActiveRunCombatResources;
            return true;
        }

        public bool TryGetActiveRunAbilityLoadout(
            string runSessionId,
            out RunAbilityLoadoutService loadout)
        {
            string normalizedId = runSessionId?.Trim() ?? string.Empty;
            if (normalizedId.Length == 0 ||
                ActiveRunAbilityLoadout == null ||
                !string.Equals(
                    normalizedId,
                    activeRunStateSessionId,
                    StringComparison.Ordinal))
            {
                loadout = null;
                return false;
            }

            loadout = ActiveRunAbilityLoadout;
            return true;
        }

        public bool TryGetActiveRunAbilityChoices(
            string runSessionId,
            out RunAbilityChoiceService choices)
        {
            string normalizedId = runSessionId?.Trim() ?? string.Empty;
            if (normalizedId.Length == 0 ||
                ActiveRunAbilityChoices == null ||
                !string.Equals(
                    normalizedId,
                    activeRunStateSessionId,
                    StringComparison.Ordinal))
            {
                choices = null;
                return false;
            }

            choices = ActiveRunAbilityChoices;
            return true;
        }

        public bool TryGetActiveRunStartReadiness(
            string runSessionId,
            out RunStartReadinessService readiness)
        {
            string normalizedId = runSessionId?.Trim() ?? string.Empty;
            if (normalizedId.Length == 0 ||
                ActiveRunStartReadiness == null ||
                !string.Equals(
                    normalizedId,
                    activeRunStateSessionId,
                    StringComparison.Ordinal))
            {
                readiness = null;
                return false;
            }

            readiness = ActiveRunStartReadiness;
            return true;
        }

        public bool TryGetActiveRunStartingAbilitySelection(
            string runSessionId,
            out RunStartingAbilitySelectionService selection)
        {
            string normalizedId = runSessionId?.Trim() ?? string.Empty;
            if (normalizedId.Length == 0 ||
                ActiveRunStartingAbilitySelection == null ||
                !string.Equals(
                    normalizedId,
                    activeRunStateSessionId,
                    StringComparison.Ordinal))
            {
                selection = null;
                return false;
            }

            selection = ActiveRunStartingAbilitySelection;
            return true;
        }

        public bool TryGetActiveRunLevelAbilitySelection(
            string runSessionId,
            out RunLevelAbilitySelectionService selection)
        {
            string normalizedId = runSessionId?.Trim() ?? string.Empty;
            if (normalizedId.Length == 0 ||
                ActiveRunLevelAbilitySelection == null ||
                !string.Equals(
                    normalizedId,
                    activeRunStateSessionId,
                    StringComparison.Ordinal))
            {
                selection = null;
                return false;
            }

            selection = ActiveRunLevelAbilitySelection;
            return true;
        }

        public bool TryGetActiveRunUpgradeChoices(
            string runSessionId,
            out RunUpgradeChoiceService choices)
        {
            string normalizedId = runSessionId?.Trim() ?? string.Empty;
            if (normalizedId.Length == 0 ||
                ActiveRunUpgradeChoices == null ||
                !string.Equals(
                    normalizedId,
                    activeRunStateSessionId,
                    StringComparison.Ordinal))
            {
                choices = null;
                return false;
            }

            choices = ActiveRunUpgradeChoices;
            return true;
        }

        public bool TryGetActiveRunLevelUpgradeSelection(
            string runSessionId,
            out RunLevelUpgradeSelectionService selection)
        {
            string normalizedId = runSessionId?.Trim() ?? string.Empty;
            if (normalizedId.Length == 0 ||
                ActiveRunLevelUpgradeSelection == null ||
                !string.Equals(
                    normalizedId,
                    activeRunStateSessionId,
                    StringComparison.Ordinal))
            {
                selection = null;
                return false;
            }

            selection = ActiveRunLevelUpgradeSelection;
            return true;
        }

        public bool TryGetActiveRunLevelRewardSelection(
            string runSessionId,
            out RunLevelRewardSelectionService selection)
        {
            string normalizedId = runSessionId?.Trim() ?? string.Empty;
            if (normalizedId.Length == 0 ||
                ActiveRunLevelRewardSelection == null ||
                !string.Equals(
                    normalizedId,
                    activeRunStateSessionId,
                    StringComparison.Ordinal))
            {
                selection = null;
                return false;
            }

            selection = ActiveRunLevelRewardSelection;
            return true;
        }

        public bool TryGetActiveRunUpgradeStatApplication(
            string runSessionId,
            out RunUpgradeStatApplicationService application)
        {
            string normalizedId = runSessionId?.Trim() ?? string.Empty;
            if (normalizedId.Length == 0 ||
                ActiveRunUpgradeStatApplication == null ||
                !string.Equals(
                    normalizedId,
                    activeRunStateSessionId,
                    StringComparison.Ordinal))
            {
                application = null;
                return false;
            }

            application = ActiveRunUpgradeStatApplication;
            return true;
        }

        public bool TryGetCharacterSnapshot(
            string characterId,
            out CharacterSnapshot snapshot)
        {
            string normalizedId = characterId?.Trim() ?? string.Empty;
            if (normalizedId.Length == 0)
            {
                snapshot = null;
                return false;
            }

            return characterSnapshots.TryGetValue(normalizedId, out snapshot);
        }

        public CharacterSnapshotCaptureResult TryCaptureCharacter(
            string characterId,
            PlayerInventory inventory,
            PlayerEquipmentRuntime equipment,
            PlayerExperience experience,
            PlayerGold gold)
        {
            CharacterSnapshotCaptureResult result = CharacterSnapshots.TryCapture(
                characterId,
                inventory,
                equipment,
                experience,
                gold);
            if (!result.Success)
                return result;

            string normalizedId = result.Snapshot.CharacterId;
            characterSnapshots[normalizedId] = result.Snapshot;
            CharacterSnapshotChanged?.Invoke(normalizedId, result.Snapshot);
            return result;
        }

        public CharacterSnapshotRestoreResult TryRestoreCharacter(
            string characterId,
            PlayerInventory inventory,
            PlayerEquipmentRuntime equipment,
            PlayerExperience experience,
            PlayerGold gold)
        {
            string normalizedId = characterId?.Trim() ?? string.Empty;
            if (normalizedId.Length == 0)
            {
                return CharacterSnapshotRestoreResult.Failed(
                    CharacterSnapshotError.MissingCharacterId);
            }

            if (!characterSnapshots.TryGetValue(
                    normalizedId,
                    out CharacterSnapshot snapshot))
            {
                return CharacterSnapshotRestoreResult.Failed(
                    CharacterSnapshotError.SnapshotNotFound,
                    $"No snapshot is stored for character '{normalizedId}'.");
            }

            return CharacterSnapshots.TryRestore(
                snapshot,
                ItemDefinitions,
                inventory,
                equipment,
                experience,
                gold);
        }

        internal bool TryStoreCharacterSnapshots(
            IReadOnlyList<CharacterSnapshot> snapshots,
            out string error)
        {
            error = string.Empty;
            if (snapshots == null || snapshots.Count == 0)
            {
                error = "No character snapshots were supplied.";
                return false;
            }

            HashSet<string> characterIds = new(StringComparer.Ordinal);
            for (int i = 0; i < snapshots.Count; i++)
            {
                CharacterSnapshot snapshot = snapshots[i];
                if (snapshot == null ||
                    string.IsNullOrWhiteSpace(snapshot.CharacterId) ||
                    snapshot.SchemaVersion != CharacterSnapshot.CurrentSchemaVersion)
                {
                    error = $"Character snapshot {i} is invalid.";
                    return false;
                }

                if (!characterIds.Add(snapshot.CharacterId))
                {
                    error =
                        $"Character '{snapshot.CharacterId}' occurs more than once.";
                    return false;
                }
            }

            for (int i = 0; i < snapshots.Count; i++)
            {
                CharacterSnapshot snapshot = snapshots[i];
                characterSnapshots[snapshot.CharacterId] = snapshot;
            }

            for (int i = 0; i < snapshots.Count; i++)
            {
                CharacterSnapshot snapshot = snapshots[i];
                CharacterSnapshotChanged?.Invoke(
                    snapshot.CharacterId,
                    snapshot);
            }

            return true;
        }

        internal bool TryGetSettledRunResult(
            string runSessionId,
            out RunResultSummary result)
        {
            string normalizedId = runSessionId?.Trim() ?? string.Empty;
            result = settledRunResult;
            return result != null &&
                   string.Equals(
                       result.RunSessionId,
                       normalizedId,
                       StringComparison.Ordinal);
        }

        internal bool TryRecordSettledRunResult(RunResultSummary result)
        {
            if (result == null || !result.IsValid ||
                GameSession.State.ActiveRun == null ||
                !string.Equals(
                    result.RunSessionId,
                    GameSession.State.ActiveRun.RunSessionId,
                    StringComparison.Ordinal))
            {
                return false;
            }

            if (settledRunResult != null)
            {
                return string.Equals(
                           settledRunResult.RunSessionId,
                           result.RunSessionId,
                           StringComparison.Ordinal) &&
                       settledRunResult.Outcome == result.Outcome &&
                       settledRunResult.CompletedRoundCount ==
                           result.CompletedRoundCount &&
                       settledRunResult.CharacterExperienceAwarded ==
                           result.CharacterExperienceAwarded &&
                       settledRunResult.CrystalsAwarded ==
                           result.CrystalsAwarded;
            }

            settledRunResult = result;
            return true;
        }

        private void HandleGameSessionStateChanged(GameSessionState state)
        {
            if (state == null)
                return;

            if (state.Phase == GameSessionPhase.TransitionToRun)
            {
                settledRunResult = null;
                CreateRunState(state.ActiveRun);
                return;
            }

            if (state.Phase == GameSessionPhase.Hub)
                ClearRunState();
        }

        private void CreateRunState(RunSessionDescriptor descriptor)
        {
            if (descriptor == null ||
                string.IsNullOrWhiteSpace(descriptor.RunSessionId))
            {
                ClearRunState();
                return;
            }

            RunProgressionService progression = new(
                runExperienceCurve,
                maximumParticipantCount);
            RunCombatResourceService combatResources = new(
                maximumParticipantCount);
            RunAbilityLoadoutService abilityLoadout = new(
                runAbilitySlotCount,
                maximumParticipantCount);
            RunParticipantIdentity[] participantRoster =
                new RunParticipantIdentity[descriptor.Participants.Count];
            for (int i = 0; i < descriptor.Participants.Count; i++)
            {
                RunParticipantSelection participant =
                    descriptor.Participants[i];
                RunParticipantIdentity identity = new(
                    participant.PlayerId,
                    participant.CharacterId);
                participantRoster[i] = identity;
                RunProgressionResult registration =
                    progression.TryRegisterParticipant(identity);
                if (!registration.Success)
                {
                    throw new InvalidOperationException(
                        "Validated run participant could not be registered " +
                        $"for progression: {registration.Error}.");
                }

                RunCombatResourceResult resourceRegistration =
                    combatResources.TryRegisterParticipant(identity);
                if (!resourceRegistration.Success)
                {
                    throw new InvalidOperationException(
                        "Validated run participant could not be registered " +
                        $"for combat resources: {resourceRegistration.Error}.");
                }

                RegisterParticipantCombatResources(
                    combatResources,
                    participant);

                RunAbilityLoadoutResult abilityRegistration =
                    abilityLoadout.TryRegisterParticipant(identity);
                if (!abilityRegistration.Success)
                {
                    throw new InvalidOperationException(
                        "Validated run participant could not be registered " +
                        $"for ability loadout: {abilityRegistration.Error}.");
                }

                if (participant.StartingAbilityId.Length > 0)
                {
                    RunAbilityLoadoutResult startingAbility =
                        abilityLoadout.TryGrantAndAssignAbility(
                            participant.PlayerId,
                            participant.StartingAbilityId,
                            0);
                    if (!startingAbility.Success)
                    {
                        throw new InvalidOperationException(
                            $"Starting ability '{participant.StartingAbilityId}' " +
                            $"could not be assigned: {startingAbility.Error}.");
                    }
                }
            }

            RunStartReadinessService startReadiness = new(
                abilityLoadout,
                participantRoster);
            for (int i = 0; i < descriptor.Participants.Count; i++)
            {
                RunParticipantSelection participant =
                    descriptor.Participants[i];
                if (participant.StartingAbilityId.Length == 0)
                    continue;

                RunStartReadinessResult confirmation =
                    startReadiness.TryConfirmStartingAbility(
                        participant.PlayerId,
                        participant.StartingAbilityId);
                if (!confirmation.Success)
                {
                    startReadiness.Dispose();
                    throw new InvalidOperationException(
                        "Starting ability readiness could not be confirmed: " +
                        $"{confirmation.Error}.");
                }
            }

            if (startReadiness.AllParticipantsConfirmed &&
                !startReadiness.TrySeal().Success)
            {
                startReadiness.Dispose();
                throw new InvalidOperationException(
                    "Complete starting ability readiness could not be sealed.");
            }

            RunAbilityChoiceService abilityChoices = new(abilityLoadout);
            RunStartingAbilitySelectionService startingAbilitySelection = new(
                abilityChoices,
                startReadiness);
            for (int i = 0; i < descriptor.Participants.Count; i++)
            {
                RunParticipantSelection participant =
                    descriptor.Participants[i];
                if (participant.StartingAbilityId.Length > 0 ||
                    participant.CharacterArchetypeId.Length == 0)
                {
                    continue;
                }

                RunStartingAbilitySelectionResult offer =
                    startingAbilitySelection.TryOfferStartingChoice(
                        participant.PlayerId,
                        RunStartingAbilitySelectionService.StartingChoiceId,
                        participant.CharacterArchetypeId,
                        StartingAbilityPools,
                        CreateStartingChoiceSeed(descriptor.Seed, i));
                if (!offer.Success)
                {
                    startReadiness.Dispose();
                    throw new InvalidOperationException(
                        $"Starting ability choice for participant " +
                        $"'{participant.PlayerId}' could not be offered: " +
                        $"{offer.Error}.");
                }
            }

            bool coordinatedRunLevelRewards = RunUpgrades != null;
            RunLevelAbilitySelectionService levelAbilitySelection = null;
            RunAbilityUnlockParticipantPlan[] abilityPlans = null;
            if (AbilityUnlockSchedules != null)
            {
                abilityPlans =
                    new RunAbilityUnlockParticipantPlan[
                        descriptor.Participants.Count];
                for (int i = 0; i < descriptor.Participants.Count; i++)
                {
                    RunParticipantSelection participant =
                        descriptor.Participants[i];
                    if (!AbilityUnlockSchedules.TryResolve(
                            participant.CharacterArchetypeId,
                            out RunAbilityUnlockSchedule schedule))
                    {
                        startReadiness.Dispose();
                        throw new InvalidOperationException(
                            "No ability unlock schedule is configured for " +
                            $"character archetype '{participant.CharacterArchetypeId}'.");
                    }

                    for (int milestoneIndex = 0;
                         milestoneIndex < schedule.Milestones.Count;
                         milestoneIndex++)
                    {
                        if (schedule.Milestones[milestoneIndex]
                                .TargetSlotIndex >= runAbilitySlotCount)
                        {
                            startReadiness.Dispose();
                            throw new InvalidOperationException(
                                $"Ability unlock schedule '{schedule.ScheduleId}' " +
                                "targets a slot outside the active run loadout.");
                        }
                    }

                    abilityPlans[i] = new RunAbilityUnlockParticipantPlan(
                        participant.PlayerId,
                        i,
                        schedule);
                }

                levelAbilitySelection = new RunLevelAbilitySelectionService(
                    progression,
                    abilityChoices,
                    abilityPlans,
                    descriptor.Seed,
                    observeChanges: !coordinatedRunLevelRewards);
            }

            RunUpgradeChoiceService upgradeChoices = null;
            RunLevelUpgradeSelectionService levelUpgradeSelection = null;
            RunLevelRewardSelectionService levelRewardSelection = null;
            RunUpgradeStatApplicationService upgradeStatApplication = null;
            if (coordinatedRunLevelRewards)
            {
                upgradeChoices = new RunUpgradeChoiceService(
                    RunUpgrades,
                    maximumParticipantCount);
                RunUpgradeUnlockParticipantPlan[] upgradePlans =
                    new RunUpgradeUnlockParticipantPlan[
                        descriptor.Participants.Count];
                string[] playerIds =
                    new string[descriptor.Participants.Count];
                for (int i = 0; i < descriptor.Participants.Count; i++)
                {
                    RunParticipantSelection participant =
                        descriptor.Participants[i];
                    RunParticipantIdentity identity = participantRoster[i];
                    RunUpgradeChoiceResult registration =
                        upgradeChoices.TryRegisterParticipant(identity);
                    if (!registration.Success)
                    {
                        startReadiness.Dispose();
                        levelAbilitySelection?.Dispose();
                        throw new InvalidOperationException(
                            "Validated run participant could not be registered " +
                            $"for upgrades: {registration.Error}.");
                    }

                    if (!UpgradeUnlockSchedules.TryResolve(
                            participant.CharacterArchetypeId,
                            out RunUpgradeUnlockSchedule schedule))
                    {
                        startReadiness.Dispose();
                        levelAbilitySelection?.Dispose();
                        throw new InvalidOperationException(
                            "No upgrade unlock schedule is configured for " +
                            $"character archetype '{participant.CharacterArchetypeId}'.");
                    }

                    if (HasConflictingRewardLevels(
                            abilityPlans[i].Schedule,
                            schedule))
                    {
                        startReadiness.Dispose();
                        levelAbilitySelection?.Dispose();
                        throw new InvalidOperationException(
                            $"Character archetype '{participant.CharacterArchetypeId}' has ability and upgrade rewards at the same run level.");
                    }

                    upgradePlans[i] = new RunUpgradeUnlockParticipantPlan(
                        participant.PlayerId,
                        i,
                        schedule);
                    playerIds[i] = participant.PlayerId;
                }

                levelUpgradeSelection =
                    new RunLevelUpgradeSelectionService(
                        progression,
                        upgradeChoices,
                        upgradePlans,
                        descriptor.Seed,
                        observeChanges: false);
                levelRewardSelection = new RunLevelRewardSelectionService(
                    progression,
                    abilityChoices,
                    upgradeChoices,
                    levelAbilitySelection,
                    levelUpgradeSelection,
                    playerIds);
                upgradeStatApplication =
                    new RunUpgradeStatApplicationService(
                        upgradeChoices,
                        RunUpgrades);
            }

            activeRunStateSessionId = descriptor.RunSessionId;
            ActiveRunProgression = progression;
            ActiveRunCombatResources = combatResources;
            ActiveRunAbilityLoadout = abilityLoadout;
            ActiveRunAbilityChoices = abilityChoices;
            ActiveRunStartReadiness = startReadiness;
            ActiveRunStartingAbilitySelection = startingAbilitySelection;
            ActiveRunLevelAbilitySelection = levelAbilitySelection;
            ActiveRunUpgradeChoices = upgradeChoices;
            ActiveRunLevelUpgradeSelection = levelUpgradeSelection;
            ActiveRunLevelRewardSelection = levelRewardSelection;
            ActiveRunUpgradeStatApplication = upgradeStatApplication;
            ActiveRunProgressionChanged?.Invoke(
                activeRunStateSessionId,
                ActiveRunProgression);
            ActiveRunCombatResourcesChanged?.Invoke(
                activeRunStateSessionId,
                ActiveRunCombatResources);
            ActiveRunAbilityLoadoutChanged?.Invoke(
                activeRunStateSessionId,
                ActiveRunAbilityLoadout);
            ActiveRunAbilityChoicesChanged?.Invoke(
                activeRunStateSessionId,
                ActiveRunAbilityChoices);
            ActiveRunStartReadinessChanged?.Invoke(
                activeRunStateSessionId,
                ActiveRunStartReadiness);
            ActiveRunStartingAbilitySelectionChanged?.Invoke(
                activeRunStateSessionId,
                ActiveRunStartingAbilitySelection);
            if (ActiveRunLevelAbilitySelection != null)
            {
                ActiveRunLevelAbilitySelectionChanged?.Invoke(
                    activeRunStateSessionId,
                    ActiveRunLevelAbilitySelection);
            }

            if (ActiveRunUpgradeChoices != null)
            {
                ActiveRunUpgradeChoicesChanged?.Invoke(
                    activeRunStateSessionId,
                    ActiveRunUpgradeChoices);
                ActiveRunLevelUpgradeSelectionChanged?.Invoke(
                    activeRunStateSessionId,
                    ActiveRunLevelUpgradeSelection);
                ActiveRunLevelRewardSelectionChanged?.Invoke(
                    activeRunStateSessionId,
                    ActiveRunLevelRewardSelection);
                ActiveRunUpgradeStatApplicationChanged?.Invoke(
                    activeRunStateSessionId,
                    ActiveRunUpgradeStatApplication);
            }
        }

        private static bool HasConflictingRewardLevels(
            RunAbilityUnlockSchedule abilities,
            RunUpgradeUnlockSchedule upgrades)
        {
            for (int abilityIndex = 0;
                 abilityIndex < abilities.Milestones.Count;
                 abilityIndex++)
            {
                int abilityLevel =
                    abilities.Milestones[abilityIndex].UnlockLevel;
                for (int upgradeIndex = 0;
                     upgradeIndex < upgrades.Milestones.Count;
                     upgradeIndex++)
                {
                    if (upgrades.Milestones[upgradeIndex].UnlockLevel ==
                        abilityLevel)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void RegisterParticipantCombatResources(
            RunCombatResourceService combatResources,
            RunParticipantSelection participant)
        {
            if (CombatResourceLoadouts == null)
                return;

            if (!CombatResourceLoadouts.TryResolve(
                    participant.CharacterArchetypeId,
                    out RunCombatResourceLoadout loadout))
            {
                throw new InvalidOperationException(
                    $"No combat resource loadout is configured for character " +
                    $"archetype '{participant.CharacterArchetypeId}'.");
            }

            for (int i = 0; i < loadout.Resources.Count; i++)
            {
                RunCombatResourceDefinition resource = loadout.Resources[i];
                RunCombatResourceResult registration =
                    combatResources.TryRegisterResource(
                        participant.PlayerId,
                        resource.ResourceId,
                        resource.Maximum,
                        resource.Initial);
                if (!registration.Success)
                {
                    throw new InvalidOperationException(
                        $"Combat resource '{resource.ResourceId}' from " +
                        $"loadout '{loadout.LoadoutId}' could not be " +
                        $"registered for participant " +
                        $"'{participant.PlayerId}': {registration.Error}.");
                }
            }
        }

        private static int CreateStartingChoiceSeed(
            int runSeed,
            int participantIndex)
        {
            return unchecked((runSeed * 397) ^ participantIndex);
        }

        private void ClearRunState()
        {
            if (ActiveRunProgression == null &&
                ActiveRunCombatResources == null &&
                ActiveRunAbilityLoadout == null &&
                ActiveRunAbilityChoices == null &&
                ActiveRunStartReadiness == null &&
                ActiveRunStartingAbilitySelection == null &&
                ActiveRunLevelAbilitySelection == null &&
                ActiveRunUpgradeChoices == null &&
                ActiveRunLevelUpgradeSelection == null &&
                ActiveRunLevelRewardSelection == null &&
                ActiveRunUpgradeStatApplication == null)
                return;

            string clearedRunSessionId = activeRunStateSessionId;
            activeRunStateSessionId = string.Empty;
            bool hadProgression = ActiveRunProgression != null;
            bool hadCombatResources = ActiveRunCombatResources != null;
            bool hadAbilityLoadout = ActiveRunAbilityLoadout != null;
            bool hadAbilityChoices = ActiveRunAbilityChoices != null;
            bool hadStartReadiness = ActiveRunStartReadiness != null;
            bool hadStartingSelection =
                ActiveRunStartingAbilitySelection != null;
            bool hadLevelAbilitySelection =
                ActiveRunLevelAbilitySelection != null;
            bool hadUpgradeChoices = ActiveRunUpgradeChoices != null;
            bool hadLevelUpgradeSelection =
                ActiveRunLevelUpgradeSelection != null;
            bool hadLevelRewardSelection =
                ActiveRunLevelRewardSelection != null;
            bool hadUpgradeStatApplication =
                ActiveRunUpgradeStatApplication != null;
            ActiveRunLevelRewardSelection?.Dispose();
            ActiveRunUpgradeStatApplication?.Dispose();
            ActiveRunLevelUpgradeSelection?.Dispose();
            ActiveRunLevelAbilitySelection?.Dispose();
            ActiveRunStartReadiness?.Dispose();
            ActiveRunProgression = null;
            ActiveRunCombatResources = null;
            ActiveRunAbilityLoadout = null;
            ActiveRunAbilityChoices = null;
            ActiveRunStartReadiness = null;
            ActiveRunStartingAbilitySelection = null;
            ActiveRunLevelAbilitySelection = null;
            ActiveRunUpgradeChoices = null;
            ActiveRunLevelUpgradeSelection = null;
            ActiveRunLevelRewardSelection = null;
            ActiveRunUpgradeStatApplication = null;
            if (hadProgression)
            {
                ActiveRunProgressionChanged?.Invoke(
                    clearedRunSessionId,
                    null);
            }

            if (hadCombatResources)
            {
                ActiveRunCombatResourcesChanged?.Invoke(
                    clearedRunSessionId,
                    null);
            }

            if (hadAbilityLoadout)
            {
                ActiveRunAbilityLoadoutChanged?.Invoke(
                    clearedRunSessionId,
                    null);
            }

            if (hadAbilityChoices)
            {
                ActiveRunAbilityChoicesChanged?.Invoke(
                    clearedRunSessionId,
                    null);
            }

            if (hadStartReadiness)
            {
                ActiveRunStartReadinessChanged?.Invoke(
                    clearedRunSessionId,
                    null);
            }

            if (hadStartingSelection)
            {
                ActiveRunStartingAbilitySelectionChanged?.Invoke(
                    clearedRunSessionId,
                    null);
            }

            if (hadLevelAbilitySelection)
            {
                ActiveRunLevelAbilitySelectionChanged?.Invoke(
                    clearedRunSessionId,
                    null);
            }

            if (hadUpgradeChoices)
            {
                ActiveRunUpgradeChoicesChanged?.Invoke(
                    clearedRunSessionId,
                    null);
            }

            if (hadLevelUpgradeSelection)
            {
                ActiveRunLevelUpgradeSelectionChanged?.Invoke(
                    clearedRunSessionId,
                    null);
            }

            if (hadLevelRewardSelection)
            {
                ActiveRunLevelRewardSelectionChanged?.Invoke(
                    clearedRunSessionId,
                    null);
            }

            if (hadUpgradeStatApplication)
            {
                ActiveRunUpgradeStatApplicationChanged?.Invoke(
                    clearedRunSessionId,
                    null);
            }
        }
    }
}
