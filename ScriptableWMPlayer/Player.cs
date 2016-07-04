using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ScriptableWMPlayer
{
    public partial class Player : Form
    {
        public string CommandReceived(string cmd)
        {
            if (this.InvokeRequired)
            {
                return (string)this.Invoke((Func<string>)(() => this.CommandReceived(cmd)));
            }

            string[] commands = cmd.Split((char)1);

            if (commands.Length == 0)
            {
                return "Ok";
            }
            else
            {
                if (commands[0].Equals("-status", StringComparison.InvariantCultureIgnoreCase))
                {
                    string state = this.axWindowsMediaPlayer1.playState.ToString();
                    state = state.Replace("wmpps", "");
                    return state;
                }
                else if (commands[0].Equals("-play", StringComparison.InvariantCultureIgnoreCase))
                {
                    this.axWindowsMediaPlayer1.Ctlcontrols.play();
                    return "Ok";
                }
                else if (commands[0].Equals("-pause", StringComparison.InvariantCultureIgnoreCase))
                {
                    this.axWindowsMediaPlayer1.Ctlcontrols.pause();
                    return "Ok";
                }
                else if (commands[0].Equals("-stop", StringComparison.InvariantCultureIgnoreCase))
                {
                    this.axWindowsMediaPlayer1.Ctlcontrols.stop();
                    return "Ok";
                }
                else if (commands[0].Equals("-next", StringComparison.InvariantCultureIgnoreCase))
                {
                    this.axWindowsMediaPlayer1.Ctlcontrols.next();
                    return "Ok";
                }
                else if (commands[0].Equals("-prev", StringComparison.InvariantCultureIgnoreCase))
                {
                    this.axWindowsMediaPlayer1.Ctlcontrols.previous();
                    return "Ok";
                }
                else if (commands[0].Equals("-mute", StringComparison.InvariantCultureIgnoreCase))
                {
                    this.axWindowsMediaPlayer1.settings.mute = !this.axWindowsMediaPlayer1.settings.mute;
                    return this.axWindowsMediaPlayer1.settings.mute ? "Muted" : "Unmuted";
                }
                else if (commands[0].Equals("-volume", StringComparison.InvariantCultureIgnoreCase) && commands.Length > 1)
                {
                    string volume = commands[1];
                    int vol = -1;
                    if (int.TryParse(volume, out vol))
                    {
                        this.axWindowsMediaPlayer1.settings.volume = vol;
                        return $"Current Volume: {this.axWindowsMediaPlayer1.settings.volume}";
                    }
                    if (commands[1].Contains("+") || commands[1].Contains("-"))
                    {
                        int plusCount = commands[1].Count((c) => c == '+');
                        int minusCount = commands[1].Count((c) => c == '-');
                        if (plusCount > 0)
                        {
                            plusCount *= 5;
                            this.axWindowsMediaPlayer1.settings.volume = this.axWindowsMediaPlayer1.settings.volume + plusCount;
                        }
                        else if (minusCount > 0)
                        {
                            minusCount *= 5;
                            this.axWindowsMediaPlayer1.settings.volume = this.axWindowsMediaPlayer1.settings.volume - minusCount;
                        }
                        return $"Current Volume: {this.axWindowsMediaPlayer1.settings.volume}";
                    }
                    return $"Couldn't parse volume: {commands[1]}";
                }
                else if (commands[0].Equals("-seek", StringComparison.InvariantCultureIgnoreCase) && commands.Length > 1)
                {
                    int seconds = 0;
                    if (int.TryParse(commands[1], out seconds))
                    {
                        this.axWindowsMediaPlayer1.Ctlcontrols.currentPosition = this.axWindowsMediaPlayer1.Ctlcontrols.currentPosition + seconds;
                    }
                    else
                    {
                        int plusCount = commands[1].Count((c) => c == '+');
                        int minusCount = commands[1].Count((c) => c == '-');
                        if (plusCount > 0)
                        {
                            plusCount *= 10;
                            this.axWindowsMediaPlayer1.Ctlcontrols.currentPosition = this.axWindowsMediaPlayer1.Ctlcontrols.currentPosition + plusCount;
                        }
                        else if (minusCount > 0)
                        {
                            minusCount *= 10;
                            this.axWindowsMediaPlayer1.Ctlcontrols.currentPosition = this.axWindowsMediaPlayer1.Ctlcontrols.currentPosition - minusCount;
                        }
                    }
                    return $"CurrentPosition: {this.axWindowsMediaPlayer1.Ctlcontrols.currentPosition:F2}";
                }
                else if (commands[0].Equals("-close", StringComparison.InvariantCultureIgnoreCase))
                {
                    this.Close();
                    return "Ok";
                }
            }

            return "Unknown Command";
        }

        bool autoClose = false;

        public Player()
        {
            InitializeComponent();
        }

        public Player(string[] args) : this()
        {
            WMPLib.IWMPPlaylist playlist = null;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].Equals("-noautoplay", StringComparison.InvariantCultureIgnoreCase))
                {
                    this.axWindowsMediaPlayer1.settings.autoStart = false;
                }
                else if (args[i].Equals("-autoclose", StringComparison.InvariantCultureIgnoreCase))
                {
                    this.autoClose = true;
                }
                else if (args[i].Equals("-playlist", StringComparison.InvariantCultureIgnoreCase))
                {
                    string playlistFile = args[i + 1];
                    if (File.Exists(playlistFile))
                    {
                        playlist = this.axWindowsMediaPlayer1.newPlaylist("Playlist", playlistFile);
                    }
                }
                else if (args[i].Equals("-files"))
                {
                    i++;
                    playlist = this.axWindowsMediaPlayer1.newPlaylist("Playlist", "");
                    for ( ; i < args.Length; i++)
                    {
                        if (File.Exists(args[i]))
                        {
                            WMPLib.IWMPMedia media = this.axWindowsMediaPlayer1.newMedia(args[i]);
                            playlist.appendItem(media);
                        }
                    }
                }
            }
            if (playlist != null)
            {
                this.axWindowsMediaPlayer1.currentPlaylist = playlist;
            }
        }

        private void Player_Shown(object sender, EventArgs e)
        {
            this.axWindowsMediaPlayer1.PlayStateChange += AxWindowsMediaPlayer1_PlayStateChange;
        }

        private void AxWindowsMediaPlayer1_PlayStateChange(object sender, AxWMPLib._WMPOCXEvents_PlayStateChangeEvent e)
        {
            if ((WMPLib.WMPPlayState)e.newState == WMPLib.WMPPlayState.wmppsStopped && this.autoClose)
            {
                this.Close();
            }
        }
    }
}
