using ExileCore2;
using ExileCore2.PoEMemory.MemoryObjects;
using ExileCore2.PoEMemory.Elements.InventoryElements;
using ExileCore2.Shared.Enums;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.Windows.Forms;
using System.Numerics;
using System.Drawing;
using ExileCore2.PoEMemory.Components;
using ExileCore2.PoEMemory.Elements.InventoryElements;


// ..

namespace WaystoneAlchemy
{
    public class WaystoneAlchemyPlugin : BaseSettingsPlugin<WaystoneAlchemySettings>
    {
        private volatile bool _shouldStop = false;
        private const Keys ActivationKey = Keys.F2;
        private bool _previousKeyState;
        private bool _prevParanoiaHotkey;
        private bool _prevParanoiaHotkeyState;
        private bool _previousAlchemyHotkey;
        private bool _prevCorruptHotkey;
        private bool _prevCorruptHotkeyState;


        public override bool Initialise()
        {
            LogMessage("WaystoneAlchemy Plugin Initialized Successfully.", 3);
            return true;
        }


        public override void Tick()
        {
            // Check if the emergency stop hotkey is pressed
            if (EmergencyStopHotkeyPressed())
            {
                // If the emergency stop is activated, exit early
                return;
            }
            
            var openInventory = GameController.IngameState?.IngameUi.InventoryPanel.IsVisible ?? false;
            var inventoryOpen = GameController.IngameState?.IngameUi.InventoryPanel.IsVisible ?? false;
            var currentlyPressed = Input.IsKeyDown(Settings.AlchemyHotkey.Value);
            if (inventoryOpen && currentlyPressed && !_previousKeyState)
            {
                ProcessAlchemyOnWaystones();

                if (Settings.ApplyExaltedOrbsToRareWaystone)
                {
                    ApplyExaltedOrbsToRareWaystone();
                }
                // return;
            }
            _previousKeyState = currentlyPressed;
            // Distilled Paranoia logic explicitly added clearly:
            var paranoiaPressed = Input.IsKeyDown(Settings.ParanoiaHotkey.Value);
            if (openInventory && paranoiaPressed && !_prevParanoiaHotkeyState && Settings.EnableParanoiaOnRareWaystones && !_shouldStop)
            {
                ApplyDistilledParanoiaClearly();
            }
            _prevParanoiaHotkeyState = paranoiaPressed;

            var corruptPressed = Input.IsKeyDown(Settings.CorruptHotkey.Value);
            if (inventoryOpen && corruptPressed && !_previousKeyState && Settings.CorruptRareWaystone)
            {
                CorruptWaystones();
            }

            _prevCorruptHotkeyState = corruptPressed;

            return;
        }


        private bool EmergencyStopHotkeyPressed()
        {
            if (Input.IsKeyDown(Settings.EmergencyStopHotkey.Value))
            {
                DebugWindow.LogMsg("🚫 Emergency Stop activated.", 3, Color.Orange);
                _shouldStop = true;
            }
            return _shouldStop;
        }

        private NormalInventoryItem GetCurrencyItem(string currencyPath)
        {
            var inventoryItems = GameController.IngameState.IngameUi.InventoryPanel[InventoryIndex.PlayerInventory].VisibleInventoryItems;
            return inventoryItems.FirstOrDefault(x => x.Item.Path.Contains(currencyPath) && x.Item.HasComponent<Stack>() && x.Item.GetComponent<Stack>().Size > 0);
        }

        private bool UseCurrencyOnItem(NormalInventoryItem currency, NormalInventoryItem target)
        {
           
            Input.SetCursorPos(currency.GetClientRect().Center);
            Thread.Sleep(100);
            Input.Click(MouseButtons.Right);
            Thread.Sleep(150);

            Input.SetCursorPos(target.GetClientRect().Center);
            Thread.Sleep(100);
            Input.Click(MouseButtons.Left);
            Thread.Sleep(250);
            return true;
        }

