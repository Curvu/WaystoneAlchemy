using ExileCore2.Shared.Attributes;
using ExileCore2.Shared.Interfaces;
using ExileCore2.Shared.Nodes;
using System.Windows.Forms;

namespace WaystoneAlchemy
{
    public class WaystoneAlchemySettings : ISettings
    {
        public ToggleNode Enable { get; set; } = new ToggleNode(false);

        [Menu("Use Regal on Magic Waystones")]
        public ToggleNode UseRegalOnMagicWaystones { get; set; } = new ToggleNode(false);

        [Menu("Enable Distilled Paranoia on Rare Waystones")]
        public ToggleNode EnableParanoiaOnRareWaystones { get; set; } = new ToggleNode(false);

        [Menu("Hotkey for Alchemy Waystone")]
        public HotkeyNode AlchemyHotkey { get; set; } = new HotkeyNode(Keys.F3);

        [Menu("Hotkey for Distilled Paranoia")]
        public HotkeyNode ParanoiaHotkey { get; set; } = new HotkeyNode(Keys.F4);

        [Menu("Emergency Stop Hotkey")]
        public HotkeyNode EmergencyStopHotkey { get; set; } = new HotkeyNode(Keys.Escape);
    }
}