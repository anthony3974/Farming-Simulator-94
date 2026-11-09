using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Saves and loads the farm (PLAN.md item 18): money, seeds, fertilizer, fuel, the crops in stock, the
/// selected crop, every field (name, owner, crop, growth stage and time, fertilizer) and the vehicles you own
/// (where they are parked and which one you drive). One save, a JSON file in Application.persistentDataPath.
///
/// When it saves (by itself, there is no SAVE button): every <see cref="autoSaveSeconds"/>, when the game is
/// paused, when you leave with MAIN MENU, and when the app goes to the background or closes (phones close
/// apps in the background without asking, so that one matters most).
///
/// Main menu: LOAD GAME (live when a save exists) sets <see cref="LoadRequested"/> and opens GameScene; this
/// script then puts the saved farm back one frame after the scene starts (after every other Start).
/// NEW GAME starts the scene's own farm; its first save replaces the old one.
///
/// It sits on the PlayerValues object; PlayerValues adds it in Play mode if it is not there. Add it in the
/// editor to change the settings below.
/// </summary>
public class SaveGame : MonoBehaviour
{
    [Tooltip("Saves by itself this often, in real seconds (0 = only on pause, main menu and closing the app).")]
    public float autoSaveSeconds = 60f;
    [Tooltip("Shows \"Farm loaded\" after loading.")]
    public bool showLoadedMessage = true;

    public const string FileName = "farm.json";
    // raise when the format changes in a way old saves need help with (see Load)
    public const int FormatVersion = 1;

    public static string SavePath => Path.Combine(Application.persistentDataPath, FileName);
    public static bool HasSave => File.Exists(SavePath);

    /// <summary>Set by the main menu's LOAD GAME: the next GameScene puts the saved farm back.</summary>
    public static bool LoadRequested { get; set; }

    public static SaveGame Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { Instance = null; LoadRequested = false; }

    private PlayerValues values;
    private GameCharacter player;
    private bool ready;     // false until the scene has started (and the save is loaded), so nothing saves a half-built farm
    private float nextAutoSave;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    IEnumerator Start()
    {
        values = GetComponent<PlayerValues>();
        if (values == null) values = FindFirstObjectByType<PlayerValues>();
        player = FindFirstObjectByType<GameCharacter>();

        // wait one frame, so every field, vehicle and PlayerValues has run its Start (names, owners, the start
        // crop, the tractor as the first vehicle) before the save overwrites them
        yield return null;

        if (LoadRequested)
        {
            LoadRequested = false;
            Load();
        }
        ready = true;
        nextAutoSave = Time.unscaledTime + autoSaveSeconds;
    }

    void Update()
    {
        if (!ready || autoSaveSeconds <= 0f || Time.unscaledTime < nextAutoSave) return;
        nextAutoSave = Time.unscaledTime + autoSaveSeconds;
        Save();
    }

    // Android: the app is put in the background (home button, a call, switching apps). It may never come back.
    void OnApplicationPause(bool paused)
    {
        if (paused) Save();
    }

    void OnApplicationQuit() => Save();

    // ---- saving ----

    /// <summary>Writes the farm to the save file. Returns false if it could not (the reason is in the log).</summary>
    public bool Save()
    {
        if (!ready || values == null) return false;
        try
        {
            string json = JsonUtility.ToJson(Capture(), true);
            string path = SavePath;
            string temp = path + ".tmp";
            // write a new file first, then swap it in, so a crash mid-write never leaves half a save
            File.WriteAllText(temp, json);
            if (File.Exists(path)) File.Replace(temp, path, null); // one step, so there is never a moment with no save
            else File.Move(temp, path);
            nextAutoSave = Time.unscaledTime + autoSaveSeconds;
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Could not save the farm to {SavePath}: {e.Message}", this);
            return false;
        }
    }

    private SaveData Capture()
    {
        var data = new SaveData
        {
            version = FormatVersion,
            savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            gameVersion = Application.version,
            money = values.Money,
            seeds = values.Seeds,
            fertilizer = values.Fertilizer,
            fuel = values.Fuel,
            wheat = values.InWheat,
            barley = values.InBarley,
            oat = values.InOat,
            selectedCrop = values.SelectedCrop,
        };

        foreach (var field in AllFields())
            if (field.enabled) data.fields.Add(field.GetSave(FieldId(field)));

        if (player != null)
        {
            foreach (var v in player.OwnedVehicles)
                data.vehicles.Add(new VehicleSave { kind = v.kind, position = v.transform.position, rotation = v.transform.rotation });
            data.activeVehicle = Vehicle.Active != null ? Vehicle.Active.kind : VehicleKind.Tractor;
        }
        return data;
    }

    // ---- loading ----

