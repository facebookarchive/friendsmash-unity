using UnityEngine;
using System.Collections;

public class WebAssetManager : MonoBehaviour
{

    private static WebAssetManager instance;

    public static WebAssetManager Instance
    {
        get { return instance == null ? (instance = new WebAssetManager()) : instance; }
    }

    // instance props are public so we can easily set them in the editor
    private string staticResourceBaseUrl;
    public static string StaticResourceBaseUrl
    {
        set { instance.staticResourceBaseUrl = value; }
        get { return instance.staticResourceBaseUrl; }
    }

    private string appBaseUrl;
    public static string AppBaseUrl
    {
        set { instance.appBaseUrl = value; }
        get { return instance.appBaseUrl; }
    }

    void Awake()
    {
        DontDestroyOnLoad(this);
    }
}
