﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using Nucleus.Gaming.Coop;
using Nucleus.Gaming.Package;
using Nucleus.Gaming.Platform.Windows;
using Nucleus.Coop.App.Forms;
using System.IO;
using Nucleus.Gaming;
using Nucleus.Coop.App.Properties;
using Nucleus.Gaming.Tools.GameStarter;
using Nucleus.Gaming.Diagnostics;
using Nucleus.Gaming.Platform.Windows.Controls;

namespace Nucleus.Coop.App.Controls {
    public partial class HandlerManagerControl : BasePageControl {
        private GameHandlerBaseMetadata currentMetadata;
        private Bitmap interrobang;
        private int titleBarWidth;

        public override int RequiredTitleBarWidth { get { return titleBarWidth; } set { } }

        public HandlerManagerControl() {
            InitializeComponent();

            titleBarWidth = list_left.Width + 1;
            bool designMode = (LicenseManager.UsageMode == LicenseUsageMode.Designtime);

            this.Title = "Settings";
            //this.Image = Resources.nucleus;
            this.Image = FormGraphicsUtil.BuildCharToBitmap(new Size(40, 40), 30, Color.FromArgb(240, 240, 240), "⚙");

            if (!designMode) {
                LoadInstalled();
            }

            list_left.HorizontalScroll.Maximum = 0;
            list_left.AutoScroll = false;
            list_left.VerticalScroll.Visible = false;
            list_left.AutoScroll = true;

            panel_disks.Visible = false;
            list_storage.CanSelectControls = false;
        }

        public override void UserLeft() {
            base.UserLeft();

            panel_disks.Visible = false;
            panel_gameData.Visible = false;
            panel_installedGames.Visible = false;
            list_left.Deselect();
        }

        private GameControl listInstalled;
        private GameControl scanGames;

        private void LoadInstalled() {
            var gm = GameManager.Instance;
            var handlers = gm.User.InstalledHandlers;

            interrobang = FormGraphicsUtil.BuildCharToBitmap(new Size(40, 40), 30, Color.FromArgb(240, 240, 240), "🗋");
            list_left.Controls.Clear();

            //HorizontalLineControl line = new HorizontalLineControl();
            //list_left.Controls.Add(line);
            //line.LineHorizontalPc = 100;
            //line.Width = list_left.Width;
            //line.LineHeight = 1;
            //line.LineColor = Color.FromArgb(255, 41, 45, 47);

            TitleSeparator gamesSep = new TitleSeparator();
            gamesSep.SetTitle("GAMES");
            gamesSep.Height = 20;
            this.list_left.Controls.Add(gamesSep);

            GameControl installGame = new GameControl();
            installGame.Width = list_left.Width;
            installGame.UpdateTitleText("Install game from exe");
            installGame.Image = FormGraphicsUtil.BuildCharToBitmap(new Size(40, 40), 30, Color.FromArgb(240, 240, 240), "📁");
            installGame.Click += InstallGame_Click;
            list_left.Controls.Add(installGame);

            scanGames = new GameControl();
            scanGames.Width = list_left.Width;
            scanGames.UpdateTitleText("Scan for game exes");
            scanGames.Image = FormGraphicsUtil.BuildCharToBitmap(new Size(40, 40), 30, Color.FromArgb(240, 240, 240), "🔍");
            scanGames.Click += ScanGames_Click;
            list_left.Controls.Add(scanGames);

            listInstalled = new GameControl();
            listInstalled.Width = list_left.Width;
            listInstalled.UpdateTitleText("Remove games from list");
            listInstalled.Image = FormGraphicsUtil.BuildCharToBitmap(new Size(40, 40), 30, Color.FromArgb(240, 240, 240), "⌫", 0, 8);
            listInstalled.Click += ListInstalled_Click;
            list_left.Controls.Add(listInstalled);

            TitleSeparator sep = new TitleSeparator();
            sep.SetTitle("HANDLERS");
            sep.Height = 20;
            this.list_left.Controls.Add(sep);

            GameControl installFile = new GameControl();
            installFile.Width = list_left.Width;
            installFile.UpdateTitleText("Install handler from file");
            //installFile.Image = Resources.nucleus;
            installFile.Image = FormGraphicsUtil.BuildCharToBitmap(new Size(40, 40), 30, Color.FromArgb(240, 240, 240), "🔍");
            installFile.Click += InstallFile_Click;
            list_left.Controls.Add(installFile);

            if (handlers.Count == 0) {
                GameControl noAvailable = new GameControl();
                noAvailable.Width = list_left.Width;
                noAvailable.UpdateTitleText("No installed game handlers");
                noAvailable.Image = interrobang;
                list_left.Controls.Add(noAvailable);
            } else {
                handlers.Sort(GameHandlerMetadata.CompareHandlerTitle);
                foreach (var handler in handlers) {
                    GameControl gameHandler = new GameControl();
                    gameHandler.Width = list_left.Width;
                    gameHandler.SetHandlerMetadata(handler);
                    gameHandler.Click += GameHandler_Click;
                    gameHandler.Image = interrobang;
                    list_left.Controls.Add(gameHandler);
                }
            }

            DPIManager.ForceUpdate();
        }

