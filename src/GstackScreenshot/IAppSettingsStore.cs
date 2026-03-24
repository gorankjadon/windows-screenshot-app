namespace GstackScreenshot
{
    internal interface IAppSettingsStore
    {
        AppSettings Load();
        void Save(AppSettings settings);
    }
}
