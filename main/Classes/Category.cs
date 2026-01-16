using client.User_controls;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace client.Classes
{
    public class Category
    {
        public string Name;
        public string ColorString = System.Drawing.ColorTranslator.ToHtml(Color.FromArgb(31, 31, 31));
        public bool allowOpenAll = false;
        public List<ProgramShortcut> ShortcutList;
        public int Width; // not used aon
        public double Opacity = 10;
        Regex specialCharRegex = new Regex("[*'\",_&#^@]");

        private static int[] iconSizes = new int[] { 16, 32, 64, 128, 256, 512 };

        public Category(string path)
        {
            string fullPath;

            if (System.IO.File.Exists(@MainPath.path + @"\" + path + @"\ObjectData.xml"))
            {
                fullPath = @MainPath.path + @"\" + path + @"\ObjectData.xml";
            }
            else
            {
                fullPath = Path.GetFullPath(path + "\\ObjectData.xml");
            }

            System.Xml.Serialization.XmlSerializer reader =
                new System.Xml.Serialization.XmlSerializer(typeof(Category));
            using (StreamReader file = new StreamReader(fullPath))
            {
                Category category = (Category)reader.Deserialize(file);
                this.Name = category.Name;
                this.ShortcutList = category.ShortcutList;
                this.Width = category.Width;
                this.ColorString = category.ColorString;
                this.Opacity = category.Opacity;
                this.allowOpenAll = category.allowOpenAll;
            }
        }

        public Category() // needed for XML serialization
        {
        }

        public void CreateConfig(Image groupImage)
        {
            string path = @"config\" + this.Name;

            System.IO.Directory.CreateDirectory(@path);

            System.Xml.Serialization.XmlSerializer writer =
                new System.Xml.Serialization.XmlSerializer(typeof(Category));

            using (FileStream file = System.IO.File.Create(@path + @"\ObjectData.xml"))
            {
                writer.Serialize(file, this);
                file.Close();
            }

            Image img = ImageFunctions.ResizeImage(groupImage, 256, 256);
            img.Save(path + @"\GroupImage.png");

            if (GetMimeType(groupImage).ToString() == "*.PNG")
            {
                createMultiIcon(groupImage, path + @"\GroupIcon.ico");
            }
            else
            {
                using (FileStream fs = new FileStream(path + @"\GroupIcon.ico", FileMode.Create))
                {
                    ImageFunctions.IconFromImage(img).Save(fs);
                    fs.Close();
                }
            }

            ShellLink.InstallShortcut(
                Path.GetFullPath(@System.AppDomain.CurrentDomain.FriendlyName),
                "tjackenpacken.taskbarGroup.menu." + this.Name,
                 path + " shortcut",
                 Path.GetFullPath(@path),
                 Path.GetFullPath(path + @"\GroupIcon.ico"),
                 path + "\\" + this.Name + ".lnk",
                 this.Name
            );

            cacheIcons();

            System.IO.File.Move(@path + "\\" + this.Name + ".lnk",
                Path.GetFullPath(@"Shortcuts\" + Regex.Replace(this.Name, @"(_)+", " ") + ".lnk"));
        }

        private static void createMultiIcon(Image iconImage, string filePath)
        {
            var diffList = from number in iconSizes
                           select new
                           {
                               number,
                               difference = Math.Abs(number - iconImage.Height)
                           };
            var nearestSize = (from diffItem in diffList
                               orderby diffItem.difference
                               select diffItem).First().number;

            List<Bitmap> iconList = new List<Bitmap>();

            while (nearestSize != 16)
            {
                iconList.Add(ImageFunctions.ResizeImage(iconImage, nearestSize, nearestSize));
                nearestSize = (int)Math.Round((decimal)nearestSize / 2);
            }

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                IconFactory.SavePngsAsIcon(iconList.ToArray(), stream);
            }
        }

        public Bitmap LoadIconImage()
        {
            string path = @"config\" + Name + @"\GroupImage.png";

            using (MemoryStream ms = new MemoryStream(System.IO.File.ReadAllBytes(path)))
                return new Bitmap(ms);
        }

        private string GetIconsFolderAbsolutePath()
        {
            return Path.Combine(MainPath.path, "config", this.Name, "Icons");
        }

        private string GetCacheIconPathAbsolute(ProgramShortcut shortcutObject)
        {
            string iconsFolder = GetIconsFolderAbsolutePath();
            string programPath = shortcutObject.FilePath;

            // Must match cacheIcons() naming exactly
            string baseName;
            if (shortcutObject.isWindowsApp)
                baseName = specialCharRegex.Replace(programPath, string.Empty);
            else
                baseName = Path.GetFileNameWithoutExtension(programPath);

            string suffix = Directory.Exists(programPath) ? "_FolderObjTSKGRoup.png" : ".png";
            return Path.Combine(iconsFolder, baseName + suffix);
        }

        private string ResolveCustomIconPathAbsolute(ProgramShortcut shortcutObject)
        {
            string p = shortcutObject.CustomIconPath ?? string.Empty;
            if (string.IsNullOrWhiteSpace(p))
                return string.Empty;

            // If stored relative, resolve under config/<GroupName>/
            if (!Path.IsPathRooted(p))
                p = Path.Combine(MainPath.path, "config", this.Name, p);

            return p;
        }

        // Goal is to create a folder with icons of the programs pre-cached and ready to be read
        public void cacheIcons()
        {
            string groupConfigPath = Path.Combine(MainPath.path, "config", this.Name);
            string iconFolder = Path.Combine(groupConfigPath, "Icons");

            if (Directory.Exists(iconFolder))
            {
                Directory.Delete(iconFolder, true);
            }
            Directory.CreateDirectory(iconFolder);

            for (int i = 0; i < ShortcutList.Count; i++)
            {
                ProgramShortcut sc = ShortcutList[i];
                string savePath = GetCacheIconPathAbsolute(sc);

                // Prefer custom icon if present (so cache matches what UI shows)
                Image custom = null;
                try
                {
                    string customAbs = ResolveCustomIconPathAbsolute(sc);
                    if (!string.IsNullOrEmpty(customAbs) && File.Exists(customAbs))
                    {
                        using (var ms = new MemoryStream(File.ReadAllBytes(customAbs)))
                        using (var img = Image.FromStream(ms))
                        {
                            custom = new Bitmap(img);
                        }
                    }

                    if (custom != null)
                    {
                        using (custom)
                        {
                            custom.Save(savePath);
                        }
                        continue;
                    }

                    // Fallback to whatever UI resolved (existing behavior)
                    ucProgramShortcut programShortcutControl =
                        Application.OpenForms["frmGroup"].Controls["pnlShortcuts"].Controls[i] as ucProgramShortcut;

                    // logo is likely a Bitmap; saving is fine, but ensure null safety
                    if (programShortcutControl != null && programShortcutControl.logo != null)
                    {
                        programShortcutControl.logo.Save(savePath);
                    }
                    else
                    {
                        // Worst-case fallback
                        using (var bmp = global::client.Properties.Resources.Error)
                        {
                            bmp.Save(savePath);
                        }
                    }
                }
                catch
                {
                    // avoid crashing cache build on a single icon failure
                }
            }
        }

        // Try to load an image from the cache
        public Image loadImageCache(ProgramShortcut shortcutObject)
        {
            string programPath = shortcutObject.FilePath;

            // 1) Custom icon first (absolute OR relative supported)
            if (!string.IsNullOrEmpty(shortcutObject.CustomIconPath))
            {
                try
                {
                    string customAbs = ResolveCustomIconPathAbsolute(shortcutObject);
                    if (!string.IsNullOrEmpty(customAbs) && File.Exists(customAbs))
                    {
                        using (MemoryStream ms = new MemoryStream(File.ReadAllBytes(customAbs)))
                        using (Image tempImage = Image.FromStream(ms))
                        {
                            return new Bitmap(tempImage);
                        }
                    }
                }
                catch
                {
                    // Fall through to cache/default icon loading
                }
            }

            // 2) Cache / default behavior
            if (System.IO.File.Exists(programPath) || Directory.Exists(programPath) || shortcutObject.isWindowsApp)
            {
                // 2a) Try cache
                try
                {
                    string cacheImagePath = GetCacheIconPathAbsolute(shortcutObject);
                    using (MemoryStream ms = new MemoryStream(System.IO.File.ReadAllBytes(cacheImagePath)))
                    using (Image img = Image.FromStream(ms))
                    {
                        return new Bitmap(img);
                    }
                }
                catch
                {
                    // 2b) Recreate cache entry
                    try
                    {
                        string savePath = GetCacheIconPathAbsolute(shortcutObject);

                        Image extracted;
                        if (Path.GetExtension(programPath).ToLower() == ".lnk")
                        {
                            extracted = Forms.frmGroup.handleLnkExt(programPath);
                        }
                        else if (Directory.Exists(programPath))
                        {
                            extracted = handleFolder.GetFolderIcon(programPath).ToBitmap();
                        }
                        else
                        {
                            extracted = Icon.ExtractAssociatedIcon(programPath).ToBitmap();
                        }

                        using (extracted)
                        {
                            extracted.Save(savePath);
                            return new Bitmap(extracted);
                        }
                    }
                    catch
                    {
                        return global::client.Properties.Resources.Error;
                    }
                }
            }

            return global::client.Properties.Resources.Error;
        }

        public static string GetMimeType(Image i)
        {
            var imgguid = i.RawFormat.Guid;
            foreach (ImageCodecInfo codec in ImageCodecInfo.GetImageDecoders())
            {
                if (codec.FormatID == imgguid)
                    return codec.FilenameExtension;
            }
            return "image/unknown";
        }
    }

    // NOTE: The original file used ImageCodecInfo.GetImageDecoders().
    // If your project doesn't have ImageInfo, replace ImageInfo with ImageCodecInfo.
}
