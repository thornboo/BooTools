using System;
using System.Collections.Generic;

namespace BooTools.Plugins.WallpaperSwitcher
{
    public class WallpaperConfig
    {
        public bool Enabled { get; set; } = true;
        public int Interval { get; set; } = 300; // 秒
        public string Mode { get; set; } = "random"; // random, sequential
        public string WallpaperDirectory { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        public List<string> FileExtensions { get; set; } = new() { ".jpg", ".jpeg", ".png", ".bmp" };
    }
}
