using System.Collections.Generic;

namespace Titanhold.UI.Common
{
    public sealed class ItemTooltipBlock
    {
        public ItemTooltipBlock(string title, IEnumerable<string> lines)
        {
            Title = title;
            Lines = lines != null ? new List<string>(lines) : new List<string>();
        }

        public string Title { get; }
        public IReadOnlyList<string> Lines { get; }
    }

    public sealed class ItemTooltipData
    {
        private readonly List<ItemTooltipBlock> blocks = new();

        public string Title { get; set; }
        public string Subtitle { get; set; }
        public string Description { get; set; }
        public string Footer { get; set; }
        public string SellPriceText { get; set; }
        public string StackText { get; set; }
        public IReadOnlyList<ItemTooltipBlock> Blocks => blocks;

        public void AddBlock(string title, IEnumerable<string> lines)
        {
            ItemTooltipBlock block = new ItemTooltipBlock(title, lines);
            if (string.IsNullOrWhiteSpace(block.Title) && block.Lines.Count == 0)
                return;

            blocks.Add(block);
        }
    }
}
