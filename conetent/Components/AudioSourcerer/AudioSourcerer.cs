using UnityEngine;
using System.Collections.Generic;

#if DOTWEEN
using DG.Tweening;
#endif

[AddComponentMenu("Audio/Audio Sourcerer")]
[RequireComponent(typeof(Transform))]
public class AudioSourcerer : MonoBehaviour
{
    public enum ClipCompositionType
    {
        Single,
        Random,
        Dual
    }

    [SerializeField] private ClipCompositionType _clipCompositionType = ClipCompositionType.Single;

    // Single mode
    [SerializeField] private AudioClip _clip;

    // Random mode
    [SerializeField] private List<AudioClip> _randomClips = new List<AudioClip>();

    // Dual mode
    [SerializeField] private AudioClip _dualClipTrue;
    [SerializeField] private AudioClip _dualClipFalse;

    [SerializeField] private bool _playOnAwake = true;
    [SerializeField] private bool _loop = false;

    // Volume settings
    [SerializeField] [Range(0f, 1f)] private float _volume = 1f;
    [SerializeField] [Range(0f, 1f)] private float _minVolume = 0.5f;
    [SerializeField] [Range(0f, 1f)] private float _maxVolume = 1f;
    [SerializeField] private RandomizationType _volumeRandomizationType = RandomizationType.None;

    // Pitch settings
    [SerializeField] [Range(-3f, 3f)] private float _pitch = 1f;
    [SerializeField] [Range(-3f, 3f)] private float _minPitch = 0.8f;
    [SerializeField] [Range(-3f, 3f)] private float _maxPitch = 1.2f;
    [SerializeField] private RandomizationType _pitchRandomizationType = RandomizationType.None;

    [SerializeField] [Range(0f, 1f)] private float _spatialBlend = 0f;
    [SerializeField] private float _minDistance = 1f;
    [SerializeField] private float _maxDistance = 500f;

    // Enum for randomization type
    public enum RandomizationType
    {
        None,       // No randomization
        Range,      // Random value between min and max
        MinOrMax    // Random choice between exactly min or exactly max
    }

    private AudioSource _internalSource;

    #region Properties that mirror the AudioSource API
    public AudioClip clip
    {
        get => _clip;
        set
        {
            _clip = value;
            if (_internalSource != null)
                _internalSource.clip = value;
        }
    }

    public bool playOnAwake
    {
        get => _playOnAwake;
        set
        {
            _playOnAwake = value;
            if (_internalSource != null)
                _internalSource.playOnAwake = value;
        }
    }

    public bool loop
    {
        get => _loop;
        set
        {
            _loop = value;
            if (_internalSource != null)
                _internalSource.loop = value;
        }
    }

    public float volume
    {
        get => _volume;
        set
        {
            _volume = Mathf.Clamp01(value);
            if (_internalSource != null)
                _internalSource.volume = _volume;
        }
    }

    public float pitch
    {
        get => _pitch;
        set
        {
            _pitch = Mathf.Clamp(value, -3f, 3f);
            if (_internalSource != null)
                _internalSource.pitch = _pitch;
        }
    }

    public float spatialBlend
    {
        get => _spatialBlend;
        set
        {
            _spatialBlend = Mathf.Clamp01(value);
            if (_internalSource != null)
                _internalSource.spatialBlend = _spatialBlend;
        }
    }

    public float minDistance
    {
        get => _minDistance;
        set
        {
            _minDistance = Mathf.Max(0f, value);
            if (_internalSource != null)
                _internalSource.minDistance = _minDistance;
        }
    }

    public float maxDistance
    {
        get => _maxDistance;
        set
        {
            _maxDistance = Mathf.Max(_minDistance, value);
            if (_internalSource != null)
                _internalSource.maxDistance = _maxDistance;
        }
    }

    public bool isPlaying => _internalSource != null && _internalSource.isPlaying;
    #endregion

    // Core methods
    private void Awake()
    {
        InitializeAudioSource();

        if (_playOnAwake)
        {
            switch (_clipCompositionType)
            {
                case ClipCompositionType.Single:
                    if (_clip != null)
                        Play();
                    break;
                case ClipCompositionType.Random:
                    if (_randomClips != null && _randomClips.Count > 0)
                        Play();
                    break;
                case ClipCompositionType.Dual:
                    if (_dualClipTrue != null)
                        PlayDual(true);
                    break;
            }
        }
    }

    private void OnDestroy()
    {
        if (_internalSource != null)
        {
            Destroy(_internalSource);
            _internalSource = null;
        }
    }

