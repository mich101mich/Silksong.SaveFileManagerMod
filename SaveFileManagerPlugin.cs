using BepInEx;

namespace SaveFileManager
{
    // TODO - adjust the plugin guid as needed
    [BepInAutoPlugin(id: "io.github.mich101mich.savefilemanager")]
    public partial class SaveFileManagerPlugin : BaseUnityPlugin
    {
        private void Awake()
        {
            // Put your initialization logic here
            Logger.LogInfo($"Plugin {Name} ({Id}) has loaded!");
        }
    }
}
