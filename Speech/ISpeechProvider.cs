namespace MonsterPromAccessibility.Speech
{
    public interface ISpeechProvider
    {
        string Name { get; }
        bool IsAvailable();
        void Initialize();
        void Speak(string text, bool interrupt = true);
        void Stop();
        void Dispose();
    }
}
