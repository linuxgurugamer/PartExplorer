using ToolbarControl_NS;
using UnityEngine;

namespace PartExplorer
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class RegisterToolbar : MonoBehaviour
    {
        public const string MODID = "PartExplorer";
        public const string MODNAME = "Part Explorer";


        void Start()
        {
            ToolbarControl.RegisterMod(MODID, MODNAME);
#if false
            Log = new KSP_Log.Log("PartExplorer"
#if DEBUG
                , KSP_Log.Log.LEVEL.DETAIL
#endif
                    );
#endif
        }
    }
}