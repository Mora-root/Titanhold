using System;
using UnityEngine;

namespace Titanhold.Session
{
    [Serializable]
    public sealed class RunSceneParticipantBinding
    {
        [SerializeField] private string playerId;
        [SerializeField] private string characterId;
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private PlayerEquipmentRuntime equipment;
        [SerializeField] private PlayerExperience experience;
        [SerializeField] private PlayerGold gold;

        public RunSceneParticipantBinding(
            string playerId,
            string characterId,
            PlayerInventory inventory,
            PlayerEquipmentRuntime equipment,
            PlayerExperience experience,
            PlayerGold gold)
        {
            this.playerId = playerId?.Trim() ?? string.Empty;
            this.characterId = characterId?.Trim() ?? string.Empty;
            this.inventory = inventory;
            this.equipment = equipment;
            this.experience = experience;
            this.gold = gold;
        }

        public string PlayerId => playerId;
        public string CharacterId => characterId;
        public PlayerInventory Inventory => inventory;
        public PlayerEquipmentRuntime Equipment => equipment;
        public PlayerExperience Experience => experience;
        public PlayerGold Gold => gold;
        public bool IsValid =>
            !string.IsNullOrWhiteSpace(playerId) &&
            !string.IsNullOrWhiteSpace(characterId) &&
            inventory != null &&
            equipment != null &&
            experience != null &&
            gold != null &&
            equipment.gameObject == inventory.gameObject &&
            experience.gameObject == inventory.gameObject &&
            gold.gameObject == inventory.gameObject;

        public bool Matches(RunParticipantSelection participant)
        {
            return string.Equals(
                       playerId,
                       participant.PlayerId,
                       StringComparison.Ordinal) &&
                   string.Equals(
                       characterId,
                       participant.CharacterId,
                       StringComparison.Ordinal);
        }
    }
}
