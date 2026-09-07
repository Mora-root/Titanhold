using System;
using Titanhold.Combat;
using Titanhold.Run;
using UnityEditor;
using UnityEngine;

public static class RunCombatResourceValidationRunner
{
    [MenuItem("Tools/Titanhold/Validate Run Combat Resources")]
    public static void Validate()
    {
        try
        {
            ValidateParticipantRoster();
            ValidateParticipantResourceCommands();
            Debug.Log(
                "Run combat resource validation passed (2 scenarios).");
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"Run combat resource validation failed: {exception}");
        }
    }

    private static void ValidateParticipantRoster()
    {
        RunCombatResourceService resources = new(2);
        Assert(resources.TryRegisterParticipant(
                   Identity("player:one", "character:one")).Success &&
               resources.TryRegisterParticipant(
                   Identity("player:two", "character:two")).Success &&
               resources.ParticipantCount == 2,
            "Valid combat-resource participants were not registered.");
        Assert(resources.TryRegisterParticipant(
                   Identity("player:one", "character:three")).Error ==
               RunCombatResourceError.DuplicatePlayer &&
               resources.TryRegisterParticipant(
                   Identity("player:three", "character:one")).Error ==
               RunCombatResourceError.DuplicateCharacter &&
               resources.TryRegisterParticipant(
                   Identity("player:three", "character:three")).Error ==
               RunCombatResourceError.ParticipantLimitExceeded,
            "Combat-resource roster accepted an invalid participant.");
    }

    private static void ValidateParticipantResourceCommands()
    {
        RunCombatResourceService resources = new();
        resources.TryRegisterParticipant(
            Identity("player:one", "character:one"));
        int notifications = 0;
        resources.ResourceChanged += (playerId, snapshot) =>
        {
            notifications++;
            Assert(playerId == "player:one" && snapshot.IsValid,
                "Resource notification lost participant ownership.");
        };

        RunCombatResourceResult registration =
            resources.TryRegisterResource(
                "player:one",
                " resource:rage ",
                8f);
        Assert(registration.Success && registration.Changed &&
               registration.Resource.ResourceId == "resource:rage" &&
               registration.Resource.Current == 0f &&
               notifications == 1,
            "Participant Rage was not registered cleanly.");
        Assert(resources.TryRegisterResource(
                   "player:one",
                   "resource:rage",
                   10f).Error ==
               RunCombatResourceError.DuplicateResource &&
               resources.TryRegisterResource(
                   "player:missing",
                   "resource:rage",
                   8f).Error ==
               RunCombatResourceError.ParticipantNotFound,
            "Invalid resource registration changed the roster.");
        Assert(resources.TryCreateParticipantGateway(
                   "player:one",
                   out ICombatResourceGateway gateway),
            "Registered participant did not expose a resource gateway.");

        CombatExecutionId executionId = CombatExecutionId.New();
        Assert(gateway.TryGain(
                   executionId,
                   "resource:rage",
                   3f) &&
               resources.TryGetResource(
                   "player:one",
                   "resource:rage",
                   out CombatResourceSnapshot rage) &&
               rage.Current == 3f && notifications == 2,
            "Participant gateway did not route Rage generation.");
        Assert(gateway.TryGain(
                   executionId,
                   "resource:rage",
                   3f) &&
               resources.TryGetResource(
                   "player:one",
                   "resource:rage",
                   out rage) &&
               rage.Current == 3f && notifications == 2,
            "Replayed execution duplicated participant Rage.");
        Assert(!gateway.TryGain(
                   CombatExecutionId.New(),
                   "resource:missing",
                   3f) &&
               resources.TrySpend(
                   "player:one",
                   "resource:rage",
                   4f).Error ==
               RunCombatResourceError.InsufficientResource,
            "Rejected participant resource commands mutated state.");
        Assert(resources.TrySetCurrent(
                   "player:one",
                   "resource:rage",
                   8f).Success &&
               resources.TrySpend(
                   "player:one",
                   "resource:rage",
                   5f).Success &&
               resources.TryGetResource(
                   "player:one",
                   "resource:rage",
                   out rage) &&
               rage.Current == 3f && notifications == 4,
            "Participant resource set/spend commands were not applied.");
    }

    private static RunParticipantIdentity Identity(
        string playerId,
        string characterId)
    {
        return new RunParticipantIdentity(playerId, characterId);
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
