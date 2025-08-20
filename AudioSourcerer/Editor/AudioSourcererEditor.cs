using UnityEngine;
using UnityEditor;
using System.Reflection;

#if UNITY_EDITOR
[CustomEditor(typeof(AudioSourcerer))]
public class AudioSourcererEditor : Editor
{
    private AudioSourcerer _target;
    private SerializedProperty _clipProp;
    private SerializedProperty _playOnAwakeProp;
    private SerializedProperty _loopProp;
    
    // Volume properties
    private SerializedProperty _volumeProp;
    private SerializedProperty _minVolumeProp;
    private SerializedProperty _maxVolumeProp;
    private SerializedProperty _volumeRandomizationTypeProp;
    
    // Pitch properties
    private SerializedProperty _pitchProp;
    private SerializedProperty _minPitchProp;
    private SerializedProperty _maxPitchProp;
    private SerializedProperty _pitchRandomizationTypeProp;
    
    private SerializedProperty _spatialBlendProp;
    private SerializedProperty _minDistanceProp;
    private SerializedProperty _maxDistanceProp;
    
    // Editor preview audio
    private bool _isPreviewPlaying = false;
    private bool _isPreviewPaused = false;
    private int _previewID = -1;

    // Reflection caching
    private Assembly _unityEditorAssembly;
    private System.Type _audioUtilClass;
    private MethodInfo _playClipMethod;
    private MethodInfo _stopClipMethod;
    private MethodInfo _pauseClipMethod;
    private MethodInfo _resumeClipMethod;
    private MethodInfo _setPitchMethod;
    private MethodInfo _setVolumeMethod;
    private bool _reflectionInitialized = false;

    private void OnEnable()
    {
        _target = (AudioSourcerer)target;
        
        _clipProp = serializedObject.FindProperty("_clip");
        _playOnAwakeProp = serializedObject.FindProperty("_playOnAwake");
        _loopProp = serializedObject.FindProperty("_loop");
        
        // Volume properties
        _volumeProp = serializedObject.FindProperty("_volume");
        _minVolumeProp = serializedObject.FindProperty("_minVolume");
        _maxVolumeProp = serializedObject.FindProperty("_maxVolume");
        _volumeRandomizationTypeProp = serializedObject.FindProperty("_volumeRandomizationType");
        
        // Pitch properties
        _pitchProp = serializedObject.FindProperty("_pitch");
        _minPitchProp = serializedObject.FindProperty("_minPitch");
        _maxPitchProp = serializedObject.FindProperty("_maxPitch");
        _pitchRandomizationTypeProp = serializedObject.FindProperty("_pitchRandomizationType");
        
        _spatialBlendProp = serializedObject.FindProperty("_spatialBlend");
        _minDistanceProp = serializedObject.FindProperty("_minDistance");
        _maxDistanceProp = serializedObject.FindProperty("_maxDistance");
        
        // Initialize reflection methods
        InitializeReflectionMethods();
    }

