using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TButt.Settings;

namespace TButt
{
    public static class TBGameSettings
    {
        public static readonly string playerSettingsFilename = "PlayerSettings";
        private static TBPlayerSettings _playerSettings;
        private static bool _initialized = false;

        public static void Initialize()
        {
            switch (TBCore.GetActivePlatform())
            {
                case VRPlatform.PlayStationVR:
                    TBServiceDataManager.Async.Load(playerSettingsFilename);
                    return;
                 default:
                    _playerSettings = TBDataManager.DeserializeFromFile<TBPlayerSettings>(playerSettingsFilename + ".json", TBDataManager.PathType.PersistentDataPath);
                    break;

            }
        }

        public static void ReadPlayerSettings(TBPlayerSettings settings)
        {
            if (_playerSettings.language == 0)
            {
                TBLogging.LogMessage("Player settings not found. Creating defaults.");
                _playerSettings = CreateDefaultPlayerSettings();
            }
            else
            {
                TBLogging.LogMessage("Loaded player settings.");

                switch (TBCore.GetActivePlatform())
                {
                    case VRPlatform.OculusPC:
                    case VRPlatform.SteamVR:
                        UnityEngine.XR.XRSettings.eyeTextureResolutionScale = _playerSettings.renderScale;
                        QualitySettings.antiAliasing = _playerSettings.antialiasing;
                        break;
                }
            }

            _initialized = true;
        }

        private static TBPlayerSettings CreateDefaultPlayerSettings()
        {
            TBPlayerSettings defaultPlayerSettings = new TBPlayerSettings();
            defaultPlayerSettings.language = Application.systemLanguage;
            defaultPlayerSettings.musicVolume = 1;
            defaultPlayerSettings.voVolume = 1;
            defaultPlayerSettings.sfxVolume = 1;
            defaultPlayerSettings.renderScale = TBSettings.GetConfiguredRenderscale();
            defaultPlayerSettings.antialiasing = QualitySettings.antiAliasing;
            return defaultPlayerSettings;
        }

        public static TBPlayerSettings GetActivePlayerSettings()
        {
            if (!_initialized)
                Initialize();
            return _playerSettings;
        }

        public static void SetMusicVolume(float vol)
        {
            _playerSettings.musicVolume = vol;
        }

        public static void SetSFXVolume(float vol)
        {
            _playerSettings.sfxVolume = vol;
        }

        public static void SetLanguage(SystemLanguage lang)
        {
            _playerSettings.language = lang;
        }

        public static void SetRenderscale(float scale)
        {
            if (scale > 2)
                scale = 2;

            _playerSettings.renderScale = scale;
            if (TBCore.IsNativeIntegration())
                UnityEngine.XR.XRSettings.eyeTextureResolutionScale = scale;
        }

        public static void SetAntialiasing(int aa)
        {
            QualitySettings.antiAliasing = aa;
            _playerSettings.antialiasing = aa;
        }

        public static void SaveSettings()
        {
            // TBDataManager.Async.Save(_playerSettings, playerSettingsFilename, "Settings", "Game settings", false);
            switch (TBCore.GetActivePlatform())
            {
                case VRPlatform.PlayStationVR:
                    TBServiceDataManager.Async.Save(_playerSettings, playerSettingsFilename);
                    return;
                default:
                    TBDataManager.SerializeObjectToFile(_playerSettings, playerSettingsFilename + ".json");
                    break;

            }
        }
    }

    /// <summary>
    /// Settings that can be overridden by the player, such as in an options menu.
    /// </summary>
    [System.Serializable]
    public struct TBPlayerSettings
    {
        public SystemLanguage language;
        public float musicVolume;
        public float voVolume;
        public float sfxVolume;
        public float renderScale;
        public int antialiasing;
    }
}