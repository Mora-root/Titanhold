using System;

namespace Titanhold.UI.Common
{
    public interface IItemSlotEventSource
    {
        event Action<global::ItemSlotRef> ItemSlotRightClicked;
        event Action<IItemDragSourceView, global::ItemSlotRef, ItemDragVisualData> ItemSlotDragStarted;
        event Action<global::ItemSlotRef> ItemSlotDropped;
        event Action ItemSlotDragEnded;
    }
}