    private void Load()
    {
        SaveData data;
        try
        {
            if (!HasSave) { Debug.LogWarning("LOAD GAME: there is no save file, starting a new farm.", this); return; }
            data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
            if (data == null) throw new Exception("the file is empty");
        }
        catch (Exception e)
        {
            // keep the broken file for a look, so the next save does not silently replace it
            Debug.LogError($"Could not load the farm from {SavePath}: {e.Message}. It was kept as {FileName}.bad; starting a new farm.", this);
            try { File.Copy(SavePath, SavePath + ".bad", true); } catch (Exception) { }
            Flash("The saved farm could not be loaded. Starting a new farm.", 5f);
            return;
        }

        if (data.version > FormatVersion)
            Debug.LogWarning($"The save is from a newer version of the game (format {data.version}, this game reads {FormatVersion}). Loading what it can.", this);

        values.Money = data.money;
        values.Seeds = data.seeds;
        values.Fertilizer = data.fertilizer;
        values.InWheat = data.wheat;
        values.InBarley = data.barley;
        values.InOat = data.oat;
        values.SelectCrop(data.selectedCrop);

        // fields: matched by their place in the hierarchy (Terrain/Fields/Field Tony 1). A field added to the
        // scene after the save keeps its new-game state; a saved field that is gone is skipped.
        var saved = new Dictionary<string, FieldSave>();
        foreach (var f in data.fields)
            if (f != null && !string.IsNullOrEmpty(f.id)) saved[f.id] = f;
        foreach (var field in AllFields())
            if (field.enabled && saved.TryGetValue(FieldId(field), out var f)) field.ApplySave(f);

        LoadVehicles(data);

        // last, so its "Low fuel" message (if any) is the one left on screen
        values.Fuel = data.fuel;
        if (showLoadedMessage && !values.FuelLow)
            Flash($"Farm loaded (saved {data.savedAt})", 3f);
    }

    private void LoadVehicles(SaveData data)
    {
        if (player == null || data.vehicles == null || data.vehicles.Count == 0) return;

        var inScene = FindObjectsByType<Vehicle>(FindObjectsSortMode.None);
        Vehicle drive = null;
        foreach (var vs in data.vehicles)
        {
            if (vs == null) continue;
            Vehicle vehicle = null;
            foreach (var v in inScene) if (v.kind == vs.kind) { vehicle = v; break; }
            if (vehicle == null)
            {
                Debug.LogWarning($"The save owns a {vs.kind}, but there is none in the scene.", this);
                continue;
            }
            vehicle.PlaceAt(vs.position, vs.rotation);
            player.AddVehicle(vehicle);
            if (vs.kind == data.activeVehicle) drive = vehicle;
        }
        if (drive != null) player.Drive(drive);
    }

    // ---- helpers ----

    private static FieldController[] AllFields() => FindObjectsByType<FieldController>(FindObjectsSortMode.None);

    /// <summary>The field's place in the hierarchy, for example "Terrain/Fields/Field Tony 1". Stays the same
    /// between games, unlike the AI fields' random names.</summary>
    private static string FieldId(FieldController field)
    {
        var parts = new List<string>();
        for (var t = field.transform; t != null; t = t.parent)
        {
            // two objects with the same name under one parent: add the number of the same-named ones before it
            string part = t.name;
            if (t.parent != null)
            {
                int same = 0;
                for (int i = 0; i < t.GetSiblingIndex(); i++)
                    if (t.parent.GetChild(i).name == t.name) same++;
                if (same > 0) part += "#" + same;
            }
            parts.Add(part);
        }
        parts.Reverse();
        return string.Join("/", parts);
    }

    private static void Flash(string text, float seconds)
    {
        var prompt = ScreenPrompt.Instance;
        if (prompt != null) prompt.Flash(text, seconds);
    }
}

// ---- the save file (JSON). Add new fields at the end with a sensible default: old saves just lack them. ----

[Serializable]
public class SaveData
{
    public int version;
    public string savedAt;
    public string gameVersion;

    public double money;        // a float until 2026-11-08; older saves load the same
    public int seeds;
    public int fertilizer;
    public float fuel;
    public int wheat;
    public int barley;
    public int oat;
    public CropType selectedCrop = CropType.Wheat;

    public List<FieldSave> fields = new();
    public List<VehicleSave> vehicles = new();
    public VehicleKind activeVehicle = VehicleKind.Tractor;
}

[Serializable]
public class FieldSave
{
    public string id;           // place in the hierarchy (see SaveGame.FieldId)
    public string name;         // the field's name (AI fields get a random one each new game)
    public string owner;
    public CropType crop;
    public int stage;           // FieldController.Stage: 0 empty, 1 planted, 2 to 4 growing, 5 ready
    public float growSeconds;   // time spent in the current growth stage
    public int fertilizer;      // fertilizer levels (0 to 2)
    public int fertilizedAt;    // growth stage of the last fertilizing (one per stage)
}

[Serializable]
public class VehicleSave
{
    public VehicleKind kind;
    public Vector3 position;
    public Quaternion rotation;
}
