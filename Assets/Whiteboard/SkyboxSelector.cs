using UnityEngine;

public class SkyboxSelector : MonoBehaviour
{
    [Header("Skybox Options")]
    [SerializeField] private Material[] skyboxes;

    // Select and apply a random skybox from the available options
    public void SelectRandomSkybox()
    {
        int index = Random.Range(0, skyboxes.Length);
        RenderSettings.skybox = skyboxes[index];
        DynamicGI.UpdateEnvironment();
    }
}