    private void InitializeAudioSource()
    {
        if (_internalSource == null)
        {
            _internalSource = gameObject.AddComponent<AudioSource>();
            _internalSource.hideFlags = HideFlags.HideInInspector;
        }

        _internalSource.playOnAwake = false; // We'll handle play on awake ourselves
        _internalSource.loop = _loop;
        _internalSource.spatialBlend = _spatialBlend;
        _internalSource.minDistance = _minDistance;
        _internalSource.maxDistance = _maxDistance;

        ApplyRandomizedValues();
    }

    private void ApplyRandomizedValues()
    {
        // Apply volume settings
        if (_volumeRandomizationType != RandomizationType.None)
        {
            if (_volumeRandomizationType == RandomizationType.Range)
            {
                _volume = UnityEngine.Random.Range(_minVolume, _maxVolume);
            }
            else // MinOrMax
            {
                _volume = UnityEngine.Random.value < 0.5f ? _minVolume : _maxVolume;
            }
        }

        // Apply pitch settings
        if (_pitchRandomizationType != RandomizationType.None)
        {
            if (_pitchRandomizationType == RandomizationType.Range)
            {
                _pitch = UnityEngine.Random.Range(_minPitch, _maxPitch);
            }
            else // MinOrMax
            {
                _pitch = UnityEngine.Random.value < 0.5f ? _minPitch : _maxPitch;
            }
        }

        if (_internalSource != null)
        {
            _internalSource.volume = _volume;
            _internalSource.pitch = _pitch;
        }
    }

    #region Public API that mirrors the AudioSource
    public void Play()
    {
        if (_internalSource == null)
            return;

        ApplyRandomizedValues();

        switch (_clipCompositionType)
        {
            case ClipCompositionType.Single:
                if (_clip != null)
                {
                    _internalSource.clip = _clip;
                    _internalSource.Play();
                }
                break;
            case ClipCompositionType.Random:
                if (_randomClips != null && _randomClips.Count > 0)
                {
                    AudioClip chosen = _randomClips[Random.Range(0, _randomClips.Count)];
                    _internalSource.clip = chosen;
                    _internalSource.Play();
                }
                break;
            case ClipCompositionType.Dual:
                // Do nothing, user should call PlayDual(bool)
                break;
        }
    }

    public void PlayDual(bool useFirstClip)
    {
        if (_internalSource == null || _clipCompositionType != ClipCompositionType.Dual)
            return;

        ApplyRandomizedValues();

        AudioClip playClip = useFirstClip ? _dualClipTrue : _dualClipFalse;
        if (playClip != null)
        {
            _internalSource.clip = playClip;
            _internalSource.Play();
        }
    }

    public void Stop()
    {
        if (_internalSource != null)
            _internalSource.Stop();
    }

    public void Pause()
    {
        if (_internalSource != null)
            _internalSource.Pause();
    }

    public void UnPause()
    {
        if (_internalSource != null)
            _internalSource.UnPause();
    }

    public void PlayOneShot(AudioClip clip, float volumeScale = 1.0f)
    {
        if (_internalSource != null && clip != null)
        {
            float adjustedVolume = volumeScale;

            if (_volumeRandomizationType != RandomizationType.None)
            {
                if (_volumeRandomizationType == RandomizationType.Range)
                {
                    adjustedVolume *= UnityEngine.Random.Range(_minVolume, _maxVolume);
                }
                else // MinOrMax
                {
                    adjustedVolume *= UnityEngine.Random.value < 0.5f ? _minVolume : _maxVolume;
                }
            }
            else
            {
                adjustedVolume *= _volume;
            }

            _internalSource.PlayOneShot(clip, adjustedVolume);
        }
    }

    public void PlayClipAtPoint(AudioClip clip, Vector3 position, float volume = 1.0f)
    {
        if (clip != null)
            AudioSource.PlayClipAtPoint(clip, position, volume);
    }
    #endregion

    #region DOTween Extensions
    #if DOTWEEN
    public void DOFade(float endValue, float duration)
    {
        if (_internalSource != null)
        {
            _internalSource.DOFade(endValue, duration);
        }
    }

    public void DOPitch(float endValue, float duration)
    {
        if (_internalSource != null)
        {
            _internalSource.DOPitch(endValue, duration);
        }
    }
    #endif
    #endregion

    // Expose composition type/clips for editor and users
    public ClipCompositionType clipCompositionType
    {
        get => _clipCompositionType;
        set => _clipCompositionType = value;
    }
    public AudioClip singleClip
    {
        get => _clip;
        set => _clip = value;
    }
    public List<AudioClip> randomClips => _randomClips;
    public AudioClip dualClipTrue
    {
        get => _dualClipTrue;
        set => _dualClipTrue = value;
    }
    public AudioClip dualClipFalse
    {
        get => _dualClipFalse;
        set => _dualClipFalse = value;
    }
}