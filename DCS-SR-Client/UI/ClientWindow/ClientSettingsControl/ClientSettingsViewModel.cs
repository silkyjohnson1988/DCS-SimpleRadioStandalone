using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using Ciribob.DCS.SimpleRadio.Standalone.Client.Audio.Managers;
using Ciribob.DCS.SimpleRadio.Standalone.Client.Settings;
using Ciribob.DCS.SimpleRadio.Standalone.Client.Singletons;
using Ciribob.DCS.SimpleRadio.Standalone.Client.Utils;
using Ciribob.SRS.Common.Helpers;
using NLog;

namespace Ciribob.DCS.SimpleRadio.Standalone.Client.UI.ClientWindow.ClientSettingsControl;
using PropertyChangedBase = Ciribob.SRS.Common.Helpers.PropertyChangedBase;
public class ClientSettingsViewModel : PropertyChangedBase
{
    private readonly GlobalSettingsStore _globalSettings = GlobalSettingsStore.Instance;
    private readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public ClientSettingsViewModel()
    {
        
        ResetOverlayCommand = new DelegateCommand(() =>
        {
            //TODO trigger event on messagehub
            //close overlay
            //    _radioOverlayWindow?.Close();
            //    _radioOverlayWindow = null;

            _globalSettings.SetPositionSetting(GlobalSettingsKeys.RadioX, 300);
            _globalSettings.SetPositionSetting(GlobalSettingsKeys.RadioY, 300);
            _globalSettings.SetPositionSetting(GlobalSettingsKeys.RadioWidth, 122);
            _globalSettings.SetPositionSetting(GlobalSettingsKeys.RadioHeight, 270);
            _globalSettings.SetPositionSetting(GlobalSettingsKeys.RadioOpacity, 1.0);
        });

        CreateProfileCommand = new DelegateCommand(() =>
        {
            var inputProfileWindow = new InputProfileWindow.InputProfileWindow(name =>
            {
                if (name.Trim().Length > 0)
                {
                    _globalSettings.ProfileSettingsStore.AddNewProfile(name);

                    NotifyPropertyChanged(nameof(AvailableProfiles));
                    ReloadSettings();
                }
            });
            inputProfileWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            inputProfileWindow.Owner = Application.Current.MainWindow;
            inputProfileWindow.ShowDialog();
        });

        CopyProfileCommand = new DelegateCommand(() =>
        {
            var current = _globalSettings.ProfileSettingsStore.CurrentProfileName;
            var inputProfileWindow = new InputProfileWindow.InputProfileWindow(name =>
            {
                if (name.Trim().Length > 0)
                {
                    _globalSettings.ProfileSettingsStore.CopyProfile(current, name);
                    NotifyPropertyChanged(nameof(AvailableProfiles));
                    ReloadSettings();
                }
            });
            inputProfileWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            inputProfileWindow.Owner = Application.Current.MainWindow;
            inputProfileWindow.ShowDialog();
        });

        RenameProfileCommand = new DelegateCommand(() =>
        {
            var current = _globalSettings.ProfileSettingsStore.CurrentProfileName;
            if (current.Equals("default"))
            {
                MessageBox.Show(Application.Current.MainWindow,
                    "Cannot rename the default input!",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            else
            {
                var oldName = current;
                var inputProfileWindow = new InputProfileWindow.InputProfileWindow(name =>
                {
                    if (name.Trim().Length > 0)
                    {
                        _globalSettings.ProfileSettingsStore.RenameProfile(oldName, name);
                        SelectedProfile = _globalSettings.ProfileSettingsStore.CurrentProfileName;
                        NotifyPropertyChanged(nameof(AvailableProfiles));
                        ReloadSettings();
                    }
                }, true, oldName);
                inputProfileWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                inputProfileWindow.Owner = Application.Current.MainWindow;
                inputProfileWindow.ShowDialog();
            }
        });

        DeleteProfileCommand = new DelegateCommand(() =>
        {
            var current = _globalSettings.ProfileSettingsStore.CurrentProfileName;

            if (current.Equals("default"))
            {
                MessageBox.Show(Application.Current.MainWindow,
                    "Cannot delete the default input!",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            else
            {
                var result = MessageBox.Show(Application.Current.MainWindow,
                    $"Are you sure you want to delete {current} ?",
                    "Confirmation",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    _globalSettings.ProfileSettingsStore.RemoveProfile(current);
                    SelectedProfile = _globalSettings.ProfileSettingsStore.CurrentProfileName;
                    NotifyPropertyChanged(nameof(AvailableProfiles));
                    ReloadSettings();
                }
            }
        });

        SetSRSPathCommand = new DelegateCommand(() => { }
            
            //TODO fill this in
            );
    }

    public ICommand ResetOverlayCommand { get; set; }

    public ICommand CreateProfileCommand { get; set; }
    public ICommand CopyProfileCommand { get; set; }
    public ICommand RenameProfileCommand { get; set; }
    public ICommand DeleteProfileCommand { get; set; }
    
    public ICommand SetSRSPathCommand { get; set; }

    /**
         * Global Settings
         */
    
    public bool AutoConnect
    {
        get => _globalSettings.GetClientSettingBool(GlobalSettingsKeys.AutoConnect);
        set
        {
            _globalSettings.SetClientSetting(GlobalSettingsKeys.AutoConnect, value);
            NotifyPropertyChanged();
        }
    }
    
    public bool AutoConnectPrompt
    {
        get => _globalSettings.GetClientSettingBool(GlobalSettingsKeys.AutoConnectPrompt);
        set
        {
            _globalSettings.SetClientSetting(GlobalSettingsKeys.AutoConnectPrompt, value);
            NotifyPropertyChanged();
        }
    }
    
    public bool AutoConnectMismatchPrompt
    {
        get => _globalSettings.GetClientSettingBool(GlobalSettingsKeys.AutoConnectMismatchPrompt);
        set
        {
            _globalSettings.SetClientSetting(GlobalSettingsKeys.AutoConnectMismatchPrompt, value);
            NotifyPropertyChanged();
        }
    }
    
    public bool RadioOverlayTaskbarHide
    {
        get => _globalSettings.GetClientSettingBool(GlobalSettingsKeys.RadioOverlayTaskbarHide);
        set
        {
            _globalSettings.SetClientSetting(GlobalSettingsKeys.RadioOverlayTaskbarHide, value);
            NotifyPropertyChanged();
        }
    }
    
    public bool RefocusDCS
    {
        get => _globalSettings.GetClientSettingBool(GlobalSettingsKeys.RefocusDCS);
        set
        {
            _globalSettings.SetClientSetting(GlobalSettingsKeys.RefocusDCS, value);
            NotifyPropertyChanged();
        }
    }

 

    public bool MinimiseToTray
    {
        get => _globalSettings.GetClientSettingBool(GlobalSettingsKeys.MinimiseToTray);
        set
        {
            _globalSettings.SetClientSetting(GlobalSettingsKeys.MinimiseToTray, value);
            NotifyPropertyChanged();
        }
    }

    public bool StartMinimised
    {
        get => _globalSettings.GetClientSettingBool(GlobalSettingsKeys.StartMinimised);
        set
        {
            _globalSettings.SetClientSetting(GlobalSettingsKeys.StartMinimised, value);
            NotifyPropertyChanged();
        }
    }
    
    public bool ShowTransmitterName
    {
        get => _globalSettings.GetClientSettingBool(GlobalSettingsKeys.ShowTransmitterName);
        set
        {
            _globalSettings.SetClientSetting(GlobalSettingsKeys.ShowTransmitterName, value);
            NotifyPropertyChanged();
        }
    }

    public bool MicAGC
    {
        get => _globalSettings.GetClientSettingBool(GlobalSettingsKeys.AGC);
        set
        {
            _globalSettings.SetClientSetting(GlobalSettingsKeys.AGC, value);
            NotifyPropertyChanged();
        }
    }

    public bool MicDenoise
    {
        get => _globalSettings.GetClientSettingBool(GlobalSettingsKeys.Denoise);
        set
        {
            _globalSettings.SetClientSetting(GlobalSettingsKeys.Denoise, value);
            NotifyPropertyChanged();
        }
    }

    
    public bool VOX
    {
        get => _globalSettings.GetClientSettingBool(GlobalSettingsKeys.VOX);
        set
        {
            _globalSettings.SetClientSetting(GlobalSettingsKeys.VOX, value);
            NotifyPropertyChanged();
        }
    }
    
    public int VOXMinimumTime
    {
        get => _globalSettings.GetClientSettingInt(GlobalSettingsKeys.VOXMinimumTime);
        set
        {
            _globalSettings.SetClientSetting(GlobalSettingsKeys.VOXMinimumTime, value);
            NotifyPropertyChanged();
        }
    }
    
    public int VOXMode
    {
        get => _globalSettings.GetClientSettingInt(GlobalSettingsKeys.VOXMode);
        set
        {
            _globalSettings.SetClientSetting(GlobalSettingsKeys.VOXMode, value);
            NotifyPropertyChanged();
        }
    }
    
    public double VOXMinimumDB
    {
        get => _globalSettings.GetClientSettingDouble(GlobalSettingsKeys.VOXMinimumDB);
        set
        {
            _globalSettings.SetClientSetting(GlobalSettingsKeys.VOXMinimumDB, value);
            NotifyPropertyChanged();
        }
    }
    
    public bool RecordAudio
    {
        get => _globalSettings.GetClientSettingBool(GlobalSettingsKeys.RecordAudio);
        set
        {
            _globalSettings.SetClientSetting(GlobalSettingsKeys.RecordAudio, value);
            NotifyPropertyChanged();
        }
    }
    
    public bool AllowRecording
    {
        get => _globalSettings.GetClientSettingBool(GlobalSettingsKeys.AllowRecording);
        set
        {
            _globalSettings.SetClientSetting(GlobalSettingsKeys.AllowRecording, value);
            NotifyPropertyChanged();
        }
    }
    
    public bool SingleFileMixdown
    {
        get => _globalSettings.GetClientSettingBool(GlobalSettingsKeys.SingleFileMixdown);
        set
        {
            _globalSettings.SetClientSetting(GlobalSettingsKeys.SingleFileMixdown, value);
            NotifyPropertyChanged();
        }
    }
    
    public int RecordingQuality
    {
        get =>  int.Parse(
            _globalSettings.GetClientSetting(GlobalSettingsKeys.RecordingQuality).StringValue[1].ToString());
        set
        {
            _globalSettings.SetClientSetting(GlobalSettingsKeys.RecordingQuality, $"V{(int)value}");
            NotifyPropertyChanged();
        }
    }
    
    public bool DisallowedAudioTone
    {
        get => _globalSettings.GetClientSettingBool(GlobalSettingsKeys.DisallowedAudioTone);
        set
        {
            _globalSettings.SetClientSetting(GlobalSettingsKeys.DisallowedAudioTone, value);
            NotifyPropertyChanged();
        }
    }
    
    public bool AutoSelectSettingsProfile
    {
        get => _globalSettings.GetClientSettingBool(GlobalSettingsKeys.AutoSelectSettingsProfile);
        set
        {
            _globalSettings.SetClientSetting(GlobalSettingsKeys.AutoSelectSettingsProfile, value);
            NotifyPropertyChanged();
        }
    }
    
    public bool CheckForBetaUpdates
    {
        get => _globalSettings.GetClientSettingBool(GlobalSettingsKeys.CheckForBetaUpdates);
        set
        {
            _globalSettings.SetClientSetting(GlobalSettingsKeys.CheckForBetaUpdates, value);
            NotifyPropertyChanged();
        }
    }

    public bool RequireAdmin
    {
        get => _globalSettings.GetClientSettingBool(GlobalSettingsKeys.RequireAdmin);
        set
        {
            _globalSettings.SetClientSetting(GlobalSettingsKeys.RequireAdmin, value);
            NotifyPropertyChanged();
        }
    }
    
    public bool VAICOMTXInhibitEnabled
    {
        get => _globalSettings.GetClientSettingBool(GlobalSettingsKeys.VAICOMTXInhibitEnabled);
        set
        {
            _globalSettings.SetClientSetting(GlobalSettingsKeys.VAICOMTXInhibitEnabled, value);
            NotifyPropertyChanged();
        }
    }
    

    public bool AllowMoreInputs
    {
        get => _globalSettings.GetClientSettingBool(GlobalSettingsKeys.ExpandControls);
        set
        {
            _globalSettings.SetClientSetting(GlobalSettingsKeys.ExpandControls, value);
            NotifyPropertyChanged();
            MessageBox.Show(
                "You must restart SRS for this setting to take effect.\n\nTurning this on will allow almost any DirectX device to be used as input expect a Mouse but may cause issues with other devices being detected. \n\nUse device white listing instead",
                "Restart SRS", MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }
    
    
    public bool AllowXInputController
    {
        get => _globalSettings.GetClientSettingBool(GlobalSettingsKeys.AllowXInputController);
        set
        {
            _globalSettings.SetClientSetting(GlobalSettingsKeys.AllowXInputController, value);
            NotifyPropertyChanged();
            MessageBox.Show(
                "You must restart SRS for this setting to take effect.",
                "Restart SimpleRadio Standalone", MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }
    
    public bool PlayConnectionSounds
    {
        get => _globalSettings.GetClientSettingBool(GlobalSettingsKeys.PlayConnectionSounds);
        set
        {
            _globalSettings.SetClientSetting(GlobalSettingsKeys.PlayConnectionSounds, value);
            NotifyPropertyChanged();
        }
    }
    

    /**
         * Profile Settings
         */

    public bool RadioSwitchIsPTT
    {
        get => _globalSettings.ProfileSettingsStore.GetClientSettingBool(ProfileSettingsKeys.RadioSwitchIsPTT);
        set
        {
            _globalSettings.ProfileSettingsStore.SetClientSettingBool(ProfileSettingsKeys.RadioSwitchIsPTT, value);
            NotifyPropertyChanged();
        }
    }

    public bool AutoSelectChannel
    {
        get => _globalSettings.ProfileSettingsStore.GetClientSettingBool(
            ProfileSettingsKeys.AutoSelectPresetChannel);
        set
        {
            _globalSettings.ProfileSettingsStore.SetClientSettingBool(ProfileSettingsKeys.AutoSelectPresetChannel,
                value);
            NotifyPropertyChanged();
        }
    }
    public bool AlwaysAllowHotasControls
    {
        get => _globalSettings.ProfileSettingsStore.GetClientSettingBool(
            ProfileSettingsKeys.AlwaysAllowHotasControls);
        set
        {
            _globalSettings.ProfileSettingsStore.SetClientSettingBool(ProfileSettingsKeys.AlwaysAllowHotasControls,
                value);
            NotifyPropertyChanged();
        }
    }
    
    public bool AlwaysAllowTransponderOverlay
    {
        get => _globalSettings.ProfileSettingsStore.GetClientSettingBool(
            ProfileSettingsKeys.AlwaysAllowTransponderOverlay);
        set
        {
            _globalSettings.ProfileSettingsStore.SetClientSettingBool(ProfileSettingsKeys.AlwaysAllowTransponderOverlay,
                value);
            NotifyPropertyChanged();
        }
    }
    
    public bool AllowDCSPTT
    {
        get => _globalSettings.ProfileSettingsStore.GetClientSettingBool(
            ProfileSettingsKeys.AllowDCSPTT);
        set
        {
            _globalSettings.ProfileSettingsStore.SetClientSettingBool(ProfileSettingsKeys.AllowDCSPTT,
                value);
            NotifyPropertyChanged();
        }
    }

    public bool RotaryStyleIncrement
    {
        get => _globalSettings.ProfileSettingsStore.GetClientSettingBool(
            ProfileSettingsKeys.RotaryStyleIncrement);
        set
        {
            _globalSettings.ProfileSettingsStore.SetClientSettingBool(ProfileSettingsKeys.RotaryStyleIncrement,
                value);
            NotifyPropertyChanged();
        }
    }

    

    public float PTTReleaseDelay
    {
        get => _globalSettings.ProfileSettingsStore.GetClientSettingFloat(ProfileSettingsKeys.PTTReleaseDelay);
        set
        {
            _globalSettings.ProfileSettingsStore.SetClientSettingFloat(ProfileSettingsKeys.PTTReleaseDelay, value);
            NotifyPropertyChanged();
        }
    }

    public float PTTStartDelay
    {
        get => _globalSettings.ProfileSettingsStore.GetClientSettingFloat(ProfileSettingsKeys.PTTStartDelay);
        set
        {
            _globalSettings.ProfileSettingsStore.SetClientSettingFloat(ProfileSettingsKeys.PTTStartDelay, value);
            NotifyPropertyChanged();
        }
    }

    public bool RadioRxStartToggle
    {
        get => _globalSettings.ProfileSettingsStore.GetClientSettingBool(ProfileSettingsKeys.RadioRxEffects_Start);
        set
        {
            _globalSettings.ProfileSettingsStore.SetClientSettingBool(ProfileSettingsKeys.RadioRxEffects_Start,
                value);
            NotifyPropertyChanged();
        }
    }

    public bool RadioRxEndToggle
    {
        get => _globalSettings.ProfileSettingsStore.GetClientSettingBool(ProfileSettingsKeys.RadioRxEffects_End);
        set
        {
            _globalSettings.ProfileSettingsStore.SetClientSettingBool(ProfileSettingsKeys.RadioRxEffects_End,
                value);
            NotifyPropertyChanged();
        }
    }

    public bool RadioTxStartToggle
    {
        get => _globalSettings.ProfileSettingsStore.GetClientSettingBool(ProfileSettingsKeys.RadioTxEffects_Start);
        set
        {
            _globalSettings.ProfileSettingsStore.SetClientSettingBool(ProfileSettingsKeys.RadioTxEffects_Start,
                value);
            NotifyPropertyChanged();
        }
    }

    public bool RadioTxEndToggle
    {
        get => _globalSettings.ProfileSettingsStore.GetClientSettingBool(ProfileSettingsKeys.RadioRxEffects_End);
        set
        {
            _globalSettings.ProfileSettingsStore.SetClientSettingBool(ProfileSettingsKeys.RadioRxEffects_End,
                value);
            NotifyPropertyChanged();
        }
    }

    public List<CachedAudioEffect> RadioTransmissionStart =>
        CachedAudioEffectProvider.Instance.RadioTransmissionStart;

    public CachedAudioEffect SelectedRadioTransmissionStartEffect
    {
        set
        {
            GlobalSettingsStore.Instance.ProfileSettingsStore.SetClientSettingString(
                ProfileSettingsKeys.RadioTransmissionStartSelection, value.FileName);
            NotifyPropertyChanged();
        }
        get => CachedAudioEffectProvider.Instance.SelectedRadioTransmissionStartEffect;
    }

    public List<CachedAudioEffect> RadioTransmissionEnd => CachedAudioEffectProvider.Instance.RadioTransmissionEnd;

    public CachedAudioEffect SelectedRadioTransmissionEndEffect
    {
        set
        {
            GlobalSettingsStore.Instance.ProfileSettingsStore.SetClientSettingString(
                ProfileSettingsKeys.RadioTransmissionEndSelection, value.FileName);
            NotifyPropertyChanged();
        }
        get => CachedAudioEffectProvider.Instance.SelectedRadioTransmissionEndEffect;
    }
    
    public List<CachedAudioEffect> IntercomTransmissionStart =>
        CachedAudioEffectProvider.Instance.IntercomTransmissionStart;

    public CachedAudioEffect SelectedIntercomTransmissionStartEffect
    {
        set
        {
            GlobalSettingsStore.Instance.ProfileSettingsStore.SetClientSettingString(
                ProfileSettingsKeys.IntercomTransmissionStartSelection, value.FileName);
            NotifyPropertyChanged();
        }
        get => CachedAudioEffectProvider.Instance.SelectedIntercomTransmissionStartEffect;
    }

    public List<CachedAudioEffect> IntercomTransmissionEnd => CachedAudioEffectProvider.Instance.IntercomTransmissionEnd;

    public CachedAudioEffect SelectedIntercomTransmissionEndEffect
    {
        set
        {
            GlobalSettingsStore.Instance.ProfileSettingsStore.SetClientSettingString(
                ProfileSettingsKeys.IntercomTransmissionEndSelection, value.FileName);
            NotifyPropertyChanged();
        }
        get => CachedAudioEffectProvider.Instance.SelectedIntercomTransmissionEndEffect;
    }
    
    public bool RadioEncryptionEffects
    {
        get => _globalSettings.ProfileSettingsStore.GetClientSettingBool(ProfileSettingsKeys.RadioEncryptionEffects);
        set
        {
            _globalSettings.ProfileSettingsStore.SetClientSettingBool(ProfileSettingsKeys.RadioEncryptionEffects, value);
            NotifyPropertyChanged();
        }
    }
    
    public bool MIDSRadioEffect
    {
        get => _globalSettings.ProfileSettingsStore.GetClientSettingBool(ProfileSettingsKeys.MIDSRadioEffect);
        set
        {
            _globalSettings.ProfileSettingsStore.SetClientSettingBool(ProfileSettingsKeys.MIDSRadioEffect, value);
            NotifyPropertyChanged();
        }
    }

    public bool RadioSoundEffectsToggle
    {
        get => _globalSettings.ProfileSettingsStore.GetClientSettingBool(ProfileSettingsKeys.RadioEffects);
        set
        {
            _globalSettings.ProfileSettingsStore.SetClientSettingBool(ProfileSettingsKeys.RadioEffects, value);
            NotifyPropertyChanged();
        }
    }

    public bool RadioEffectsClippingToggle
    {
        get => _globalSettings.ProfileSettingsStore.GetClientSettingBool(ProfileSettingsKeys.RadioEffectsClipping);
        set
        {
            _globalSettings.ProfileSettingsStore.SetClientSettingBool(ProfileSettingsKeys.RadioEffectsClipping,
                value);
            NotifyPropertyChanged();
        }
    }

    public bool FMRadioToneToggle
    {
        get => _globalSettings.ProfileSettingsStore.GetClientSettingBool(ProfileSettingsKeys.NATOTone);
        set
        {
            _globalSettings.ProfileSettingsStore.SetClientSettingBool(ProfileSettingsKeys.NATOTone, value);
            NotifyPropertyChanged();
        }
    }

    public double FMRadioToneVolume
    {
        get => _globalSettings.ProfileSettingsStore.GetClientSettingFloat(ProfileSettingsKeys.NATOToneVolume)
            / double.Parse(
                ProfileSettingsStore.DefaultSettingsProfileSettings[ProfileSettingsKeys.NATOToneVolume.ToString()],
                CultureInfo.InvariantCulture) * 100;
        set
        {
            var orig = double.Parse(
                ProfileSettingsStore.DefaultSettingsProfileSettings[ProfileSettingsKeys.NATOToneVolume.ToString()],
                CultureInfo.InvariantCulture);
            var vol = orig * (value / 100);

            _globalSettings.ProfileSettingsStore.SetClientSettingFloat(ProfileSettingsKeys.NATOToneVolume,
                (float)vol);
            NotifyPropertyChanged();
        }
    }
    
    public bool HAVEQUICKTone
    {
        get => _globalSettings.ProfileSettingsStore.GetClientSettingBool(ProfileSettingsKeys.HAVEQUICKTone);
        set
        {
            _globalSettings.ProfileSettingsStore.SetClientSettingBool(ProfileSettingsKeys.HAVEQUICKTone, value);
            NotifyPropertyChanged();
        }
    }

    public double HQToneVolume
    {
        get => _globalSettings.ProfileSettingsStore.GetClientSettingFloat(ProfileSettingsKeys.HQToneVolume)
            / double.Parse(
                ProfileSettingsStore.DefaultSettingsProfileSettings[ProfileSettingsKeys.HQToneVolume.ToString()],
                CultureInfo.InvariantCulture) * 100;
        set
        {
            var orig = double.Parse(
                ProfileSettingsStore.DefaultSettingsProfileSettings[ProfileSettingsKeys.HQToneVolume.ToString()],
                CultureInfo.InvariantCulture);
            var vol = orig * (value / 100);

            _globalSettings.ProfileSettingsStore.SetClientSettingFloat(ProfileSettingsKeys.HQToneVolume,
                (float)vol);
            NotifyPropertyChanged();
        }
    }

    public bool BackgroundRadioNoiseToggle
    {
        get => _globalSettings.ProfileSettingsStore.GetClientSettingBool(ProfileSettingsKeys
            .RadioBackgroundNoiseEffect);
        set
        {
            _globalSettings.ProfileSettingsStore.SetClientSettingBool(
                ProfileSettingsKeys.RadioBackgroundNoiseEffect, value);
            NotifyPropertyChanged();
        }
    }

    public double UHFEffectVolume
    {
        get => _globalSettings.ProfileSettingsStore.GetClientSettingFloat(ProfileSettingsKeys.UHFNoiseVolume)
            / double.Parse(
                ProfileSettingsStore.DefaultSettingsProfileSettings[ProfileSettingsKeys.UHFNoiseVolume.ToString()],
                CultureInfo.InvariantCulture) * 100;
        set
        {
            var orig = double.Parse(
                ProfileSettingsStore.DefaultSettingsProfileSettings[ProfileSettingsKeys.UHFNoiseVolume.ToString()],
                CultureInfo.InvariantCulture);
            var vol = orig * (value / 100);

            _globalSettings.ProfileSettingsStore.SetClientSettingFloat(ProfileSettingsKeys.UHFNoiseVolume,
                (float)vol);
            NotifyPropertyChanged();
        }
    }

    public double VHFEffectVolume
    {
        get => _globalSettings.ProfileSettingsStore.GetClientSettingFloat(ProfileSettingsKeys.VHFNoiseVolume)
            / double.Parse(
                ProfileSettingsStore.DefaultSettingsProfileSettings[ProfileSettingsKeys.VHFNoiseVolume.ToString()],
                CultureInfo.InvariantCulture) * 100;
        set
        {
            var orig = double.Parse(
                ProfileSettingsStore.DefaultSettingsProfileSettings[ProfileSettingsKeys.VHFNoiseVolume.ToString()],
                CultureInfo.InvariantCulture);
            var vol = orig * (value / 100);

            _globalSettings.ProfileSettingsStore.SetClientSettingFloat(ProfileSettingsKeys.VHFNoiseVolume,
                (float)vol);
            NotifyPropertyChanged();
        }
    }

    public double HFEffectVolume
    {
        get => _globalSettings.ProfileSettingsStore.GetClientSettingFloat(ProfileSettingsKeys.HFNoiseVolume)
            / double.Parse(
                ProfileSettingsStore.DefaultSettingsProfileSettings[ProfileSettingsKeys.HFNoiseVolume.ToString()],
                CultureInfo.InvariantCulture) * 100;
        set
        {
            var orig = double.Parse(
                ProfileSettingsStore.DefaultSettingsProfileSettings[ProfileSettingsKeys.HFNoiseVolume.ToString()],
                CultureInfo.InvariantCulture);
            var vol = orig * (value / 100);

            _globalSettings.ProfileSettingsStore.SetClientSettingFloat(ProfileSettingsKeys.HFNoiseVolume,
                (float)vol);
            NotifyPropertyChanged();
        }
    }

    public double FMEffectVolume
    {
        get => _globalSettings.ProfileSettingsStore.GetClientSettingFloat(ProfileSettingsKeys.FMNoiseVolume)
            / double.Parse(
                ProfileSettingsStore.DefaultSettingsProfileSettings[ProfileSettingsKeys.FMNoiseVolume.ToString()],
                CultureInfo.InvariantCulture) * 100;
        set
        {
            var orig = double.Parse(
                ProfileSettingsStore.DefaultSettingsProfileSettings[ProfileSettingsKeys.FMNoiseVolume.ToString()],
                CultureInfo.InvariantCulture);
            var vol = orig * (value / 100);

            _globalSettings.ProfileSettingsStore.SetClientSettingFloat(ProfileSettingsKeys.FMNoiseVolume,
                (float)vol);
            NotifyPropertyChanged();
        }
    }


    /**
         * Radio Audio Balance
         */

    public float RadioChannel1
    {
        get => _globalSettings.ProfileSettingsStore.GetClientSettingFloat(ProfileSettingsKeys.Radio1Channel);
        set
        {
            _globalSettings.ProfileSettingsStore.SetClientSettingFloat(ProfileSettingsKeys.Radio1Channel, value);
            NotifyPropertyChanged();
        }
    }

    public float RadioChannel2
    {
        get => _globalSettings.ProfileSettingsStore.GetClientSettingFloat(ProfileSettingsKeys.Radio2Channel);
        set
        {
            _globalSettings.ProfileSettingsStore.SetClientSettingFloat(ProfileSettingsKeys.Radio2Channel, value);
            NotifyPropertyChanged();
        }
    }

    public float RadioChannel3
    {
        get => _globalSettings.ProfileSettingsStore.GetClientSettingFloat(ProfileSettingsKeys.Radio3Channel);
        set
        {
            _globalSettings.ProfileSettingsStore.SetClientSettingFloat(ProfileSettingsKeys.Radio3Channel, value);
            NotifyPropertyChanged();
        }
    }

    public float RadioChannel4
    {
        get => _globalSettings.ProfileSettingsStore.GetClientSettingFloat(ProfileSettingsKeys.Radio4Channel);
        set
        {
            _globalSettings.ProfileSettingsStore.SetClientSettingFloat(ProfileSettingsKeys.Radio4Channel, value);
            NotifyPropertyChanged();
        }
    }

    public float RadioChannel5
    {
        get => _globalSettings.ProfileSettingsStore.GetClientSettingFloat(ProfileSettingsKeys.Radio5Channel);
        set
        {
            _globalSettings.ProfileSettingsStore.SetClientSettingFloat(ProfileSettingsKeys.Radio5Channel, value);
            NotifyPropertyChanged();
        }
    }

    public float RadioChannel6
    {
        get => _globalSettings.ProfileSettingsStore.GetClientSettingFloat(ProfileSettingsKeys.Radio6Channel);
        set
        {
            _globalSettings.ProfileSettingsStore.SetClientSettingFloat(ProfileSettingsKeys.Radio6Channel, value);
            NotifyPropertyChanged();
        }
    }

    public float RadioChannel7
    {
        get => _globalSettings.ProfileSettingsStore.GetClientSettingFloat(ProfileSettingsKeys.Radio7Channel);
        set
        {
            _globalSettings.ProfileSettingsStore.SetClientSettingFloat(ProfileSettingsKeys.Radio7Channel, value);
            NotifyPropertyChanged();
        }
    }

    public float RadioChannel8
    {
        get => _globalSettings.ProfileSettingsStore.GetClientSettingFloat(ProfileSettingsKeys.Radio8Channel);
        set
        {
            _globalSettings.ProfileSettingsStore.SetClientSettingFloat(ProfileSettingsKeys.Radio8Channel, value);
            NotifyPropertyChanged();
        }
    }

    public float RadioChannel9
    {
        get => _globalSettings.ProfileSettingsStore.GetClientSettingFloat(ProfileSettingsKeys.Radio9Channel);
        set
        {
            _globalSettings.ProfileSettingsStore.SetClientSettingFloat(ProfileSettingsKeys.Radio9Channel, value);
            NotifyPropertyChanged();
        }
    }

    public float RadioChannel10
    {
        get => _globalSettings.ProfileSettingsStore.GetClientSettingFloat(ProfileSettingsKeys.Radio10Channel);
        set
        {
            _globalSettings.ProfileSettingsStore.SetClientSettingFloat(ProfileSettingsKeys.Radio10Channel, value);
            NotifyPropertyChanged();
        }
    }

    public float Intercom
    {
        get => _globalSettings.ProfileSettingsStore.GetClientSettingFloat(ProfileSettingsKeys.IntercomChannel);
        set
        {
            _globalSettings.ProfileSettingsStore.SetClientSettingFloat(ProfileSettingsKeys.IntercomChannel, value);
            NotifyPropertyChanged();
        }
    }

    public string SelectedProfile
    {
        set
        {
            if (value != null)
            {
                _globalSettings.ProfileSettingsStore.CurrentProfileName = value;
                //TODO send event notifying of change to current profile
                ReloadSettings();
                // EventBus.Instance.PublishOnUIThreadAsync(new ProfileChangedMessage());
            }

            NotifyPropertyChanged();
        }
        get => _globalSettings.ProfileSettingsStore.CurrentProfileName;
    }

    public List<string> AvailableProfiles
    {
        set
        {
            //do nothing
        }
        get => _globalSettings.ProfileSettingsStore.ProfileNames;
    }

    public string PlayerName
    {
        set
        {
            if (value != null) ClientStateSingleton.Instance.DcsPlayerRadioInfo.name = value;
        }
        get => ClientStateSingleton.Instance.DcsPlayerRadioInfo.name;
    }

    public uint PlayerID
    {
        set
        {
            if (value != null) ClientStateSingleton.Instance.DcsPlayerRadioInfo.unitId = value;
        }
        get => ClientStateSingleton.Instance.DcsPlayerRadioInfo.unitId;
    }

    private void ReloadSettings()
    {
        //Autoconnect
        NotifyPropertyChanged(nameof(AutoConnect));
        NotifyPropertyChanged(nameof(AutoConnectPrompt));
        NotifyPropertyChanged(nameof(AutoConnectMismatchPrompt));
        
        //SRS interface
        NotifyPropertyChanged(nameof(RadioOverlayTaskbarHide));
        NotifyPropertyChanged(nameof(RefocusDCS));
        NotifyPropertyChanged(nameof(MinimiseToTray));
        NotifyPropertyChanged(nameof(StartMinimised));
        NotifyPropertyChanged(nameof(ShowTransmitterName));
        
        //Microphone
        NotifyPropertyChanged(nameof(MicAGC));
        NotifyPropertyChanged(nameof(MicDenoise));
        
        //Intercom Hot Mic Voice Detection
        NotifyPropertyChanged(nameof(VOX));
        NotifyPropertyChanged(nameof(VOXMinimumTime));
        NotifyPropertyChanged(nameof(VOXMode));
        NotifyPropertyChanged(nameof(VOXMinimumDB));
        
        //Recording
        NotifyPropertyChanged(nameof(AllowRecording));
        NotifyPropertyChanged(nameof(RecordAudio));
        NotifyPropertyChanged(nameof(SingleFileMixdown));
        NotifyPropertyChanged(nameof(RecordingQuality));
        NotifyPropertyChanged(nameof(DisallowedAudioTone));
        
        //Miscellaneous Settings
        NotifyPropertyChanged(nameof(AutoSelectSettingsProfile));
        NotifyPropertyChanged(nameof(CheckForBetaUpdates));
        NotifyPropertyChanged(nameof(RequireAdmin));
        NotifyPropertyChanged(nameof(VAICOMTXInhibitEnabled));
        NotifyPropertyChanged(nameof(AllowMoreInputs));
        NotifyPropertyChanged(nameof(AllowXInputController));
        NotifyPropertyChanged(nameof(PlayConnectionSounds));

        //Controls / Cockpit Integration
        NotifyPropertyChanged(nameof(RadioSwitchIsPTT));
        NotifyPropertyChanged(nameof(AutoSelectChannel));
        NotifyPropertyChanged(nameof(AlwaysAllowHotasControls));
        NotifyPropertyChanged(nameof(AlwaysAllowTransponderOverlay));
        NotifyPropertyChanged(nameof(AllowDCSPTT));
        NotifyPropertyChanged(nameof(RotaryStyleIncrement));
        NotifyPropertyChanged(nameof(PTTReleaseDelay));
        NotifyPropertyChanged(nameof(PTTStartDelay));
        
        //Profile
        NotifyPropertyChanged(nameof(SelectedProfile));
        
        //Radio Effect Settings
        NotifyPropertyChanged(nameof(RadioRxStartToggle));
        NotifyPropertyChanged(nameof(RadioRxEndToggle));
        NotifyPropertyChanged(nameof(RadioTxStartToggle));
        NotifyPropertyChanged(nameof(RadioTxEndToggle));
        NotifyPropertyChanged(nameof(SelectedRadioTransmissionStartEffect));
        NotifyPropertyChanged(nameof(SelectedRadioTransmissionEndEffect));
        NotifyPropertyChanged(nameof(SelectedIntercomTransmissionStartEffect));
        NotifyPropertyChanged(nameof(SelectedIntercomTransmissionEndEffect));
        NotifyPropertyChanged(nameof(RadioEncryptionEffects));
        NotifyPropertyChanged(nameof(MIDSRadioEffect));
        NotifyPropertyChanged(nameof(RadioSoundEffectsToggle));
        NotifyPropertyChanged(nameof(RadioEffectsClippingToggle));
        NotifyPropertyChanged(nameof(FMRadioToneToggle));
        NotifyPropertyChanged(nameof(FMRadioToneVolume));
        NotifyPropertyChanged(nameof(HAVEQUICKTone));
        NotifyPropertyChanged(nameof(HQToneVolume));
        NotifyPropertyChanged(nameof(BackgroundRadioNoiseToggle));
        NotifyPropertyChanged(nameof(UHFEffectVolume));
        NotifyPropertyChanged(nameof(VHFEffectVolume));
        NotifyPropertyChanged(nameof(HFEffectVolume));
        NotifyPropertyChanged(nameof(FMEffectVolume));
        
        //Audio
        NotifyPropertyChanged(nameof(RadioChannel1));
        NotifyPropertyChanged(nameof(RadioChannel2));
        NotifyPropertyChanged(nameof(RadioChannel3));
        NotifyPropertyChanged(nameof(RadioChannel4));
        NotifyPropertyChanged(nameof(RadioChannel5));
        NotifyPropertyChanged(nameof(RadioChannel6));
        NotifyPropertyChanged(nameof(RadioChannel7));
        NotifyPropertyChanged(nameof(RadioChannel8));
        NotifyPropertyChanged(nameof(RadioChannel9));
        NotifyPropertyChanged(nameof(RadioChannel10));
        NotifyPropertyChanged(nameof(Intercom));
        

        //TODO send message to tell input to reload!
    }
}