using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using Tomatech.RePalette;


public class ThemeAssetLoader : MonoBehaviour, IThemeable
{
    [SerializeField]
    ThemeAssetReference<Sprite> assetRef;
    [SerializeField]
    UnityEvent<Sprite> onThemeSpriteRecieved;

    public async Task UpdateThemeContent()
    {
        var assetTask = assetRef?.GetAsset();
        if (assetTask != null)
        {
            await assetTask;
            onThemeSpriteRecieved.Invoke(assetTask.Result);
        }
    }

    private void OnEnable()
    {
        RepaletteResourceManager.RegisterThemeable(this);
        var _ = UpdateThemeContent();
    }

    private void OnDisable()
    {
        RepaletteResourceManager.UnregisterThemeable(this);
    }

    private void OnDestroy()
    {
        assetRef.ReleaseAsset();
    }

}