    private void InitializeReflectionMethods()
    {
        try
        {
            _unityEditorAssembly = typeof(AudioImporter).Assembly;
            _audioUtilClass = _unityEditorAssembly.GetType("UnityEditor.AudioUtil");
            
            if (_audioUtilClass == null)
            {
                Debug.LogError("[AudioSourcererEditor] Failed to find AudioUtil class");
                return;
            }
            
            // Try to find methods with alternate names/signatures
            // For PlayPreviewClip, try different possible signatures
            _playClipMethod = 
                _audioUtilClass.GetMethod("PlayPreviewClip", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, new System.Type[] { typeof(AudioClip), typeof(int), typeof(bool) }, null) ??
                _audioUtilClass.GetMethod("PlayClip", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, new System.Type[] { typeof(AudioClip), typeof(int), typeof(bool) }, null) ??
                _audioUtilClass.GetMethod("PlayClip", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, new System.Type[] { typeof(AudioClip) }, null);
            
            // For StopAllPreviewClips, try different possible names
            _stopClipMethod = 
                _audioUtilClass.GetMethod("StopAllPreviewClips", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) ??
                _audioUtilClass.GetMethod("StopAllClips", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            // Look for pause/resume methods
            _pauseClipMethod = 
                _audioUtilClass.GetMethod("PausePreviewClip", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) ??
                _audioUtilClass.GetMethod("PauseClip", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            
            _resumeClipMethod = 
                _audioUtilClass.GetMethod("ResumePreviewClip", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) ??
                _audioUtilClass.GetMethod("ResumeClip", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            
            // For SetPreviewClipPitch, try different possible names
            _setPitchMethod = 
                _audioUtilClass.GetMethod("SetPreviewClipPitch", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, new System.Type[] { typeof(float) }, null) ??
                _audioUtilClass.GetMethod("SetClipPitch", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, new System.Type[] { typeof(float) }, null);
            
            // For SetPreviewClipVolume, try different possible names
            _setVolumeMethod = 
                _audioUtilClass.GetMethod("SetPreviewClipVolume", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, new System.Type[] { typeof(float) }, null) ??
                _audioUtilClass.GetMethod("SetClipVolume", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, new System.Type[] { typeof(float) }, null);
            
            _reflectionInitialized = _playClipMethod != null && _stopClipMethod != null;
            
            if (!_reflectionInitialized)
            {
                Debug.LogError("[AudioSourcererEditor] Failed to initialize required AudioUtil methods");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[AudioSourcererEditor] Error initializing reflection methods: {e.Message}");
            _reflectionInitialized = false;
        }
    }

    private void OnDisable()
    {
        StopPreview();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(_clipProp);
        EditorGUILayout.PropertyField(_playOnAwakeProp);
        EditorGUILayout.PropertyField(_loopProp);
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Volume Settings", EditorStyles.boldLabel);
        
        // Draw volume properties
        EditorGUILayout.PropertyField(_volumeRandomizationTypeProp, new GUIContent("Volume Randomization"));
        
        if ((AudioSourcerer.RandomizationType)_volumeRandomizationTypeProp.enumValueIndex == AudioSourcerer.RandomizationType.None)
        {
            EditorGUILayout.PropertyField(_volumeProp);
        }
        else
        {
            // Min-max slider for volume
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Volume Range");
            
            float minVol = _minVolumeProp.floatValue;
            float maxVol = _maxVolumeProp.floatValue;
            
            // Display numeric values
            EditorGUILayout.LabelField(minVol.ToString("F2"), GUILayout.Width(35));
            
            // Min-max slider
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.MinMaxSlider(ref minVol, ref maxVol, 0f, 1f, GUILayout.ExpandWidth(true));
            if (EditorGUI.EndChangeCheck())
            {
                _minVolumeProp.floatValue = minVol;
                _maxVolumeProp.floatValue = maxVol;
            }
            
            EditorGUILayout.LabelField(maxVol.ToString("F2"), GUILayout.Width(35));
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Pitch Settings", EditorStyles.boldLabel);
        
        // Draw pitch properties
        EditorGUILayout.PropertyField(_pitchRandomizationTypeProp, new GUIContent("Pitch Randomization"));
        
        if ((AudioSourcerer.RandomizationType)_pitchRandomizationTypeProp.enumValueIndex == AudioSourcerer.RandomizationType.None)
        {
            EditorGUILayout.PropertyField(_pitchProp);
        }
        else
        {
            // Min-max slider for pitch
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Pitch Range");
            
            float minPitch = _minPitchProp.floatValue;
            float maxPitch = _maxPitchProp.floatValue;
            
            // Display numeric values
            EditorGUILayout.LabelField(minPitch.ToString("F2"), GUILayout.Width(35));
            
            // Min-max slider
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.MinMaxSlider(ref minPitch, ref maxPitch, -3f, 3f, GUILayout.ExpandWidth(true));
            if (EditorGUI.EndChangeCheck())
            {
                _minPitchProp.floatValue = minPitch;
                _maxPitchProp.floatValue = maxPitch;
            }
            
            EditorGUILayout.LabelField(maxPitch.ToString("F2"), GUILayout.Width(35));
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("3D Sound Settings", EditorStyles.boldLabel);
        
        EditorGUILayout.Slider(_spatialBlendProp, 0f, 1f, new GUIContent("Spatial Blend", "Sets how much the sound is affected by 3D spatialisation (0 = 2D, 1 = 3D)"));
        
        if (_spatialBlendProp.floatValue > 0)
        {
            // Min-max slider for distance
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Distance Range");
            
            float minDist = _minDistanceProp.floatValue;
            float maxDist = _maxDistanceProp.floatValue;
            
            // Display numeric values
            EditorGUILayout.LabelField(minDist.ToString("F1"), GUILayout.Width(35));
            
            // Min-max slider - using 0 to 1000 as reasonable max range for audio
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.MinMaxSlider(ref minDist, ref maxDist, 0f, 1000f, GUILayout.ExpandWidth(true));
            if (EditorGUI.EndChangeCheck())
            {
                _minDistanceProp.floatValue = minDist;
                _maxDistanceProp.floatValue = maxDist;
            }
            
            EditorGUILayout.LabelField(maxDist.ToString("F1"), GUILayout.Width(35));
            EditorGUILayout.EndHorizontal();
        }

        serializedObject.ApplyModifiedProperties();

        // Playback controls
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Playback Controls", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        
        if (Application.isPlaying)
        {
            // RUNTIME CONTROLS - Always show all three buttons
            
            // Play/Resume button
            bool isPaused = !_target.isPlaying && _previewID != -1;
            GUI.enabled = !_target.isPlaying || isPaused;
            if (GUILayout.Button(isPaused ? "Resume" : "Play"))
            {
                if (isPaused)
                    _target.UnPause();
                else
                    _target.Play();
            }
            
            // Pause button
            GUI.enabled = _target.isPlaying;
            if (GUILayout.Button("Pause"))
            {
                _target.Pause();
            }
            
            // Stop button
            GUI.enabled = _target.isPlaying || isPaused;
            if (GUILayout.Button("Stop"))
            {
                _target.Stop();
                _previewID = -1; // Reset preview ID to indicate not paused
            }
            
            GUI.enabled = true;
        }
        else
        {
            // EDITOR PREVIEW CONTROLS - Always show all three buttons
            
            AudioClip clip = (AudioClip)_clipProp.objectReferenceValue;
            bool hasClip = clip != null && _reflectionInitialized;
            
            // Preview/Resume button
            GUI.enabled = hasClip && (!_isPreviewPlaying || _isPreviewPaused);
            if (GUILayout.Button(_isPreviewPaused ? "Resume" : "Preview"))
            {
                if (_isPreviewPaused)
                    ResumePreview();
                else
                    PlayPreview();
            }
            
            // Pause button
            GUI.enabled = hasClip && _isPreviewPlaying && !_isPreviewPaused;
            if (GUILayout.Button("Pause"))
            {
                PausePreview();
            }
            
            // Stop button
            GUI.enabled = hasClip && (_isPreviewPlaying || _isPreviewPaused);
            if (GUILayout.Button("Stop"))
            {
                StopPreview();
            }
            
            GUI.enabled = true;
            
            if (!_reflectionInitialized)
            {
                EditorGUILayout.HelpBox("Editor preview not available: AudioUtil reflection failed to initialize", MessageType.Warning);
            }
        }
        
        EditorGUILayout.EndHorizontal();
    }
    
    private void PlayPreview()
    {
        if (_clipProp.objectReferenceValue == null || !_reflectionInitialized) return;
        
        AudioClip clip = (AudioClip)_clipProp.objectReferenceValue;
        
        Debug.Log($"[AudioSourcerer] Preview playing: {clip.name}");
        
        // Get a random volume and pitch based on settings
        float previewVolume = _volumeProp.floatValue;
        float previewPitch = _pitchProp.floatValue;
        
        if ((AudioSourcerer.RandomizationType)_volumeRandomizationTypeProp.enumValueIndex != AudioSourcerer.RandomizationType.None)
        {
            if ((AudioSourcerer.RandomizationType)_volumeRandomizationTypeProp.enumValueIndex == AudioSourcerer.RandomizationType.Range)
            {
                previewVolume = UnityEngine.Random.Range(_minVolumeProp.floatValue, _maxVolumeProp.floatValue);
            }
            else // MinOrMax
            {
                previewVolume = UnityEngine.Random.value < 0.5f ? _minVolumeProp.floatValue : _maxVolumeProp.floatValue;
            }
        }
        
        if ((AudioSourcerer.RandomizationType)_pitchRandomizationTypeProp.enumValueIndex != AudioSourcerer.RandomizationType.None)
        {
            if ((AudioSourcerer.RandomizationType)_pitchRandomizationTypeProp.enumValueIndex == AudioSourcerer.RandomizationType.Range)
            {
                previewPitch = UnityEngine.Random.Range(_minPitchProp.floatValue, _maxPitchProp.floatValue);
            }
            else // MinOrMax
            {
                previewPitch = UnityEngine.Random.value < 0.5f ? _minPitchProp.floatValue : _maxPitchProp.floatValue;
            }
        }
        
        // Start the preview using reflection
        StopPreview();
        
        try
        {
            object[] parameters;
            if (_playClipMethod.GetParameters().Length == 3)
            {
                parameters = new object[] { clip, 0, false };
            }
            else if (_playClipMethod.GetParameters().Length == 1)
            {
                parameters = new object[] { clip };
            }
            else
            {
                Debug.LogError($"[AudioSourcerer] Unsupported play method parameters count: {_playClipMethod.GetParameters().Length}");
                return;
            }
            
            object result = _playClipMethod.Invoke(null, parameters);
            if (result != null && result is int)
            {
                _previewID = (int)result;
            }
            else
            {
                _previewID = 0; // Default ID if method doesn't return an ID
            }
            
            // Set volume and pitch if those methods are available
            if (_setVolumeMethod != null)
            {
                _setVolumeMethod.Invoke(null, new object[] { previewVolume });
            }
            
            if (_setPitchMethod != null)
            {
                _setPitchMethod.Invoke(null, new object[] { previewPitch });
            }
            
            _isPreviewPlaying = true;
            _isPreviewPaused = false;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[AudioSourcerer] Error during preview playback: {e.Message}");
            _isPreviewPlaying = false;
            _isPreviewPaused = false;
            _previewID = -1;
        }
    }
    
    private void StopPreview()
    {
        if (!_isPreviewPlaying && !_isPreviewPaused) return;
        
        Debug.Log("[AudioSourcerer] Preview stopped");
        
        if (_reflectionInitialized)
        {
            try
            {
                _stopClipMethod.Invoke(null, null);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[AudioSourcerer] Error stopping preview: {e.Message}");
            }
        }
        
        _isPreviewPlaying = false;
        _isPreviewPaused = false;
        _previewID = -1;
    }
    
    private void PausePreview()
    {
        if (!_isPreviewPlaying || _isPreviewPaused || !_reflectionInitialized) return;
        
        Debug.Log("[AudioSourcerer] Preview paused");
        
        if (_pauseClipMethod != null)
        {
            try
            {
                _pauseClipMethod.Invoke(null, null);
                _isPreviewPaused = true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[AudioSourcerer] Error pausing preview: {e.Message}");
            }
        }
        else
        {
            // If no pause method is available, just stop the preview
            Debug.LogError("[AudioSourcerer] No pause method found, stopping preview instead");
            StopPreview();
        }
    }
    
    private void ResumePreview()
    {
        if (!_isPreviewPaused || !_reflectionInitialized) return;
        
        Debug.Log("[AudioSourcerer] Preview resumed");
        
        if (_resumeClipMethod != null)
        {
            try
            {
                _resumeClipMethod.Invoke(null, null);
                _isPreviewPaused = false;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[AudioSourcerer] Error resuming preview: {e.Message}");
            }
        }
        else
        {
            // If no resume method is available, just restart the preview
            Debug.LogError("[AudioSourcerer] No resume method found, restarting preview instead");
            PlayPreview();
        }
    }
}
#endif