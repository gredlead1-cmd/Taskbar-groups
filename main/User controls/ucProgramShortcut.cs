using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using client.Classes;
using client.Forms;

namespace client.User_controls
{
    public partial class ucProgramShortcut : UserControl
    {
        public ProgramShortcut Shortcut { get; set; }
        public frmGroup MotherForm { get; set; }

        public bool IsSelected = false;
        public int Position { get; set; }

        // Current icon bitmap owned by this control (must be disposed when replaced)
        public Bitmap logo;

        public ucProgramShortcut()
        {
            InitializeComponent();
        }

        private void ucProgramShortcut_Load(object sender, EventArgs e)
        {
            if (Shortcut.isWindowsApp)
            {
                txtShortcutName.Text = handleWindowsApp.findWindowsAppsName(Shortcut.FilePath);
            }
            else if (Shortcut.name == "")
            {
                if (File.Exists(Shortcut.FilePath) && Path.GetExtension(Shortcut.FilePath).ToLower() == ".lnk")
                {
                    txtShortcutName.Text = frmGroup.handleExtName(Shortcut.FilePath);
                }
                else
                {
                    txtShortcutName.Text = Path.GetFileNameWithoutExtension(Shortcut.FilePath);
                }
            }
            else
            {
                txtShortcutName.Text = Shortcut.name;
            }

            Size size = TextRenderer.MeasureText(txtShortcutName.Text, txtShortcutName.Font);
            txtShortcutName.Width = size.Width;
            txtShortcutName.Height = size.Height;

            // Load icon (custom or default)
            LoadIconImage();

            if (Position == 0)
            {
                cmdNumUp.Enabled = false;
                cmdNumUp.BackgroundImage = global::client.Properties.Resources.NumUpGray;
            }
            if (Position == MotherForm.Category.ShortcutList.Count - 1)
            {
                cmdNumDown.Enabled = false;
                cmdNumDown.BackgroundImage = global::client.Properties.Resources.NumDownGray;
            }
        }

        private void ucProgramShortcut_MouseEnter(object sender, EventArgs e)
        {
            ucSelected();
        }

        private void ucProgramShortcut_MouseLeave(object sender, EventArgs e)
        {
            if (MotherForm.selectedShortcut != this)
            {
                ucDeselected();
            }
        }

        private void cmdNumUp_Click(object sender, EventArgs e)
        {
            MotherForm.Swap(MotherForm.Category.ShortcutList, Position, Position - 1);
        }

        private void cmdNumDown_Click(object sender, EventArgs e)
        {
            MotherForm.Swap(MotherForm.Category.ShortcutList, Position, Position + 1);
        }

        private void cmdDelete_Click(object sender, EventArgs e)
        {
            MotherForm.DeleteShortcut(Shortcut);
        }

        private void ucProgramShortcut_Click(object sender, EventArgs e)
        {
            if (MotherForm.selectedShortcut == this)
            {
                MotherForm.resetSelection();
            }
            else
            {
                if (MotherForm.selectedShortcut != null)
                {
                    MotherForm.resetSelection();
                }

                MotherForm.enableSelection(this);
            }
        }

        public void ucDeselected()
        {
            txtShortcutName.DeselectAll();
            txtShortcutName.Enabled = false;
            txtShortcutName.Enabled = true;
            txtShortcutName.TabStop = false;

            this.BackColor = Color.FromArgb(31, 31, 31);
            txtShortcutName.BackColor = Color.FromArgb(31, 31, 31);
            cmdNumUp.BackColor = Color.FromArgb(31, 31, 31);
            cmdNumDown.BackColor = Color.FromArgb(31, 31, 31);
        }

        public void ucSelected()
        {
            this.BackColor = Color.FromArgb(26, 26, 26);
            txtShortcutName.BackColor = Color.FromArgb(26, 26, 26);
            cmdNumUp.BackColor = Color.FromArgb(26, 26, 26);
            cmdNumDown.BackColor = Color.FromArgb(26, 26, 26);
        }

