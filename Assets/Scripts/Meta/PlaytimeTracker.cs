using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Tracks how long the application has been open, summed across every session,
/// and persists the running total. It spawns itself before the first scene
/// loads and survives every load after, so the clock covers the menus and the
/// fight alike and no scene has to wire it up.
/// The total is banked whenever a scene comes up - the main menu and the game
/// scene both - and again if the application is backgrounded or closed.
/// </summary>
public class PlaytimeTracker : MonoBehaviour
{
    //----------Variables----------\\
    //Singleton
    public static PlaytimeTracker Instance { get; private set; }

    //Setting Key
    private const string TotalKey = "playtime_seconds";

    //Trackers
    private float total; // seconds spent in the application, every session summed

    //----------Bootstrap----------\\
    /*Create
     * 1 - Spawn the tracker before the first scene loads, so the clock starts
     *     with the application and nothing in a scene has to reference it
     */
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Create()
    {
        if (Instance != null) return;
        new GameObject(nameof(PlaytimeTracker)).AddComponent<PlaytimeTracker>();
    }

    //----------Event Loop----------\\
    /*Awake
     * 1 - Establish the singleton and survive scene loads
     * 2 - Read the stored total back so this session continues it
     * 3 - Bank the total on every scene load
     */
    private void Awake()
    {
        //1
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        //2
        total = PlayerPrefs.GetFloat(TotalKey, 0f);
        //3
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    /*On Destroy
     * 1 - If not the singleton just go
     * 2 - Unbind, and bank the total on the way out
     */
    private void OnDestroy()
    {
        //1
        if (Instance != this) return;
        //2
        SceneManager.sceneLoaded -= OnSceneLoaded;
        Save();
        Instance = null;
    }

    // Unscaled, so a paused game still counts as time spent in the application
    private void Update() => total += Time.unscaledDeltaTime;

    // The main menu and the game scene both bank the total as they come up
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Save();

    // Catch the application being backgrounded or closed between scene loads
    private void OnApplicationPause(bool paused) { if (paused) Save(); }
    private void OnApplicationQuit() => Save();

    //----------Public Functions----------\\
    //Seconds spent in the application across every session
    public float TotalSeconds => total;

    //True once the total has passed the given number of seconds
    public bool Reached(float seconds) => total >= seconds;

    /*Save
     * 1 - Write the running total and flush it to disk
     */
    public void Save()
    {
        PlayerPrefs.SetFloat(TotalKey, total);
        PlayerPrefs.Save();
    }
}
