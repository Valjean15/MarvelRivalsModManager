using MarvelRivalManager.Library.Entities;

namespace MarvelRivalManager.Library.Util
{
    public class KindOfModUtil
    {
        /// <summary>
        ///     Basic structure of the backup folders
        /// </summary>
        public static string[] BasicStructure(KindOfMod kind)
        {
            return kind switch
            {
                KindOfMod.Characters => ["Marvel", "Characters", "VFX"],
                KindOfMod.UI => ["Marvel", "Marvel_LQ", "UI"],
                KindOfMod.Movies => ["Marvel", "Movies", "Movies_HeroSkill", "Movies_Level"],
                KindOfMod.Audio => ["Marvel", "WwiseAudio", "Wwise"],
                _ => []
            };
        }
    }
}