        private void lbTextbox_TextChanged(object sender, EventArgs e)
        {
            Size size = TextRenderer.MeasureText(txtShortcutName.Text, txtShortcutName.Font);
            txtShortcutName.Width = size.Width;
            txtShortcutName.Height = size.Height;
            Shortcut.name = txtShortcutName.Text;
        }

        private void ucProgramShortcut_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                picShortcut.Focus();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void txtShortcutName_Click(object sender, EventArgs e)
        {
            if (!IsSelected)
                ucProgramShortcut_Click(sender, e);
        }

        // -----------------------------------------
        // ICON LOADING (CUSTOM + DEFAULT)
        // -----------------------------------------

        // Public method to refresh icon display on-demand
        public void RefreshIcon()
        {
            LoadIconImage();
        }

        // Helper: convert relative paths to absolute (config/<GroupName>/...)
        private string GetAbsoluteIconPath(string customPath)
        {
            if (string.IsNullOrWhiteSpace(customPath))
                return string.Empty;

            if (Path.IsPathRooted(customPath))
                return customPath;

            return Path.Combine(MainPath.path, "config", MotherForm.Category.Name, customPath);
        }

        // Helper: load image from disk WITHOUT locking file
        private static Bitmap LoadBitmapNoLock(string absolutePath)
        {
            using (var ms = new MemoryStream(File.ReadAllBytes(absolutePath)))
            using (var img = Image.FromStream(ms))
            {
                return new Bitmap(img);
            }
        }

        // Helper: safely replace current logo/background image and dispose old logo if owned
        private void SetIconBitmap(Bitmap newBmp)
        {
            // Dispose previous logo if it exists and is not the error resource
            if (logo != null)
            {
                try { logo.Dispose(); } catch { }
                logo = null;
            }

            logo = newBmp;
            picShortcut.BackgroundImage = logo;
        }

        // Load custom icon from CustomIconPath if available; returns null if missing/invalid
        private Bitmap LoadCustomIcon(string customPath)
        {
            try
            {
                string absolutePath = GetAbsoluteIconPath(customPath);
                if (!string.IsNullOrEmpty(absolutePath) && File.Exists(absolutePath))
                {
                    return LoadBitmapNoLock(absolutePath);
                }
            }
            catch
            {
                // fall back
            }
            return null;
        }

        // Original icon loading behavior (default)
        private Bitmap BuildDefaultIconBitmap()
        {
            if (Shortcut.isWindowsApp)
            {
                // handleWindowsApp returns a Bitmap; make a copy to own/dispose safely
                using (var bmp = handleWindowsApp.getWindowsAppIcon(Shortcut.FilePath, true))
                {
                    return new Bitmap(bmp);
                }
            }

            if (File.Exists(Shortcut.FilePath))
            {
                string imageExtension = Path.GetExtension(Shortcut.FilePath).ToLower();

                if (imageExtension == ".lnk")
                {
                    using (var bmp = frmGroup.handleLnkExt(Shortcut.FilePath))
                    {
                        return new Bitmap(bmp);
                    }
                }

                using (var bmp = Icon.ExtractAssociatedIcon(Shortcut.FilePath).ToBitmap())
                {
                    return new Bitmap(bmp);
                }
            }

            if (Directory.Exists(Shortcut.FilePath))
            {
                try
                {
                    using (var bmp = handleFolder.GetFolderIcon(Shortcut.FilePath).ToBitmap())
                    {
                        return new Bitmap(bmp);
                    }
                }
                catch
                {
                    // fall through
                }
            }

            // Error icon: clone the resource to own/dispose safely
            return new Bitmap(global::client.Properties.Resources.Error);
        }

        // Main entry: load icon (custom if possible, else default)
        private void LoadIconImage()
        {
            // Try custom first
            if (!string.IsNullOrEmpty(Shortcut.CustomIconPath))
            {
                Bitmap custom = LoadCustomIcon(Shortcut.CustomIconPath);
                if (custom != null)
                {
                    SetIconBitmap(custom);
                    return;
                }
            }

            // Default
            SetIconBitmap(BuildDefaultIconBitmap());
        }
    }
}
