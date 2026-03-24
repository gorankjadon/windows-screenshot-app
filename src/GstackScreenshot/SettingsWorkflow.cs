using System;

namespace GstackScreenshot
{
    internal sealed class SettingsWorkflow
    {
        private readonly IAppSettingsStore _settingsStore;
        private readonly IHotkeyRegistrationService _hotkeyRegistrationService;

        public SettingsWorkflow(IAppSettingsStore settingsStore, IHotkeyRegistrationService hotkeyRegistrationService)
        {
            if (settingsStore == null)
            {
                throw new ArgumentNullException("settingsStore");
            }

            if (hotkeyRegistrationService == null)
            {
                throw new ArgumentNullException("hotkeyRegistrationService");
            }

            _settingsStore = settingsStore;
            _hotkeyRegistrationService = hotkeyRegistrationService;
        }

        public AppSettings Apply(AppSettings currentSettings, AppSettings nextSettings)
        {
            _hotkeyRegistrationService.Register(nextSettings);

            try
            {
                _settingsStore.Save(nextSettings);
                return nextSettings;
            }
            catch (Exception saveException)
            {
                try
                {
                    _hotkeyRegistrationService.Register(currentSettings);
                }
                catch (Exception restoreException)
                {
                    throw new InvalidOperationException(
                        "Saving settings failed and previous hotkeys could not be restored: " + restoreException.Message,
                        saveException);
                }

                throw new InvalidOperationException(
                    "Saving settings failed. Previous hotkeys were restored. " + saveException.Message,
                    saveException);
            }
        }
    }
}
