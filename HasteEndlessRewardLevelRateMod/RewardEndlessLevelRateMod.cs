using Landfall.Haste;
using Landfall.Modding;
using MonoMod.Cil;
using UnityEngine.Localization;
using Zorro.Settings;

namespace HasteEndlessRewardLevelRateMod;

[LandfallPlugin]
public class RewardEndlessLevelRateMod
{
    // This class defines a setting that will show up in the in-game Setting menu.
    // Landfall has made this easy for us. We can simply inherit from Zorro.Settings.IExposedSetting
    // and implement any necessary methods.
    // We also inherit from IntSetting, since this is a setting that stores an int (whole number).
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
        // Register the setting so it shows up in-game.
        GameHandler.Instance.SettingsHandler.AddSetting(new EndlessRewardEveryLevelSetting());

        /* 
         * Setup an IL hook to modify the IL code of RunHandler.TransitionOnLevelComplete. As the name
         * implies, this function does the transition when a level completes (both when in a shard and
         * in Endless).
         * 
         * The logic we'll be targetting here is the following statement:
         * 
         * if (
         *   RunHandler.RunData.runConfig.isEndless &&
         *   (RunHandler.RunData.currentLevel == 0 || (RunHandler.RunData.currentLevel + 1) % 5 == 0)
         * ) {
         *   // ... transition to Endless Award scene ...
         * }
         * 
         * More specifically, the condition `(RunHandler.RunDate.currentLevel + 1) % 5 == 0`, which
         * passes only when the current level* is divisible by 5.
         * 
         * *: the game does currentLevel + 1 because levels numbers internally start at 0.
         * 
         * Note that the 5 is a constant in the code. We cannot change it by simply changing a variable
         * something like that.
         * Instead, we have to modify the IL code directly:
         */
        IL.RunHandler.TransitionOnLevelCompleted += (il) =>
        {
            // Create a cursor that will be used to edit the IL code programatically.
            // This is similar to the text cursor for when you're editing text!
            var cursor = new ILCursor(il);

            // Put the cursor to the instruction that loads the number 5 into memory
            // Note that the exact instruction here was found using dnSpyEx (ILSpy also
            // works) in IL with C# mode.
            cursor.TryGotoNext((i) => i.MatchLdcI4(5)); 

            // Remove the instruction
            cursor.Remove();

            // And in its place, put the following code.
            // This code...
            cursor.EmitDelegate(() =>
            {
                // Gets a reference to the setting we created.
                var setting = GameHandler.Instance.SettingsHandler.GetSetting<EndlessRewardEveryLevelSetting>();

                // And returns its current value.
                return setting.Value;
            });

            // And thus, we effectively replace that constant "5" with whatever value the player has put in settings!

            /* To learn more about ILHooks and how to use ILCursor, check out some of these resources:
             * - The official ILCursor docs, listing all methods available: https://monomod.dev/api/MonoMod.Cil.ILCursor.html#methods
             * - Hamunii's guide: https://lethal.wiki/dev/fundamentals/patching-code/monomod-examples#ilhook-examples
             */
        };
    }
}