        private bool IdentifyItem(NormalInventoryItem item)
        {
            var mods = item.Item.GetComponent<Mods>();
            if (mods == null || mods.Identified) return true; // already identified

            var wisdom = GetCurrencyItem("CurrencyIdentification");
            if (wisdom == null)
            {
                DebugWindow.LogMsg("No Wisdom scroll found!", 2, Color.Red);
                return false;
            }

            return UseCurrencyOnItem(wisdom, item);
        }

        private bool UseAlchemy(NormalInventoryItem item)
        {
            var alch = GetCurrencyItem("CurrencyUpgradeToRare");
            if (alch == null)
            {
                DebugWindow.LogMsg("No Alchemy Orb found!", 2, Color.Red);
                return false;
            }
            return UseCurrencyOnItem(alch, item);
        }

        private bool UseRegal(NormalInventoryItem item)
        {
            var regal = GetCurrencyItem("CurrencyUpgradeMagicToRare");
            if (regal == null)
            {
                DebugWindow.LogMsg("No Regal Orb found!", 2, Color.Red);
                return false;
            }
            return UseCurrencyOnItem(regal, item);
        }

        private void HandleWaystone(NormalInventoryItem waystone)
        {
            var mods = waystone.Item.GetComponent<Mods>();
            if (mods == null) return;

            switch (mods.ItemRarity)
            {
                case ItemRarity.Normal:
                    UseAlchemy(waystone);
                    break;

                case ItemRarity.Magic:
                    if (!Settings.UseRegalOnMagicWaystones) 
                        break;
                    
                    if (!mods.Identified)
                    {
                        if (!IdentifyItem(waystone))
                            return; // failed to identify.
                        Thread.Sleep(250);
                    }

                    UseRegal(waystone);
                    break;

                default:
                    break;
            }
        }



        private void ProcessAlchemyOnWaystones()
        {
            
            var inventoryItems = GameController.IngameState.IngameUi.InventoryPanel[InventoryIndex.PlayerInventory].VisibleInventoryItems;
            var waystones = inventoryItems.Where(x => x.Item.GetComponent<Base>()?.Name.Contains("Waystone") ?? false).ToList();

            if (!waystones.Any())
            {
                DebugWindow.LogMsg("No Waystones found in inventory.", 2, Color.Yellow);
                return;
            }

            foreach (var waystone in waystones)
                HandleWaystone(waystone);
            
        }

        // -------------------------------------------------------------------------------------------------------------------------------------------------------
        // Applying distilled items on waystones...

        private void ApplyDistilledParanoiaClearly()
        {
            
            var inventory = GameController.IngameState.IngameUi.InventoryPanel[InventoryIndex.PlayerInventory];
            var items = inventory.VisibleInventoryItems;

            var paranoia = items.FirstOrDefault(x => x.Item.GetComponent<Base>()?.Name == "Distilled Paranoia" && x.Item.GetComponent<Stack>()?.Size >= 3);
            if (paranoia == null)
            {
                DebugWindow.LogMsg("❌ No sufficient Distilled Paranoia found.", 3, Color.Red);
                return;
            }

            var rareWaystones = items.Where(x =>
                x.Item.GetComponent<Base>()?.Name.Contains("Waystone") == true &&
                x.Item.GetComponent<Mods>()?.ItemRarity == ItemRarity.Rare).ToList();

            foreach (var waystone in rareWaystones)
            {
            

                UseItemRightClick(paranoia); Thread.Sleep(500);
        

                // explicitly transfer 3 Paranoias
                for (int i = 0; i < 3; i++)
                {
                    CtrlClickItem(paranoia); Thread.Sleep(250);
                
                }

                // explicitly add rare Waystone to distilled UI (fix your forgotten step)
                CtrlClickItem(waystone); Thread.Sleep(250);
            

                ClickInstillButton(); Thread.Sleep(1000);


                CtrlClickResultingWaystoneFromDistillUI(); Thread.Sleep(500);
            }
        }

        private void UseItemRightClick(NormalInventoryItem item)
        {
       
            Input.SetCursorPos(item.GetClientRect().Center); Thread.Sleep(100);
            Input.Click(MouseButtons.Right); Thread.Sleep(100);
         
        }