        private void ResetPanels() {
            panel_disks.Visible = false;
            panel_installedGames.Visible = false;
            panel_gameData.Visible = false;
            MainForm.Instance.ChangeTitle(this.Title, this.Image);
        }

        private void ListInstalled_Click(object sender, EventArgs e) {
            MainForm.Instance.ChangeTitle("Remove Game From List", listInstalled.Image);

            panel_disks.Visible = false;
            panel_installedGames.Visible = true;
            panel_gameData.Visible = false;

            GameManager gm = GameManager.Instance;
            list_installedGames.Controls.Clear();
            // force a collect?
            GC.Collect();

            var installedGames = gm.GetInstalledGamesOrdered();
            foreach (var pair in installedGames) {
                List<UserGameInfo> games = pair.Value;

                string gameTitle = gm.MetadataManager.GetGameName(pair.Key);
                TitleSeparator sep = new TitleSeparator();
                sep.SetTitle(gameTitle);
                sep.Height = 20;
                this.list_installedGames.Controls.Add(sep);

                // get all Repository Game Infos
                for (int i = 0; i < games.Count; i++) {
                    GameControl con = new GameControl();
                    UserGameInfo game = games[i];
                    con.SetUserGameExe(game);
                    con.Width = list_installedGames.Width;
                    con.Click += Installed_Game_Click;

                    gm.MetadataManager.GetIcon(game, (Bitmap bmp) => {
                        con.Image = bmp;
                    });
                    this.list_installedGames.Controls.Add(con);
                }
            }

            DPIManager.ForceUpdate();
            DPIManager.ForceUpdate();
        }


        private void Installed_Game_Click(object sender, EventArgs e) {
            ResetPanels();

            GameControl gameHandler = (GameControl)sender;
            var games = gameHandler.UserGames;
            GameManager gm = GameManager.Instance;

            // delete game from user
            var gameInfo = gameHandler.UserGameInfo;
            gm.User.Games.Remove(gameInfo);
            gm.User.Save();

            // refresh installed games
            ListInstalled_Click(null, EventArgs.Empty);
            MainForm.Instance.RefreshGames();
        }

        private List<CheckedTextControl> diskControls;
        private void ScanGames_Click(object sender, EventArgs e) {
            MainForm.Instance.ChangeTitle("Scan Storage for Games", scanGames.Image);

            panel_disks.Visible = true;
            panel_installedGames.Visible = false;
            panel_gameData.Visible = false;

            list_storage.Controls.Clear();
            diskControls = new List<CheckedTextControl>();
            GC.Collect();

            DriveInfo[] drives = DriveInfo.GetDrives();
            for (int i = 0; i < drives.Length; i++) {
                DriveInfo drive = drives[i];

                if (drive.DriveType == DriveType.CDRom ||
                    drive.DriveType == DriveType.Network) {
                    // CDs cannot use NTFS
                    // and network I'm not even trying
                    continue;
                }

                SearchStorageInfo d = new SearchStorageInfo(drive);
                CheckedTextControl con = null;
                if (drive.IsReady) {
                    if (drive.DriveFormat != "NTFS") {
                        // ignore non-NTFS drives
                        continue;
                    }

                    con = new CheckedTextControl();
                    con.Width = list_installedGames.Width;
                    con.Checked = true;
                    con.SharedData = d;
                    list_storage.Controls.Add(con);
                    diskControls.Add(con);

                    Control sep = new Control();
                    sep.Height = 2;
                    list_storage.Controls.Add(sep);

                    try {
                        long free = drive.AvailableFreeSpace / 1024 / 1024 / 1024;
                        long total = drive.TotalSize / 1024 / 1024 / 1024;
                        long used = total - free;

                        d.SetInfo(drive.Name + " " + used + " GB used");
                        //checkedBox.Items.Add(d, true);
                    } catch {
                        // notify user of crash
                        d.SetInfo(drive.Name + " (Not authorized)");
                        //checkedBox.Items.Add(d, CheckState.Indeterminate);
                    }
                } else {
                    // user might want to get that drive ready
                    d.SetInfo(drive.Name + " (Drive not ready)");
                    //checkedBox.Items.Add(d, CheckState.Indeterminate);
                }

                if (con != null) {
                    con.UpdateTitleText(d.Info);
                }
            }

            DPIManager.ForceUpdate();
            DPIManager.ForceUpdate();
        }

