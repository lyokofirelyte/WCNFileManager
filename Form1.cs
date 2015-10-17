using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WMPLib;
using System.Net;
using System.IO;
using System.Threading;
using System.Net.Http;
using System.Web.Script.Serialization;
using Microsoft.Win32;
using System.Drawing.Drawing2D;

namespace WindowsFormsApplication1
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private List<String> timers = new List<String>();
        private int settings = 0;
        private Dictionary<String, String> defaultFancyLabels = new Dictionary<String, String>();
        private Dictionary<String, String> globalSettings = new Dictionary<String, String>();
        private string filePath = "";
        private FancyLabel loginUsername;
        private FancyLabel loginPassword;
        private string password = "";
        private bool loggedIn = false;
        private bool bgChanged = false;
        private bool naturalEnding = true;
        private WindowsMediaPlayer wmp = new WindowsMediaPlayer();
        private string[] list;
        private Dictionary<string, Bitmap> fullImages = new Dictionary<string, Bitmap>();
        private Dictionary<string, Bitmap> thumbs = new Dictionary<string, Bitmap>();
        private Bitmap currentBG;
        private List<Control> previousVisible = new List<Control>();
        private PictureBox loading = new PictureBox();
        private bool viewingImage = false;
        private FancyLabel clickedImage;
        private bool working = false;
        private int startX = 0;
        private List<FancyLabel> multi = new List<FancyLabel>();
        private WebClient client = new WebClient();
        private bool shittyComputerMode = false;

        private void updateSettings()
        {
            try
            {
                string[] settings = File.ReadAllLines(filePath + "/settings.wcn");

                foreach (string setting in settings)
                {
                    globalSettings[setting.Split(new char[] { '~' })[0]] = setting.Split(new char[] { '~' })[1];
                }
            }
            catch (Exception inUse)
            {
                MessageBox.Show(inUse.Message);
            }

            if (globalSettings.ContainsKey("ShittyComputerMode") && globalSettings["ShittyComputerMode"].Equals("Yes"))
            {
                shittyComputerMode = true;
            }
        }

        protected virtual bool IsFileLocked(string file)
        {
            FileStream stream = null;

            try
            {
                stream = File.Open(file, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }

            //file is not locked
            return false;
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            filePath = "C:/Users/" + Environment.UserName + "/AppData/Roaming/WCNFileManager";
            Opacity = 0;

            loading.Load("http://i.imgur.com/LXsNAQB.jpg");

           if (!Directory.Exists(filePath))
           {
               Directory.CreateDirectory(filePath);
               Directory.CreateDirectory(filePath + "/Music/");
               Directory.CreateDirectory(filePath + "/Download/");
               Directory.CreateDirectory(filePath + "/System/");
           }

           if (!File.Exists(filePath + "/settings.wcn"))
           {
               var myFile = File.Create(filePath + "/settings.wcn");
               myFile.Close();
           }

           loginUsername = new FancyLabel()
           {
               Size = tempUsername.Size,
               Location = tempUsername.Location,
               Text = "username",
               Font = new Font("Lithos Pro Regular", 24),
               BackColor = Color.Transparent,
               ForeColor = tempUsername.ForeColor,
               Visible = true,
               Parent = backgroundBack,
               TextAlign = ContentAlignment.TopCenter,
               Name = "loginUsername",
               BorderStyle = tempUsername.BorderStyle
           };

           loginPassword = new FancyLabel()
           {
               Size = tempPassword.Size,
               Location = tempPassword.Location,
               Text = "********",
               Font = new Font("Lithos Pro Regular", 24),
               BackColor = Color.Transparent,
               ForeColor = tempPassword.ForeColor,
               Visible = true,
               Parent = backgroundBack,
               TextAlign = ContentAlignment.TopCenter,
               Name = "loginPassword",
               BorderStyle = tempPassword.BorderStyle
           };

           defaultFancyLabels.Add("loginUsername", "username");
           defaultFancyLabels.Add("loginPassword", "********");

           backgroundBack.Controls.Add(loginUsername);
           backgroundBack.Controls.Add(loginPassword);

           AllowDrop = true;
           updateSettings();

            if (globalSettings.ContainsKey("BGPath"))
            {
                try
                {
                    backgroundBack.Image = new Bitmap(globalSettings["BGPath"]);
                    backgroundBack.SizeMode = (PictureBoxSizeMode)Enum.Parse(typeof(PictureBoxSizeMode), globalSettings["BGStyle"], true);
                } catch (Exception botchedPath)
                {
                    backgroundBack.Load(!shittyComputerMode ? "http://i.imgur.com/LvoiQw6.gif" : "http://i.imgur.com/78jicHm.jpg");
                    MessageBox.Show("Error setting BG:\n" + botchedPath.Message);
                }
            }
            else
            {
                backgroundBack.Load(!shittyComputerMode ? "http://i.imgur.com/LvoiQw6.gif" : "http://i.imgur.com/78jicHm.jpg");
                backgroundBack.SizeMode = PictureBoxSizeMode.StretchImage;
            }

            backgroundBack.Size = Size;
            wcn.Parent = backgroundBack;

            backgroundBack.BackColor = Color.Transparent;

            loginUsername.Visible = true;
            loginPassword.Visible = true;

            loginUsername.Parent = backgroundBack;
            loginPassword.Parent = backgroundBack;

            foreach (FancyLabel option in new FancyLabel[]{ loginUsername, loginPassword })
            {
                Control cont = (Control)option;
                cont.KeyPress += new KeyPressEventHandler(onKeyPress);
                cont.MouseDown += new MouseEventHandler(onMouseDown);
                cont.LostFocus += new EventHandler(onLabelLoseFocus);

                cont.MouseEnter += new EventHandler(focusGained);
                cont.MouseLeave += new EventHandler(focusLost);
            }

            Control cc = (Control)opacity;
            cc.LostFocus += new EventHandler(onLabelLoseFocus);
            cc.MouseEnter += new EventHandler(focusGained);
            cc.MouseLeave += new EventHandler(focusLost);

            this.DoubleBuffered = true;
            this.BackColor = Color.Black;

            Label tb = new Label();
            tb.Name = "console";
            Controls.Add(tb);
            tb.Location = backgroundBack.Location;
            tb.Size = new Size(backgroundBack.Size.Height - loginUsername.Size.Height - 2, backgroundBack.Size.Width);
            tb.BackColor = Color.Transparent;
            tb.ForeColor = Color.White;
            tb.Text = "[ <- ] requesting boot information";
            tb.Text += "\n[ -> ] system is ready\n[ -> ] dropping mixtape";
            tb.Text += shittyComputerMode ? "\n[ -> ] shitty computer mode enabled" : "\n[ -> ] hd mode enabled";
            tb.Visible = true;
            tb.BringToFront();
            tb.Parent = backgroundBack;
            tb.TextChanged += new EventHandler(textChangedOnConsole);

            loginUsername.KeyPress += new KeyPressEventHandler(loginUsername_KeyPress);
            loginPassword.KeyPress += new KeyPressEventHandler(loginUsername_KeyPress);

            showSettings();

            if (!shittyComputerMode)
            {
                wmp.settings.mute = true;
                wmp.PlayStateChange += new WMPLib._WMPOCXEvents_PlayStateChangeEventHandler(playStateChange);

                if (globalSettings.ContainsKey("PlayMusic") && globalSettings["PlayMusic"].Equals("Yes"))
                {
                    string[] filez = Directory.GetFiles(filePath + "/Music/", "*.*");
                    if (filez.Length == 0)
                    {
                        wmp.URL = "http://f.worldscolli.de/mlpas.mp3";
                    }
                    else
                    {
                        wmp.URL = filez[new Random().Next(filez.Length)];
                    }
                    wmp.settings.mute = false;
                }
            }

            if (globalSettings.ContainsKey("Username"))
            {
                loginUsername.Text = globalSettings["Username"];
                defaultFancyLabels["loginUsername"] = globalSettings["Username"];
            }

            if (!shittyComputerMode)
            {
                int amt = 0;

                foreach (FancyLabel l in new FancyLabel[] { new FancyLabel() { Name = "ControlPlay" }, new FancyLabel() { Name = "ControlPause" }, new FancyLabel() { Name = "ControlStop" }, new FancyLabel() { Name = "ControlNext" } })
                {
                    l.Location = new Point(1100 + amt, Controls.Find("console", true)[0].Location.Y);
                    l.AutoSize = true;
                    l.Text = l.Name.Replace("Control", "");
                    l.Font = new Font("Lithos Pro Regular", 11);
                    l.BackColor = Color.Transparent;
                    l.ForeColor = Color.White;
                    l.Parent = backgroundBack;

                    Control cont = (Control)l;
                    cont.MouseDown += new MouseEventHandler(onMouseDown);
                    cont.LostFocus += new EventHandler(onLabelLoseFocus);
                    cont.MouseEnter += new EventHandler(focusGained);
                    cont.MouseLeave += new EventHandler(focusLost);

                    backgroundBack.Controls.Add(l);
                    l.Visible = true;
                    l.BringToFront();
                    amt += l.Width + 10;
                    defaultFancyLabels[l.Name] = l.Text;
                }
            }

            currentBG = new Bitmap((Bitmap)backgroundBack.Image.Clone());

            opacity.Parent = backgroundBack;
            opacity.Visible = true;
            opacity.BringToFront();

            for (double d = 0; d <= 1; d += 0.01)
            {
                Opacity += d;
                await Task.Delay(40);
            }

            if (globalSettings.ContainsKey("ClientID"))
            {
                if (!bgChanged)
                {
                    backgroundBack.Load("http://i.imgur.com/78jicHm.jpg");
                    currentBG = (Bitmap)backgroundBack.Image.Clone();
                }

                login();
            }
        }

        private void playStateChange(int input)
        {
            if (naturalEnding && input == (int)WMPLib.WMPPlayState.wmppsMediaEnded)
            {
                wmp = new WindowsMediaPlayer();
                wmp.PlayStateChange += new WMPLib._WMPOCXEvents_PlayStateChangeEventHandler(playStateChange);
                string[] filez = Directory.GetFiles(filePath + "/Music/", "*.*");
                if (filez.Length == 0)
                {
                    wmp.URL = "http://f.worldscolli.de/mlpas.mp3";
                }
                else
                {
                    wmp.URL = filez[new Random().Next(filez.Length)];
                }
            }
        }

        private void opacityMover(object o, MouseEventArgs e)
        {
        }

        private void playState(object o)
        {

        }

        public sealed class Wallpaper
        {
            Wallpaper() { }

            const int SPI_SETDESKWALLPAPER = 20;
            const int SPIF_UPDATEINIFILE = 0x01;
            const int SPIF_SENDWININICHANGE = 0x02;

            [DllImport("user32.dll", CharSet = CharSet.Auto)]
            static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);

            public enum Style : int
            {
                Tiled,
                Centered,
                Stretched,
                Fill
            }

            public static void Set(string a, Style style)
            {
                System.IO.Stream s = new System.Net.WebClient().OpenRead(new Uri(a));
                System.Drawing.Image img = System.Drawing.Image.FromStream(s);
                string tempPath = Path.Combine(Path.GetTempPath(), "wallpaper.bmp");
                img.Save(tempPath, System.Drawing.Imaging.ImageFormat.Bmp);

                RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", true);
                if (style == Style.Stretched)
                {
                    key.SetValue(@"WallpaperStyle", 2.ToString());
                    key.SetValue(@"TileWallpaper", 0.ToString());
                }

                if (style == Style.Centered)
                {
                    key.SetValue(@"WallpaperStyle", 1.ToString());
                    key.SetValue(@"TileWallpaper", 0.ToString());
                }

                if (style == Style.Tiled)
                {
                    key.SetValue(@"WallpaperStyle", 1.ToString());
                    key.SetValue(@"TileWallpaper", 1.ToString());
                }

                if (style == Style.Fill)
                {
                    key.SetValue(@"WallpaperStyle", 10.ToString());
                    key.SetValue(@"TileWallpaper", 0.ToString());
                }

                SystemParametersInfo(SPI_SETDESKWALLPAPER,
                    0,
                    tempPath,
                    SPIF_UPDATEINIFILE | SPIF_SENDWININICHANGE);
            }
        }

        private void showSettings()
        {
            if (globalSettings.ContainsKey("BGPath"))
            {
                bgChanged = true;
            }
            if (!globalSettings.ContainsKey("MultiSelect"))
            {
                globalSettings["MultiSelect"] = "No";
            }
            if (!globalSettings.ContainsKey("PlayMusic"))
            {
                globalSettings["PlayMusic"] = "Yes";
            }
            if (!globalSettings.ContainsKey("BGStyle"))
            {
                globalSettings["BGStyle"] = "StretchImage";
            }
            if (!globalSettings.ContainsKey("ShittyComputerMode"))
            {
                globalSettings["ShittyComputerMode"] = "No";
            }
            else
            {
                shittyComputerMode = globalSettings["ShittyComputerMode"].Equals("Yes") ? true : false;
            }
            initSetting("option_LobbyMusicURL", "Open Folder", "optionInput_LobbyMusicURL", "MusicURL");
            initSetting("option_ResetBackground", "Reset BG", "optionInput_ResetBackground",  "ResetBackground");
            initSetting("option_BGStyle", "BG Style", "optionInput_BGStyle", "BGStyle");
            initSetting("option_HoverMode", "Hover Mode", "optionInput_HoverMode", "HoverMode");
            initSetting("option_MultiSelect", "Multi Select", "optionInput_MultiSelect", "MultiSelect");
            initSetting("option_ShittyComputerMode", "Shitty Computer Mode", "optionInput_ShittyComputerMode", "ShittyComputerMode");
            initSetting("option_Help", "Help", "optionInput_Help", "Help");
        }

        private void initSetting(String name, String text, String optionName, String settingValue)
        {

            FancyLabel option = new FancyLabel();
            option.Name = optionName;
            option.Text = text;
            option.AutoSize = true;
            option.Location = new Point(250 + settings, Controls.Find("console", true)[0].Location.Y);
            option.BackColor = Color.Transparent;
            option.ForeColor = Color.White;
            option.Visible = true;
            backgroundBack.Controls.Add(option);
            option.BringToFront();
            option.Font = new Font("Lithos Pro Regular", 11);
            defaultFancyLabels.Add(option.Name, option.Text);

            if (settingValue.Equals("MultiSelect"))
            {
                if (globalSettings["MultiSelect"].Equals("Yes"))
                {
                    option.BackColor = Color.Purple;
                }
            }

            if (settingValue.Equals("ShittyComputerMode"))
            {
                if (globalSettings["ShittyComputerMode"].Equals("Yes"))
                {
                    option.BackColor = Color.Purple;
                }
            }

            Control cont = (Control)option;
            cont.KeyPress += new KeyPressEventHandler(onKeyPress);
            cont.MouseDown += new MouseEventHandler(onMouseDown);
            cont.LostFocus += new EventHandler(onLabelLoseFocus);

            cont.MouseEnter += new EventHandler(focusGained);
            cont.MouseLeave += new EventHandler(focusLost);

            settings += option.Width + 10;
        }

        private void onLabelLoseFocus(object o, EventArgs e)
        {
            if (o is FancyLabel)
            {
                FancyLabel label = (FancyLabel)o;
                if (label.Text == "" || label.Text.Length == 0)
                {
                    if (defaultFancyLabels.ContainsKey(label.Name))
                    {
                        label.Text = defaultFancyLabels[label.Name];
                    }
                }
            }
        }

        private void onKeyPress(object o, KeyPressEventArgs e)
        {
            FancyLabel label = (FancyLabel)o;

            switch (label.Name)
            {
                case "loginUsername": case "loginPassword":

                    if (e.KeyChar == (char) Keys.Back)
                    {
                        if (label.Text.Length > 0)
                        {
                            label.Text = label.Text.Substring(0, label.Text.Length - 1);
                        }
                        if (label.Name.Equals("loginPassword") && password.Length > 0)
                        {
                            password = password.Substring(0, password.Length - 1);
                        }
                    }
                    else if (e.KeyChar != (char) Keys.Enter)
                    {
                        if (label.Name.Equals("loginPassword"))
                        {
                            password += e.KeyChar;
                            label.Text += "*";
                        }
                        else
                        {
                            label.Text += e.KeyChar;
                        }
                    }

                    if (e.KeyChar != (char)Keys.Enter)
                    {
                        defaultFancyLabels[label.Name] = label.Text;
                    }

                break;
            }
        }

        public partial class FancyLabel : Label
        {
            public FancyLabel()
            {
                SetStyle(ControlStyles.Selectable, true);
                SetStyle(ControlStyles.ResizeRedraw, true);
                SetStyle(ControlStyles.UserPaint, true);
                SetStyle(ControlStyles.AllPaintingInWmPaint, true);
                SetStyle(ControlStyles.OptimizedDoubleBuffer, true); 
            }
        }

        protected async void onMouseDown(object o, MouseEventArgs e)
        {
            Label tb = (Label) Controls.Find("console", true)[0];

            switch (((Control) o).Name)
            {

                case "optionInput_Help":

                    Form helpForm = new Form();
                    helpForm.AutoSize = true;
                    helpForm.Location = tempUsername.Location;

                    FancyLabel help = new FancyLabel();
                    help.Text = "WCN Quick Help";
                    help.Font = loginUsername.Font;
                    help.AutoSize = true;
                    helpForm.Controls.Add(help);

                    FancyLabel message = new FancyLabel();
                    message.Text = "Hello. Welcome to WCN File Manager by David (Hugs).";
                    message.Text += "\nYou can drag a background image onto the program to replace the default one.";
                    message.Text += "\nSometimes large images will make scrolling laggier.";
                    message.Text += "\nYou can scroll through images using the bottom bar.";
                    message.Text += "\nClicking closer to the left will move left, and clicking closer to the right will move right.";
                    message.Text += "\nYou can change how hovering over images behaves with the hover mode option.";
                    message.Text += "\nThe bg style option cycles through background image size modes.";
                    message.Text += "\nIt may take awhile for all images to load when scrolling. Once loaded it will be smooth.";
                    message.Text += "\nDownloaded images can be viewed by using the open folder option.";
                    message.Text += "\nYou can add music to the music folder and it will cycle through playing them. You can use the next button as well.";
                    message.Text += "\nThis program remembers every setting you choose and stores in %appdata%.";
                    message.Text += "\nThank you for trying WCN File Manager BETA.";
                    message.Font = tb.Font;
                    message.Location = new Point(help.Location.X, help.Location.Y + 5 + help.Height);
                    message.AutoSize = true;
                    helpForm.Controls.Add(message);

                    helpForm.Visible = true;

                break;

                case "optionInput_HoverMode":

                    if (!globalSettings.ContainsKey("HoverMode"))
                    {
                        globalSettings["HoverMode"] = "No";
                    }
                    else
                    {
                        string hover = globalSettings["HoverMode"];
                        globalSettings["HoverMode"] = hover.Equals("Yes") ? "No" : "Yes";
                    }

                    tb.Text += "\n[ -> ] hover preview set to " + globalSettings["HoverMode"];

                break;

                case "optionInput_PlayMusic": case "ControlPlay":

                    if ((int) wmp.playState != (int) WMPLib.WMPPlayState.wmppsPlaying)
                    {
                        if (wmp.URL.Equals("") || wmp.URL == null)
                        {
                            wmp = new WindowsMediaPlayer();
                            wmp.PlayStateChange += new WMPLib._WMPOCXEvents_PlayStateChangeEventHandler(playStateChange);
                            string[] filez = Directory.GetFiles(filePath + "/Music/", "*.*");
                            if (filez.Length == 0)
                            {
                                wmp.URL = "http://f.worldscolli.de/mlpas.mp3";
                            }
                            else
                            {
                                wmp.URL = filez[new Random().Next(filez.Length)];
                            }
                        }

                        wmp.controls.play();
                        globalSettings["PlayMusic"] = "Yes";
                        naturalEnding = true;
                        tb.Text += "\n[ -> ] playing " + wmp.currentMedia.name;
                    }

                break;

                case "optionInput_ShittyComputerMode":
                    
                    if (shittyComputerMode)
                    {
                        globalSettings["ShittyComputerMode"] = "No";
                    }
                    else
                    {
                        globalSettings["ShittyComputerMode"] = "Yes";
                    }

                    MessageBox.Show("The program needs to restart in order for this to take effect.\nYou will have to manually open it again.");
                    Application.Exit();

                break;

                case "optionInput_MultiSelect":

                    if (!globalSettings.ContainsKey("MultiSelect"))
                    {
                        globalSettings["MultiSelect"] = "No";
                    }

                    string value = globalSettings["MultiSelect"];
                    globalSettings["MultiSelect"] = value.Equals("Yes") ? "No" : "Yes";

                    if (globalSettings["MultiSelect"].Equals("Yes"))
                    {
                        ((Control)o).BackColor = Color.Purple;
                    }
                    else
                    {
                         ((Control)o).BackColor = Color.Transparent;
                    }

                break;

                case "optionInput_ResetBackground":

                    backgroundBack.Load(loggedIn ? "http://i.imgur.com/78jicHm.jpg" : "http://i.imgur.com/LvoiQw6.gif");
                    bgChanged = false;
                    globalSettings.Remove("BGPath");
                    backgroundBack.SizeMode = PictureBoxSizeMode.StretchImage;
                    currentBG = (Bitmap)backgroundBack.Image.Clone();
                    tb.Text += "\n[ -> ] background reset to default";

                break;

                case "ControlPause":

                    wmp.controls.pause();
                    globalSettings["PlayMusic"] = "No";
                    tb.Text += "\n[ -> ] paused " + wmp.currentMedia.name;

                break;

                case "ControlStop":

                    naturalEnding = false;
                    wmp.controls.stop();
                    globalSettings["PlayMusic"] = "No";
                    tb.Text += "\n[ -> ] stopped " + wmp.currentMedia.name;

                break;

                case "ControlNext":

                    naturalEnding = false;
                    wmp.controls.stop();
                    wmp = new WindowsMediaPlayer();
                    wmp.PlayStateChange += new WMPLib._WMPOCXEvents_PlayStateChangeEventHandler(playStateChange);
                    string[] files = Directory.GetFiles(filePath + "/Music/", "*.*");
                    if (files.Length == 0)
                    {
                        wmp.URL = "http://f.worldscolli.de/mlpas.mp3";
                    }
                    else
                    {
                        wmp.URL = files[new Random().Next(files.Length)];
                    }

                    naturalEnding = true;
                    tb.Text += "\n[ -> ] playing " + wmp.currentMedia.name;

                break;

                case "optionInput_BGStyle":

                    PictureBoxSizeMode[] pb = new PictureBoxSizeMode[] { PictureBoxSizeMode.CenterImage, PictureBoxSizeMode.Normal, PictureBoxSizeMode.StretchImage, PictureBoxSizeMode.Zoom };
                    string currSetting = globalSettings["BGStyle"];

                    for (int i = 0; i < pb.Length; i++)
                    {
                        if (pb[i].ToString().Replace(" ", "").Equals(currSetting))
                        {
                            int x = i + 1;
                            if (x >= pb.Length)
                            {
                                x = 0;
                            }
                            globalSettings["BGStyle"] = pb[x].ToString().Replace(" ", "");
                            backgroundBack.SizeMode = pb[x];
                            break;
                        }
                    }

                    tb.Text += "\n[ -> ] changing bg style to " + backgroundBack.SizeMode.ToString();

                break;

                case "PictureOptionCopy to Clipboard":

                    Clipboard.SetText(clickedImage.Name);
                    tb.Text += "\n[ -> ] copied to clipboard";

                break;

                case "PictureOptionSet as Desktop":

                    if (clickedImage.Text.Equals(""))
                    {
                        BackgroundWorker bw = new BackgroundWorker();
                        bw.DoWork += (b, w) =>
                        {
                            Wallpaper.Set(clickedImage.Name, Wallpaper.Style.Fill);
                        };

                        bw.RunWorkerAsync();

                        tb.Text += "\n[ -> ] changing wallpaper";
                    }
                    else
                    {
                        tb.Text += "\n[ -> ] error setting wallpaper";
                    }

                    ((Control)o).Enabled = false;

                break;

                case "PictureOptionDownload":

                    if (multi.Count() == 0)
                    {
                        multi.Add(clickedImage);
                    }

                    foreach (FancyLabel dl in multi)
                    {
                        string[] nameSplit = dl.Name.Split('/');
                        WebClient client = new WebClient();
                        client.DownloadFileAsync(new Uri(dl.Name), filePath + "/Download/" + nameSplit[nameSplit.Length - 1]);
                        ((Control)o).Enabled = false;

                        client.DownloadFileCompleted += (a, b) =>
                        {
                            tb.Text += "\n[ -> ] download of " + nameSplit[nameSplit.Length - 1] + " complete";
                            ((Control)o).Text = "Download";
                        };

                        client.DownloadProgressChanged += (a, b) =>
                        {
                            tb.Text += "\n[ -> ] downloading: " + b.ProgressPercentage + "%";
                            ((Control)o).Text = nameSplit[nameSplit.Length - 1] + ": " + b.ProgressPercentage + "%";
                        };
                    }

                break;

                case "PictureOptionOpen":

                    if (multi.Count() == 0)
                    {
                        multi.Add(clickedImage);
                    }

                    foreach (FancyLabel l in multi)
                    {
                        System.Diagnostics.Process.Start(l.Name);
                    }

                break;

                case "PictureOptionDelete":

                    if (multi.Count() == 0)
                    {
                        multi.Add(clickedImage);
                    }

                    foreach (FancyLabel l in multi)
                    {
                        string apiUrl = "http://files.worldscolli.de/api/remove";
                        var client = new HttpClient();
                        var values = new Dictionary<string, string>()
                        {
                            {"client_id", globalSettings["ClientID"]},
                            {"path", l.Name.Replace(@"http://f.worldscolli.de/", "")}
                        };

                        var content = new FormUrlEncodedContent(values);

                        try
                        {
                            var response = await client.PostAsync(apiUrl, content);
                            response.EnsureSuccessStatusCode();
                        }
                        catch (Exception botchedPost) {
                            MessageBox.Show(botchedPost.Message);
                        }

                        //string apiUrl = "http://files.worldscolli.de/api/remove?client_id=" + globalSettings["ClientID"] + "&path=" + l.Name.Replace(@"f.worldscolli.de/", "");
                       // HttpWebRequest request = (HttpWebRequest)WebRequest.Create(apiUrl);

                        fullImages.Remove(l.Name);
                        thumbs.Remove(l.Name);
                        backgroundBack.Controls.Remove(l);
                    }

                    int xAmt = 0;
                    int yAmt = 0;
                    int yRows = 0;

                    foreach (string pic in thumbs.Keys)
                    {
                        FancyLabel leb = (FancyLabel) Controls.Find(pic, true)[0];
                        leb.Location = new Point(10 + xAmt, tempUsername.Location.Y - 80 + yAmt);
                        yAmt += 40 + leb.Height;
                        yRows++;

                        if (yRows == 3)
                        {
                            yAmt = 0;
                            yRows = 0;
                            xAmt += 40 + leb.Width;
                        }
                    }

                    multi.Clear();
                    onMouseDown(Controls.Find("PictureOptionCancel", true)[0], e);

                break;

                case "PictureOptionCancel":

                    viewingImage = false;
                    FancyLabel black = (FancyLabel)Controls.Find("black", true)[0];

                    foreach (string s in new string[] { "Copy to Clipboard", "Open", "Download", "Set as Desktop", "Delete", "Cancel" })
                    {
                        Controls.Find("PictureOption" + s, true)[0].Visible = false;
                        Controls.Find("PictureOption" + s, true)[0].Enabled = false;
                    }

                    backgroundBack.Image = currentBG;

                    foreach (Control c in previousVisible)
                    {
                        c.Visible = true;
                        c.Enabled = true;
                    }

                    clickedImage.BorderStyle = BorderStyle.None;
                    previousVisible.Clear();

                    for (int i = 200; i >= 0; i -= 10)
                    {
                        black.BackColor = Color.FromArgb(i, 0, 0, 0);
                        if (!shittyComputerMode)
                        {
                            await Task.Delay(10);
                        }
                    }

                    black.Visible = false;

                    foreach (FancyLabel l in multi)
                    {
                        l.BorderStyle = BorderStyle.None;
                    }

                    multi = new List<FancyLabel>();

                break;

                default:

                    try
                    {
                        ((FancyLabel)o).Select();
                        ((FancyLabel)o).Text = "";
                        if (((FancyLabel)o).Name.Equals("loginPassword"))
                        {
                            password = "";
                        }
                    }
                    catch (Exception ee) { }

                break;

                case "optionInput_LobbyMusicURL":

                    OpenFileDialog file = new OpenFileDialog();
                    file.InitialDirectory = (filePath).Replace("/", "\\");
                    file.RestoreDirectory = true;
                    file.AutoUpgradeEnabled = true;
                    file.Title = "WCN File Manager";
                    file.ShowDialog();

                break;
            }
        }

        protected void onPaint(object o, PaintEventArgs e)
        {

        }

        protected void onScroll(object o, ScrollEventArgs e)
        {
            base.OnScroll(e);
        }

        private void focusGained(object sender, EventArgs e)
        {
            Control control = (Control)sender;

            if (!timers.Contains((control.Name)))
            {
                if (control is TextBox)
                {
                    if (((TextBox) control).Text.Equals("username"))
                    {
                        ((TextBox) control).Clear();
                    }

                    if (((TextBox)control).Text.Equals("password"))
                    {
                        ((TextBox)control).Clear();
                    }
                }

                if (!shittyComputerMode)
                {
                    timers.Add(control.Name);
                    startTimer(control);
                }
            }
        }

        private void focusLost(object sender, EventArgs e)
        {
            if (timers.Contains(((Control)sender).Name))
            {
                timers.Remove(((Control)sender).Name);
                if (sender is FancyLabel || sender is Label)
                {
                    if (((Control) sender).Name.Contains("PictureOption"))
                    {
                        ((Control)sender).BackColor = Color.Purple;
                    }
                    else
                    {
                      ((Control)sender).BackColor = Color.Transparent;
                    }
                }
                else if (sender is TextBox)
                {
                    ((Control)sender).BackColor = Color.FromName("Control");
                }
            }

            if (sender is FancyLabel)
            {
                FancyLabel label = (FancyLabel)sender;
                if (label.Text == "" || label.Text.Length == 0)
                {
                    if (defaultFancyLabels.ContainsKey(label.Name))
                    {
                        label.Text = defaultFancyLabels[label.Name];
                    }
                }
                if (label.Name.Contains("MultiSelect"))
                {
                    if (globalSettings.ContainsKey("MultiSelect") && globalSettings["MultiSelect"].Equals("Yes"))
                    {
                        label.BackColor = Color.Purple;
                    }
                }
            }
        }

        private void loginBackground_Click(object sender, EventArgs e)
        {

        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        public void updateIcon(PictureBox control, IconName name)
        {
            control.Load(getIcon(name));
            control.BackColor = Color.Transparent;
        }

        public String getIcon(IconName name)
        {
            switch (name)
            {
                case IconName.ANNOUNCER: return "http://i.imgur.com/wBG9R4o.png";
                case IconName.ENVELOPE: return "http://i.imgur.com/VPgOCNV.png";
            }

            return "none";
        }

        public enum IconName
        {
            ANNOUNCER,
            ENVELOPE
        }

        private void loginUsername_MouseEnter(object sender, EventArgs e)
        {
            
        }

        private void loginUsername_MouseLeave(object sender, EventArgs e)
        {

        }

        private async void startTimer(Control ctrl)
        {
            while (timers.Contains(ctrl.Name))
            {
                for (double i = 0; i < 1 && timers.Contains(ctrl.Name); i+=0.01)
                {
                    ColorRGB c = HSL2RGB(i, 0.5, 0.5);
                    await Task.Delay(50);
                    ctrl.BackColor = c;
                }

                if (!timers.Contains(ctrl.Name))
                {
                    if (ctrl is TextBox)
                    {
                        ctrl.BackColor = Color.White;
                    }

                    if (ctrl is FancyLabel || ctrl is Label)
                    {
                        if (ctrl.Name.Contains("PictureOption"))
                        {
                            ctrl.BackColor = Color.Purple;
                        }
                        else
                        {
                            ctrl.BackColor = Color.Transparent;
                        }
                    }

                    break;
                }
            }

            if (!timers.Contains(ctrl.Name))
            {
                if (ctrl is TextBox)
                {
                    ctrl.BackColor = Color.White;
                }

                if (ctrl is FancyLabel || ctrl is Label)
                {
                   if (ctrl.Name.Contains("PictureOption"))
                   {
                       ctrl.BackColor = Color.Purple;
                   }
                   else if (ctrl.Name.Contains("MultiSelect"))
                   {
                       if (globalSettings.ContainsKey("MultiSelect") && globalSettings["MultiSelect"].Equals("Yes"))
                       {
                           ctrl.BackColor = Color.Purple;
                       }
                   }
                   else
                   {
                       ctrl.BackColor = Color.Transparent;
                   }

                }
            }
        }

        private void loginPassword_MouseEnter(object sender, EventArgs e)
        {

        }

        public struct ColorRGB
        {
            public byte R;
            public byte G;
            public byte B;
            public ColorRGB(Color value)
            {
                this.R = value.R;
                this.G = value.G;
                this.B = value.B;
            }
            public static implicit operator Color(ColorRGB rgb)
            {
                Color c = Color.FromArgb(rgb.R, rgb.G, rgb.B);
                return c;
            }
            public static explicit operator ColorRGB(Color c)
            {
                return new ColorRGB(c);
            }
        }

        public static ColorRGB HSL2RGB(double h, double sl, double l)
        {
            double v;
            double r, g, b;

            r = l;   // default to gray
            g = l;
            b = l;
            v = (l <= 0.5) ? (l * (1.0 + sl)) : (l + sl - l * sl);
            if (v > 0)
            {
                double m;
                double sv;
                int sextant;
                double fract, vsf, mid1, mid2;

                m = l + l - v;
                sv = (v - m) / v;
                h *= 6.0;
                sextant = (int)h;
                fract = h - sextant;
                vsf = v * sv * fract;
                mid1 = m + vsf;
                mid2 = v - vsf;
                switch (sextant)
                {
                    case 0:
                        r = v;
                        g = mid1;
                        b = m;
                        break;
                    case 1:
                        r = mid2;
                        g = v;
                        b = m;
                        break;
                    case 2:
                        r = m;
                        g = v;
                        b = mid1;
                        break;
                    case 3:
                        r = m;
                        g = mid2;
                        b = v;
                        break;
                    case 4:
                        r = mid1;
                        g = m;
                        b = v;
                        break;
                    case 5:
                        r = v;
                        g = m;
                        b = mid2;
                        break;
                }
            }
            ColorRGB rgb;
            rgb.R = Convert.ToByte(r * 255.0f);
            rgb.G = Convert.ToByte(g * 255.0f);
            rgb.B = Convert.ToByte(b * 255.0f);
            return rgb;
        }

        private void loginLogo_MouseMove(object sender, MouseEventArgs e)
        {
            
        }

        Bitmap screenPixel = new Bitmap(1, 1, PixelFormat.Format32bppArgb);

        [DllImport("gdi32.dll", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = true)]
        public static extern int BitBlt(IntPtr hDC, int x, int y, int nWidth, int nHeight, IntPtr hSrcDC, int xSrc, int ySrc, int dwRop);

        public Color GetColorAt(Point location)
        {
            using (Graphics gdest = Graphics.FromImage(screenPixel))
            {
                using (Graphics gsrc = Graphics.FromHwnd(IntPtr.Zero))
                {
                    IntPtr hSrcDC = gsrc.GetHdc();
                    IntPtr hDC = gdest.GetHdc();
                    int retval = BitBlt(hDC, 0, 0, 1, 1, hSrcDC, location.X, location.Y, (int)CopyPixelOperation.SourceCopy);
                    gdest.ReleaseHdc();
                    gsrc.ReleaseHdc();
                }
            }

            return screenPixel.GetPixel(0, 0);
        }

        private void loginUsername_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char) Keys.Enter)
            {
                e.Handled = true;
                validate(loginUsername.Text, password);
            }
        }

        private async void validate(String u, String p)
        {
            //backgroundBack.Visible = false;
            Label tb = (Label) Controls.Find("console", true)[0];
            tb.Text += "\n[ <- ] validating username " + loginUsername.Text;
            tb.Text += "\n[ <- ] sending login request...";

            string apiUrl = "http://files.worldscolli.de/manage/json";
           /* var client = new HttpClient();
            var values = new Dictionary<string, string>()
            {
                {"username", username},
                {"password", password},
            };

            var content = new FormUrlEncodedContent(values);*/

            var httpWebRequest = (HttpWebRequest)WebRequest.Create(apiUrl);
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = "POST";

            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                string json = new JavaScriptSerializer().Serialize(new
                {
                    username = u,
                    password = p
                });

                streamWriter.Write(json);
            }

            var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                var response = streamReader.ReadToEnd();
                try
                {
                    //var response = await client.PostAsync(apiUrl, content);
                    //response.EnsureSuccessStatusCode();

                    if (response.ToString().Contains(@"id"))
                    {
                        int start = response.ToString().IndexOf("\"id\":") + 6;
                        int stop = response.ToString().Length - 2;
                        string clientId = response.ToString().Substring(start, (stop - start));
                        globalSettings["ClientID"] = clientId;
                        tb.Text += "\n[ -> ] login success";
                        globalSettings["Username"] = u;
                        loggedIn = true;
                        if (!bgChanged)
                        {
                            backgroundBack.Load("http://i.imgur.com/78jicHm.jpg");
                            currentBG = (Bitmap)backgroundBack.Image.Clone();
                        }
                        login();
                    }
                    else
                    {
                        tb.Text += "\n[ -> ] invalid username or password";
                        flashConsole(Color.Red);
                    }
                }
                catch (Exception eee)
                {
                    tb.Text += "\n[ -> ] server error";
                }
            }
        }

        private async void login()
        {
            loggedIn = true;
            loginUsername.BackColor = Color.Transparent;
            backgroundBack.Controls.Remove(loginPassword);
            wcn.Visible = false;
            loginUsername.Font = wcn.Font;
            loginUsername.AutoSize = true;
            loginUsername.ForeColor = wcn.ForeColor;

            Control cont = (Control)loginUsername;
            cont.KeyPress -= onKeyPress;
            cont.MouseDown -= onMouseDown;
            cont.LostFocus -= onLabelLoseFocus;

            cont.MouseEnter -= focusGained;
            cont.MouseLeave -= focusLost;

            int startY = loginUsername.Location.Y;

            for (int i = 0; i <= 280/5; i++)
            {
                loginUsername.Location = new Point(loginUsername.Location.X, loginUsername.Location.Y - 5);
                if (!shittyComputerMode)
                {
                    await Task.Delay(3);
                }
            }

            string apiUrl = "http://files.worldscolli.de/api/list?client_id=" + globalSettings["ClientID"];
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(apiUrl);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream resStream = response.GetResponseStream();
            StreamReader sr = new StreamReader(resStream);
            string result = sr.ReadToEnd();
            var json = new JavaScriptSerializer();
            var data = json.Deserialize<Dictionary<string, string[]>>(result);
            list = data["list"];
            loadCollection();
        }

        private void pictureZoom(object o, EventArgs e)
        {
            if (!viewingImage)
            {
                FancyLabel leb = (FancyLabel)o;

                if (!shittyComputerMode && fullImages.ContainsKey(leb.Name))
                {
                    if ((!globalSettings.ContainsKey("HoverMode") || globalSettings["HoverMode"].Equals("Yes")) && (globalSettings["MultiSelect"].Equals("No")))
                    {
                        foreach (Control c in Controls)
                        {
                            if (c.Visible && !c.Name.Equals(leb.Name) && !c.Name.Equals("backgroundBack") && !c.Name.Equals("menu"))
                            {
                                previousVisible.Add(c);
                                c.Visible = false;
                            }
                        }

                        foreach (Control c in backgroundBack.Controls)
                        {
                            if (c.Visible && !c.Name.Equals(leb.Name) && !c.Name.Equals("backgroundBack"))
                            {
                                previousVisible.Add(c);
                                c.Visible = false;
                            }
                        }

                        if (leb.Text.Equals(""))
                        {
                            if (fullImages.ContainsKey(leb.Name))
                            {
                                backgroundBack.Image = fullImages[leb.Name];
                            }
                            else
                            {
                                backgroundBack.Image = loading.Image;
                            }
                        }
                    }
                }
                leb.BorderStyle = BorderStyle.FixedSingle;
            }
        }

        private void unZoom(object o, EventArgs e)
        {
            if (!viewingImage)
            {
                //viewingImage = false;
                if (!backgroundBack.Image.Equals(currentBG))
                {
                    backgroundBack.Image = currentBG;
                }

                foreach (Control c in previousVisible)
                {
                    c.Visible = true;
                    c.Enabled = true;
                }

                ((FancyLabel)o).BorderStyle = BorderStyle.None;
                previousVisible.Clear();
            }
        }

        private async void pictureClick(object o, MouseEventArgs e)
        {
            if (!viewingImage || globalSettings["MultiSelect"].Equals("Yes"))
            {
                FancyLabel leb = (FancyLabel)o;

                if (globalSettings["MultiSelect"].Equals("No"))
                {
                    if (fullImages.ContainsKey(leb.Name))
                    {
                        backgroundBack.Image = fullImages[leb.Name];
                    }
                    else if (leb.Text.Equals("") || leb.Text == null)
                    {
                        backgroundBack.Image = loading.Image;
                        BackgroundWorker bw = new BackgroundWorker();

                        bw.DoWork += (b, w) =>
                        {
                            WebClient client = new WebClient();

                            /*client.DownloadProgressChanged += (cl, lc) =>
                            {
                                lc.ProgressPercentage
                            };*/

                            try
                            {
                                byte[] bFile = client.DownloadData(leb.Name);
                                MemoryStream ms = new MemoryStream(bFile);
                                Image img = Image.FromStream(ms);
                                fullImages.Add(leb.Name, (Bitmap)img);
                            }
                            catch (Exception eee) { }

                        };

                        bw.RunWorkerCompleted += (b, w) =>
                        {
                            if (clickedImage.Name.Equals(leb.Name) && viewingImage)
                            {
                                Invoke((MethodInvoker)delegate { backgroundBack.Image = fullImages[leb.Name]; });
                            }
                        };

                        bw.RunWorkerAsync();
                    }
                }

                if (globalSettings["MultiSelect"].Equals("Yes"))
                {
                    if (leb.BorderStyle == BorderStyle.Fixed3D)
                    {
                        multi.Remove(leb);
                        leb.BorderStyle = BorderStyle.None;
                        return;
                    }
                    else
                    {
                        multi.Add(leb);
                    }
                }

                viewingImage = true;
                clickedImage = leb;
                leb.BorderStyle = BorderStyle.Fixed3D;
                Control console = Controls.Find("console", true)[0];
                int yAmt = 0;

                Control[] ctrl = Controls.Find("black", true);
                FancyLabel black;

                if (ctrl.Count() == 0)
                {
                    black = new FancyLabel();
                    black.BackColor = Color.FromArgb(0, 0, 0, 0);
                    black.Size = Size;
                    black.Visible = true;
                    black.Name = "black";
                    black.Parent = backgroundBack;
                    black.Location = new Point(0, 0);
                    backgroundBack.Controls.Add(black);
                }
                else
                {
                    black = (FancyLabel) ctrl[0];
                }

                black.Visible = true;
                black.BringToFront();
                leb.BringToFront();
                menu.BringToFront();

                if (globalSettings["MultiSelect"].Equals("Yes"))
                {
                    opacity.BringToFront();
                    foreach (Control cc in backgroundBack.Controls)
                    {
                        if (cc.Name.Contains("worlds"))
                        {
                            cc.BringToFront();
                        }
                    }
                }

                foreach (string s in new string[] { "Copy to Clipboard", "Open", "Download", "Set as Desktop", "Delete", "Cancel" })
                {
                    Control[] ctrls = Controls.Find("PictureOption" + s, true);

                    if (ctrls.Count() == 0)
                    {
                        FancyLabel option = new FancyLabel();
                        option.Name = "PictureOption" + s;
                        option.AutoSize = true;
                        option.Font = new Font("Lithos Pro Regular", 17);
                        option.Location = new Point(console.Location.X + 10, 200 - yAmt);
                        option.Parent = backgroundBack;
                        option.BackColor = Color.Purple;
                        option.ForeColor = Color.White;
                        option.Text = s;
                        option.Visible = true;
                        option.Enabled = true;
                        backgroundBack.Controls.Add(option);
                        option.BringToFront();
                        yAmt += 10 + option.Size.Height;

                        Control cont = (Control)option;
                        cont.MouseDown += new MouseEventHandler(onMouseDown);
                        cont.LostFocus += new EventHandler(onLabelLoseFocus);
                        cont.MouseEnter += new EventHandler(focusGained);
                        cont.MouseLeave += new EventHandler(focusLost);
                    }
                    else
                    {
                        ctrls[0].Visible = true;
                        ctrls[0].Enabled = true;
                        ctrls[0].BringToFront();
                    }
                }

                Controls.Find("PictureOptionDelete", true)[0].Text = "Delete";

                if (multi.Count() <= 1)
                {
                    for (int i = 5; i <= 200; i += 10)
                    {
                        black.BackColor = Color.FromArgb(i, 0, 0, 0);
                        if (!shittyComputerMode)
                        {
                            await Task.Delay(10);
                        }
                    }
                }
            }
        }

        private void loadCollection()
        {
            int xAmt = 0;
            int yAmt = 0;
            int yRows = 0;
            List<FancyLabel> pics = new List<FancyLabel>();

            foreach (string pic in list)
            {
                FancyLabel leb = new FancyLabel();
                leb.Visible = false;
                leb.Name = pic;
                leb.ImageAlign = ContentAlignment.MiddleCenter;
                leb.Text = "\n\n\n\n\n\n" + pic.Split('.')[pic.Split('.').Length - 1];
                leb.ForeColor = Color.Black;
                leb.BackColor = Color.Transparent;
                leb.TextAlign = ContentAlignment.TopCenter;
                leb.Font = new Font("Lithos Pro Regular", 9);
                leb.Location = new Point(10 + xAmt, tempUsername.Location.Y - 80 + yAmt);
                leb.Size = new Size(100, 100);
                leb.Parent = backgroundBack;
                leb.MouseEnter += new EventHandler(pictureZoom);
                leb.MouseLeave += new EventHandler(unZoom);
                leb.MouseClick += new MouseEventHandler(pictureClick);
                backgroundBack.Controls.Add(leb);
                pics.Add(leb);
                yAmt += 40 + leb.Height;
                yRows++;

                if (yRows == 3)
                {
                    yAmt = 0;
                    yRows = 0;
                    xAmt += 40 + leb.Width;
                }
            }

            Label tb = (Label)Controls.Find("console", true)[0];
            tb.Text += "\n[ -> ] loading " + pics.Count + " files";
            int i = 0;

            foreach (FancyLabel pb in pics)
            {
                BackgroundWorker worker = new BackgroundWorker();

                worker.DoWork += (o, w) =>
                {
                    loadEffects(pb, pb.Name);
                };

                worker.RunWorkerCompleted += (o, w) =>
                {
                    Invoke((MethodInvoker)delegate { render(pb, pb.Location.Y); });
                };

                worker.RunWorkerAsync();
            }
        }

        private void loadEffects(FancyLabel backup, string pic)
        {

            WebClient wc;
            byte[] bFile;
            MemoryStream ms;
            Image img;

            try
            {
                wc = new WebClient();
                wc.Proxy = null;
                bFile = wc.DownloadData(getThumb(pic));
                ms = new MemoryStream(bFile);
                img = Image.FromStream(ms);
                thumbs.Add(backup.Name, (Bitmap) img);
                backup.Text = "";
            }
            catch (Exception botchedThumb)
            {
                wc = new WebClient();
                wc.Proxy = null;
                bFile = wc.DownloadData("http://i.imgur.com/IoOBdm1.png");
                ms = new MemoryStream(bFile);
                img = Image.FromStream(ms);
                //backup.Image = img;
                thumbs.Add(backup.Name, (Bitmap)img);
            }
        }

        private async void render(Control c, int y)
        {
            if (!thumbs.ContainsKey(c.Name))
            {
                await Task.Delay(100);
                render(c, y);
                return;
            }

            Controls.Find("console", true)[0].Text += "\n[ -> ] rendering " + c.Name;
            c.Visible = true;
            ((FancyLabel)c).Image = thumbs[c.Name];
            c.BringToFront();
            c.Size = new Size(100, 100);

            if (!((FancyLabel)c).Text.Equals(""))
            {
                ((FancyLabel)c).ImageAlign = ContentAlignment.TopCenter;
            }
        }

        private string getThumb(string url)
        {
            return "http://files.worldscolli.de/thumb/" + url.Split('/')[url.Split('/').Length - 1];
        }

        private async void flashConsole(Color color)
        {

            Controls.Find("console", true)[0].Size = new Size(Width, Controls.Find("console", true)[0].Size.Height);

            for (int i = 10; i < 256; i+= 10)
            {
                Controls.Find("console", true)[0].BackColor = Color.FromArgb(i, color.R, color.G, color.B);
                await Task.Delay(10);
            }

            for (int i = 255; i >= 0; i -= 10)
            {
                Controls.Find("console", true)[0].BackColor = Color.FromArgb(i, color.R, color.G, color.B);
                await Task.Delay(10);
            }

            Controls.Find("console", true)[0].BackColor = Color.Transparent;
        }

        private void textChangedOnConsole(object sender, EventArgs e)
        {
            Label label = (Label)sender;
            int slashN = 0;
            int curr = 0;

            foreach (char c in label.Text)
            {
                if (c == '\n')
                {
                    slashN++;
                }
                else if (slashN == 0)
                {
                    curr++;
                }

            }

            if (curr == 0)
            {
                curr = 1;
            }

            if (slashN > 14)
            {
                label.Text = label.Text.Substring(curr);
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            string[] str = new string[globalSettings.Count];
            int i = 0;

            foreach (string s in globalSettings.Keys)
            {
                str[i] = s + "~" + globalSettings[s];
                i++;
            }

            File.WriteAllLines(filePath + "/settings.wcn", str);
        }

        private void toggleSettingsMenuToolStripMenuItem_Click(object sender, EventArgs e)
        {
            globalSettings.Clear();
            MessageBox.Show("You will need to re-open the program.");
            Application.Exit();
        }

        protected bool validData;
        string path;
        protected Image image;
        protected Thread getImageThread;

        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            string filename;
            validData = GetFilename(out filename, e);
            if (validData)
            {
                path = filename;
                getImageThread = new Thread(new ThreadStart(LoadImage));
                getImageThread.Start();
                e.Effect = DragDropEffects.Copy;
            }
            else
                e.Effect = DragDropEffects.None;
        }
        private bool GetFilename(out string filename, DragEventArgs e) 
        {
            bool ret = false;
            filename = String.Empty;
            if ((e.AllowedEffect & DragDropEffects.Copy) == DragDropEffects.Copy)
            {
                Array data = ((IDataObject)e.Data).GetData("FileDrop") as Array;
                if (data != null)
                {
                    if ((data.Length == 1) && (data.GetValue(0) is String))
                    {
                        filename = ((string[])data)[0];
                        string ext = Path.GetExtension(filename).ToLower();
                        if ((ext == ".jpg") || (ext == ".png") || (ext == ".bmp") || (ext == ".gif"))
                        {
                            ret = true;
                        }
                    }
                }
            }
            return ret;
        }
        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            if (validData)
            {
                while (getImageThread.IsAlive)
                {
                    Application.DoEvents();
                    Thread.Sleep(0);
                }
                backgroundBack.Image = image;
                globalSettings["BGPath"] = path;
                bgChanged = true;
                currentBG = (Bitmap)backgroundBack.Image.Clone();
            }
        }
        protected void LoadImage()
        {
            image = new Bitmap(path);
        }

        Bitmap bitmap, img;
        Graphics bmpgraphic;

        private async void opacity_MouseDown(object sender, MouseEventArgs e)
        {
            startX = e.X;
          //  backgroundBack.Image = null;
            working = true;
            Control cont = (Control)sender;
            bool positive = e.X > cont.Width / 2;

            while (working)
            {
                foreach (Control c in backgroundBack.Controls)
                {
                    if (c.Name.Contains("worlds"))
                    {
                        c.BackColor = Color.Black;
                        // c.Location = new Point(positive ? c.Location.X + amt : c.Location.X - amt, c.Location.Y);
                        c.Location = new Point(positive ? c.Location.X - 10 : c.Location.X + 10, c.Location.Y);
                    }
                }

                await Task.Delay(1);
            }
        }

        private void opacity_MouseUp(object sender, MouseEventArgs e)
        {
            working = false;
            foreach (Control c in backgroundBack.Controls)
            {
                if (c.Name.Contains("worlds"))
                {
                    c.BackColor = Color.Transparent;
                }
            }
        }

        private void logoutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (globalSettings.ContainsKey("ClientID"))
            {
                globalSettings.Remove("ClientID");
                globalSettings.Remove("Username");
            }

            MessageBox.Show("You will be logged out. You must manually re-open the program to log in again.");
            Application.Exit();
        }

        private void menuFile_Quit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}
