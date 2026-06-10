using UnityEngine;

namespace Titanhold.UI.Common
{
    public sealed class ItemInteractionContext : MonoBehaviour
    {
        public ItemInteractionMode Mode { get; private set; } = ItemInteractionMode.None;
        public global::IItemContainerOwner ActiveContainer { get; private set; }
        public bool HasActiveContainer => Mode != ItemInteractionMode.None && ActiveContainer != null;

        public void SetContainerMode(ItemInteractionMode mode, global::IItemContainerOwner container)
        {
            Mode = container != null ? mode : ItemInteractionMode.None;
            ActiveContainer = container;
        }

        public void Clear()
        {
            Mode = ItemInteractionMode.None;
            ActiveContainer = null;
        }

        public void ClearIfContainer(global::IItemContainerOwner container)
        {
            if (container == null || !ReferenceEquals(ActiveContainer, container))
                return;

            Clear();
        }
    }
}
