using ToolbarControl_NS;
using UnityEngine;

namespace PartsReference
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class RegisterToolbar : MonoBehaviour
    {
        public const string MODID = "PartsReference";
        public const string MODNAME = "Parts Reference";


        void Start()
        {
            ToolbarControl.RegisterMod(MODID, MODNAME);
#if false
            Log = new KSP_Log.Log("PartsReference"
#if DEBUG
                , KSP_Log.Log.LEVEL.DETAIL
#endif
                    );
#endif
        }
    }
}