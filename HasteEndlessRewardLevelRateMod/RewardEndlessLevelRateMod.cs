using Landfall.Haste;
using Landfall.Modding;
using MonoMod.Cil;
using UnityEngine.Localization;
using Zorro.Settings;

namespace HasteEndlessRewardLevelRateMod;

[LandfallPlugin]
public class RewardEndlessLevelRateMod
{
    // This class defines a setting that will show up on the in-game Setting menu.
    // Landfall has made this easy for us! We can simply inherit from Zorro.Settings.IExposedSetting and
    // implement any necessary methods.
    // We also inherit from IntSetting, since we are creating a setting that stores an integer (a whole number).
    //
    // This "[HasteSetting]" thing is an annotation Landfall gives us to quickly register setting.
    // The alternative would be to call `GameHandler.Instance.SettingsHandler.AddSetting(new OurSetting());`
    // on a hook to `GameHandler.Awake` or something similar to that.
    [HasteSetting]
    public class EndlessRewardEveryLevelSetting : IntSetting, IExposedSetting
    {
        // These should be self-explanatory.
        protected override int GetDefaultValue() => 5;
        public LocalizedString GetDisplayName() => new UnlocalizedString("Endless: Give rewards every X levels");
        public string GetCategory() => SettingCategory.Difficulty;
        public override void ApplyValue() { /* Not needed for our use-case. Do nothing. */ }
    }

    // This method will run when the mod loads
    static RewardEndlessLevelRateMod()
    {
        /* 
         * Setup an IL hook to modify the IL code of RunHandler.TransitionOnLevelComplete.
         * As the name implies, this function manages the transition when a level completes (both when in
         * a shard and in Endless).
         * 
         * The logic we'll be overwriting here is in the following statement:
         * 
         *   if (
         *     RunHandler.RunData.runConfig.isEndless &&
         *     (RunHandler.RunData.currentLevel == 0 || (RunHandler.RunData.currentLevel + 1) % 5 == 0)
         *   ) {
         *     // ... transition to Endless Award scene ...
         *   }
         * 
         * More specifically, the condition `(RunHandler.RunDate.currentLevel + 1) % 5 == 0`, which returns
         * true only when the current level number is divisible by 5.
         * 
         * Note that the 5 is a constant in the code. It cannot be changed by overwriting a variable, for example.
         * Instead, we have to modify the IL code directly:
         */
        IL.RunHandler.TransitionOnLevelCompleted += (il) =>
        {
            // Create a cursor. This will be used to edit the IL code programatically.
            // You can think of this ILCursor as the cursor that shows up when you're editing text:
            // - The cursor always points to a specific character (IL instruction)
            // - The cursor can be used to edit that character (get, update, add, remove, plus some other helpers)
            // To start, the IL cursor will be set on the very first instruction of the method.
            var cursor = new ILCursor(il);

            // Put the cursor on the instruction that loads the number 5 into memory
            // The exact instruction here was found using a tool like dnSpyEx or ILSpy. You can try finding this
            // instruction by using one of those tools in a mode that shows both the IL code and the C# code
            // simultaneously.
            //
            // Loading up the game's Assembly-CSharp.dll, find the `RunHandler.TransitionOnLevelCompleted` method,
            // and look for if-statement described above.
            // You will also see that this is the only `ldc.i4 5` instruction in the entire method, so it is quite
            // safe to just find the first instance of that instruction and assume it is the one we want to change.
            cursor.TryGotoNext((i) => i.MatchLdcI4(5)); 

            // Remove the instruction
            cursor.Remove();

            // And in its place, put the following code:
            cursor.EmitDelegate(() =>
            {
                // Gets a reference to the setting we created.
                var setting = GameHandler.Instance.SettingsHandler.GetSetting<EndlessRewardEveryLevelSetting>();

                // And returns its current value.
                return setting.Value;
            });

            // And thus, we effectively replace that constant "5" with whatever value the player has put in settings!

            /*
             * To learn more about ILHooks and how to use ILCursor, check out some of these resources:
             * - The official ILCursor docs, listing all methods available: https://monomod.dev/api/MonoMod.Cil.ILCursor.html#methods
             * - Hamunii's guide: https://lethal.wiki/dev/fundamentals/patching-code/monomod-examples#ilhook-examples
             */
        };
    }
}
