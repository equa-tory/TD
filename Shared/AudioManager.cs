using System;
using System.Media;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Tokenizer.Shared
{
    public static class AudioManager
    {
        [DllImport("winmm.dll")]
        private static extern int waveOutSetVolume(IntPtr hwo, uint dwVolume);

        [DllImport("winmm.dll")]
        private static extern int waveOutGetVolume(IntPtr hwo, out uint dwVolume);

        // volume 0-100
        public static void SetVolume(int percent)
        {
            if (percent < 0)   percent = 0;
            if (percent > 100) percent = 100;

            // convert 0-100 to 0-65535 for each channel (left | right)
            uint val = (uint)(percent * 65535 / 100);
            uint volume = (val & 0xffff) | ((val & 0xffff) << 16);
            waveOutSetVolume(IntPtr.Zero, volume);
        }

        public static int GetVolume()
        {
            uint vol;
            waveOutGetVolume(IntPtr.Zero, out vol);
            uint left = vol & 0xffff;
            return (int)(left * 100 / 65535);
        }

        public static void PlaySync(string path)
        {
            if (!System.IO.File.Exists(path)) return;
            using (SoundPlayer player = new SoundPlayer(path))
            {
                player.PlaySync();
            }
        }
    }
}