        private void InstallGame_Click(object sender, EventArgs e) {
            ResetPanels();

            var gm = GameManager.Instance;
            using (OpenFileDialog open = new OpenFileDialog()) {
                open.Filter = "Game Executable Files|*.exe";
                if (open.ShowDialog() == DialogResult.OK) {
                    string path = open.FileName;

                    List<GameHandlerMetadata> allGames = gm.User.InstalledHandlers;

                    GameList list = new GameList(allGames);
                    DPIManager.ForceUpdate();

                    if (list.ShowDialog() == DialogResult.OK) {
                        GameHandlerMetadata selected = list.Selected;
                        UserGameInfo game = gm.TryAddGame(path, list.Selected);

                        if (game == null) {
                            MessageBox.Show("Game already in your library!");
                        } else {
                            MessageBox.Show("Game accepted as ID " + game.GameID);
                            MainForm.Instance.RefreshGames();
                        }
                    }
                }
            }
        }

        private void GameHandler_Click(object sender, EventArgs e) {
            GameControl gameHandler = (GameControl)sender;
            currentMetadata = gameHandler.HandlerMetadata;
            MainForm.Instance.ChangeTitle(currentMetadata.Title, gameHandler.Image);

            panel_disks.Visible = false;
            panel_gameData.Visible = true;
            panel_installedGames.Visible = false;

            label_developer.Text = "Developer: " + currentMetadata.Dev;
            label_version.Text = currentMetadata.V.ToString();
            label_nukeVer.Text = "Nucleus " + currentMetadata.PlatV;
        }

        private void InstallFile_Click(object sender, EventArgs e) {
            ResetPanels();

            using (OpenFileDialog open = new OpenFileDialog()) {
                open.Multiselect = true;
                open.Filter = "Nucleus Package Files|*.nc";
                if (open.ShowDialog() == DialogResult.OK) {
                    string[] paths = open.FileNames;
                    for (int i = 0; i < paths.Length; i++) {
                        GameManager.Instance.RepoManager.InstallPackage(paths[i]);
                    }
                    LoadInstalled();
                }
            }
        }

        private void btn_uninstall_Click(object sender, EventArgs e) {
            ResetPanels();

            label_developer.Text = "Developer Name";
            label_version.Text = "0.0";
            label_nukeVer.Text = "Nucleus Version";

            string path = PackageManager.GetBaseInstallPath(this.currentMetadata);
            Directory.Delete(path, true);
            GameManager.Instance.RebuildGameDb();
            LoadInstalled();
        }

        private void btn_search_Click(object sender, EventArgs e) {
            List<SearchStorageInfo> drivesToSearch = new List<SearchStorageInfo>();
            for (int i = 0; i < diskControls.Count; i++) {
                CheckedTextControl checkedSto = diskControls[i];
                if (checkedSto.Checked) {
                    drivesToSearch.Add((SearchStorageInfo)checkedSto.SharedData);
                }
            }

            btn_search.Text = "Scanning...";
            btn_search.Enabled = false;

            ThreadPool.QueueUserWorkItem(ScanDrivesThread, drivesToSearch);
        }

        private void ScanDrivesThread(object state) {
            List<SearchStorageInfo> storage = (List<SearchStorageInfo>)state;

            string[] result = StartGameUtil.ScanGames(storage.ToArray());

            bool shouldUpdate = false;
            for (int i = 0; i < result.Length; i++) {
                string path = result[i];
                UserGameInfo uinfo = GameManager.Instance.TryAddGame(path);

                if (uinfo != null) {
                    Log.WriteLine($"> Found new game ID {uinfo.GameID}");
                    shouldUpdate = true;
                }
            }

            if (shouldUpdate) {
                MainForm.Instance.Invoke((Action)MainForm.Instance.RefreshGames);
            }

            Invoke(new Action(() => {
                btn_search.Text = "Scan";
                btn_search.Enabled = true;
            }));
        }
    }
}