        private void CtrlClickItem(NormalInventoryItem item)
        {

            Input.KeyDown(Keys.ControlKey); Thread.Sleep(60);
            Input.SetCursorPos(item.GetClientRect().Center); Thread.Sleep(100);
            Input.Click(MouseButtons.Left); Thread.Sleep(80);
            Input.KeyUp(Keys.ControlKey); Thread.Sleep(60);
          
        }

        private void ClickInstillButton()
        {

            // CAREFUL: adjust this to your actual Instill button accurately:
            var ui = GameController.IngameState.IngameUi;
            var instillButtonPos = GameController.Window.GetWindowRectangle().TopLeft + new Vector2(628, 842);

            Input.SetCursorPos(instillButtonPos); Thread.Sleep(80);
            Input.Click(MouseButtons.Left); Thread.Sleep(100);
            
            
        }

        private void CtrlClickResultingWaystoneFromDistillUI()
        {

            // CAREFUL: adjust these explicitly to resulting Waystone position within the UI:
            var uiWaystonePos = GameController.Window.GetWindowRectangle().TopLeft + new Vector2(613, 431);

            Input.KeyDown(Keys.ControlKey); Thread.Sleep(100);
            Input.SetCursorPos(uiWaystonePos); Thread.Sleep(80);
            Input.Click(MouseButtons.Left); Thread.Sleep(80);
            Input.KeyUp(Keys.ControlKey); Thread.Sleep(60);
        }


        // Corrupting Waystones

        private void CorruptWaystones()
        {
            var inventoryItems = GameController.IngameState.IngameUi.InventoryPanel[InventoryIndex.PlayerInventory].VisibleInventoryItems;
            var waystones = inventoryItems.Where(x => x.Item.GetComponent<Base>()?.Name.Contains("Waystone") ?? false).ToList();
            if (!waystones.Any())
            {
                DebugWindow.LogMsg("No Waystones found in inventory.", 2, Color.Yellow);
                return;
            }
            var corruptionOrb = GetCurrencyItem("CurrencyCorrupt");
            if (corruptionOrb == null)
            {
                DebugWindow.LogMsg("No Corruption Orb found!", 2, Color.Red);
                return;
            }
            foreach (var waystone in waystones)
            {
                UseCurrencyOnItem(corruptionOrb, waystone);
                Thread.Sleep(250); // Adjust delay as needed
            }
        }

        private void ApplyExaltedOrbsToRareWaystone()
        {
            if (!Settings.ApplyExaltedOrbsToRareWaystone) // Ensure the toggle is enabled
                return;
            var inventoryItems = GameController.IngameState.IngameUi.InventoryPanel[InventoryIndex.PlayerInventory].VisibleInventoryItems;
            var rareWaystones = inventoryItems.Where(x =>
                x.Item.GetComponent<Base>()?.Name.Contains("Waystone") == true &&
                x.Item.GetComponent<Mods>()?.ItemRarity == ItemRarity.Rare).ToList();
            if (!rareWaystones.Any())
            {
                DebugWindow.LogMsg("No rare Waystones found in inventory.", 2, Color.Yellow);
                return;
            }
            var exaltedOrb = GetCurrencyItem("CurrencyAddModToRare");
            if (exaltedOrb == null || exaltedOrb.Item.GetComponent<Stack>().Size < 3)
            {
                DebugWindow.LogMsg("Not enough Exalted Orbs found!", 2, Color.Red);
                return;
            }
            foreach (var waystone in rareWaystones)
            {
                var mods = waystone.Item.GetComponent<Mods>();
                if (mods != null && !mods.Identified) // Check if the Waystone is identified
                {
                    if (!IdentifyItem(waystone)) // Attempt to identify the Waystone
                    {
                        DebugWindow.LogMsg("Failed to identify Waystone.", 2, Color.Red);
                        continue; // Skip this Waystone if identification fails
                    }
                    Thread.Sleep(250); // Add a small delay after identification
                }
                for (int i = 0; i < 3; i++) // Apply 3 Exalted Orbs
                {
                    UseCurrencyOnItem(exaltedOrb, waystone);
                    Thread.Sleep(250); // Adjust delay as needed
                }
            }
        }


    }
}