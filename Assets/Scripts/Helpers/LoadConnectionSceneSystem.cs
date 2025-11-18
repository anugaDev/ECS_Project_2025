using Unity.Entities;
using UnityEngine.SceneManagement;

namespace Helpers
{
    public partial class LoadConnectionSceneSystem : SystemBase
    {

        protected override void OnCreate()
        {
            Enabled = true;

            if (SceneManager.GetActiveScene() == SceneManager.GetSceneByBuildIndex(0))
            {
                return;
            }

            SceneManager.LoadScene(GlobalParameters.CONNECTION_SCENE_INDEX);
        }

        protected override void OnUpdate()
        {
        }
    }
}
