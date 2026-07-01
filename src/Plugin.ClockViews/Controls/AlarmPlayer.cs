using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;
using Plugin.Maui.Audio;

namespace Plugin.ClockViews;

/// <summary>
/// Plays a countdown alarm for a fixed duration. The audio comes from a user-supplied
/// <see cref="AlarmSource"/> (resources / file / URL); when none is given a generated beep is
/// used. A single shared player is kept so simultaneous requests don't stack.
/// </summary>
static class AlarmPlayer
{
    static IAudioPlayer? _current;

    public static async void Play(AlarmSource? source, TimeSpan duration)
    {
        try
        {
            Stream stream = await OpenAsync(source).ConfigureAwait(false);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    _current?.Dispose();
                    _current = AudioManager.Current.CreatePlayer(stream);
                    _current.Loop = true; // repeat short sounds until we stop it
                    _current.Play();

                    var player = _current;
                    _ = StopAfterAsync(player, duration);
                }
                catch
                {
                    // Audio unavailable on this platform/run — ignore.
                }
            });
        }
        catch
        {
            // Could not load the requested source — ignore.
        }
    }

    static async Task StopAfterAsync(IAudioPlayer player, TimeSpan duration)
    {
        await Task.Delay(duration).ConfigureAwait(false);
        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                player.Stop();
                player.Dispose();
            }
            catch
            {
                // Already stopped/disposed — ignore.
            }
        });
    }

    static async Task<Stream> OpenAsync(AlarmSource? source)
    {
        if (source is null)
            return GenerateBeep(5);

        switch (source.Kind)
        {
            case AlarmSourceKind.Resources:
                return await FileSystem.OpenAppPackageFileAsync(source.Value).ConfigureAwait(false);

            case AlarmSourceKind.File:
                return File.OpenRead(source.Value);

            case AlarmSourceKind.Url:
                using (var http = new HttpClient())
                {
                    byte[] bytes = await http.GetByteArrayAsync(source.Value).ConfigureAwait(false);
                    return new MemoryStream(bytes);
                }

            default:
                return GenerateBeep(5);
        }
    }

    // Builds an in-memory WAV of a repeating beep, used when no AlarmSource is supplied.
    static MemoryStream GenerateBeep(int seconds)
    {
        const int sampleRate = 44100;
        const double toneHz = 1000;
        int totalSamples = sampleRate * seconds;

        var pcm = new byte[totalSamples * 2];
        for (int i = 0; i < totalSamples; i++)
        {
            // 0.25s tone, 0.25s silence.
            bool on = (i / (sampleRate / 4)) % 2 == 0;
            short sample = 0;
            if (on)
                sample = (short)(Math.Sin(2 * Math.PI * toneHz * i / sampleRate) * short.MaxValue * 0.5);
            pcm[i * 2] = (byte)(sample & 0xFF);
            pcm[i * 2 + 1] = (byte)((sample >> 8) & 0xFF);
        }

        var ms = new MemoryStream();
        WriteWavHeader(ms, pcm.Length, sampleRate);
        ms.Write(pcm, 0, pcm.Length);
        ms.Position = 0;
        return ms;
    }

    static void WriteWavHeader(Stream s, int dataLength, int sampleRate)
    {
        void Str(string v) => s.Write(System.Text.Encoding.ASCII.GetBytes(v), 0, v.Length);
        void Int(int v) => s.Write(BitConverter.GetBytes(v), 0, 4);
        void Short(short v) => s.Write(BitConverter.GetBytes(v), 0, 2);

        Str("RIFF");
        Int(36 + dataLength);
        Str("WAVE");
        Str("fmt ");
        Int(16);            // PCM chunk size
        Short(1);           // PCM
        Short(1);           // mono
        Int(sampleRate);
        Int(sampleRate * 2); // byte rate (mono, 16-bit)
        Short(2);           // block align
        Short(16);          // bits per sample
        Str("data");
        Int(dataLength);
    }
}
