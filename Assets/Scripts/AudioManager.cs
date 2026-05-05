using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Singleton AudioManager — drag your clips into the Inspector arrays,
// then call  AudioManager.Play(SoundCategory.UI)  from anywhere.
public class AudioManager : MonoBehaviour
{
    public enum SoundCategory
    {
        // means used in a particular instance/logic
        ObjPickup, //
        ObjDrop, //
        Correct, //
        Wrong, //
        Ding, //
        Loud, //
        Laughter, //
        Random, //
        Discover, //
        Eerie, //
        Shop, //
        QuizFail,
        NewRound //
    }

    [Serializable]
    public class SoundGroup
    {
        public SoundCategory category;
        public AudioClip[] clips;
        [Range(0f, 1f)] public float volume = 1f;
    }

    // THE SINGLETON INSTANCE
    // This allows any script in the game to say "AudioManager.Instance" to talk to it.
    public static AudioManager Instance;

    [Header("Sound Groups — fill these in the Inspector")]
    [SerializeField] private SoundGroup[] soundGroups;

    [Header("Pool size for simultaneous sounds")]
    [SerializeField] private int audioSourcePoolSize = 8;

    private Dictionary<SoundCategory, SoundGroup> _lookup;
    private AudioSource[] _pool;
    private int _poolIndex;

    private void Awake()
    {
        // Singleton Setup: Ensure there is only ever ONE AudioManager in the scene
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildLookup();
        BuildPool();
    }

    private void BuildLookup()
    {
        _lookup = new Dictionary<SoundCategory, SoundGroup>();
        foreach (var group in soundGroups)
        {
            if (!_lookup.ContainsKey(group.category))
                _lookup.Add(group.category, group);
            else
                Debug.LogWarning($"[AudioManager] Duplicate category: {group.category}");
        }
    }

    private void BuildPool()
    {
        _pool = new AudioSource[audioSourcePoolSize];
        for (int i = 0; i < audioSourcePoolSize; i++)
        {
            var go = new GameObject($"PooledAudio_{i}");
            go.transform.SetParent(transform);
            _pool[i] = go.AddComponent<AudioSource>();
            _pool[i].playOnAwake = false;
            _pool[i].spatialBlend = 0f; // 2D by default
        }
    }

    // Play a random clip from the category (2D, non-positional)
    public static void Play(SoundCategory category)
    {
        Instance?.PlayInternal(category);
    }

    // Play at a world position (3D spatial)
    public static void PlayAtPoint(SoundCategory category, Vector3 position)
    {
        if (Instance == null) return;
        if (!Instance.TryGetClip(category, out var clip, out var group)) return;

        AudioSource.PlayClipAtPoint(clip, position, group.volume);
    }

    private void PlayInternal(SoundCategory category, int index = -1)
    {
        if (!TryGetClip(category, out var clip, out var group, index)) return;

        var source = NextSource();
        source.clip = clip;
        source.volume = group.volume;
        source.pitch = UnityEngine.Random.Range(0.8f, 1.2f);
        source.spatialBlend = 0f;
        source.Play();
    }

    private bool TryGetClip(SoundCategory category, out AudioClip clip, out SoundGroup group, int index = -1)
    {
        clip = null;
        group = null;

        if (!_lookup.TryGetValue(category, out group))
        {
            Debug.LogWarning($"[AudioManager] No group for category: {category}");
            return false;
        }

        if (group.clips == null || group.clips.Length == 0)
        {
            Debug.LogWarning($"[AudioManager] No clips assigned for: {category}");
            return false;
        }

        clip = index >= 0 && index < group.clips.Length
            ? group.clips[index]
            : group.clips[UnityEngine.Random.Range(0, group.clips.Length)];

        return clip != null;
    }

    private AudioSource NextSource()
    {
        var source = _pool[_poolIndex];
        _poolIndex = (_poolIndex + 1) % _pool.Length;
        return source;
    }
}