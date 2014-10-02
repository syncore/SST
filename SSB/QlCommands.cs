using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;

namespace SSB
{
    /// <summary>
    ///     Class responsible for sending various commands to QL.
    /// </summary>
    public class QlCommands : IQlCommands
    {
        private readonly SynServerBot _ssb;

        /// <summary>
        ///     Initializes a new instance of the <see cref="QlCommands" /> class.
        /// </summary>
        /// <param name="ssb">The main class.</param>
        public QlCommands(SynServerBot ssb)
        {
            _ssb = ssb;
        }

        /// <summary>
        ///     SSB command: Clear both the QL WinConsole and the in-game console.
        /// </summary>
        public void ClearBothQlConsoles()
        {
            // Windows console window
            ClearQlWinConsole();
            // In-game console window
            SendToQl("clear", false);
        }

        /// <summary>
        ///     Clears the Ql windows console.
        /// </summary>
        public void ClearQlWinConsole()
        {
            IntPtr consoleWindow = _ssb.QlWindowUtils.GetQuakeLiveConsoleWindow();
            if (consoleWindow != IntPtr.Zero)
            {
                IntPtr child = Win32Api.FindWindowEx(consoleWindow, IntPtr.Zero, "Button", "clear");
                Win32Api.SendMessage(child, Win32Api.BN_CLICKED, IntPtr.Zero, IntPtr.Zero);
                Win32Api.SendMessage(child, Win32Api.BN_CLICKED, IntPtr.Zero, IntPtr.Zero);
            }
            else
            {
                Debug.WriteLine("Unable to find 'clear' button.");
            }
        }

        /// <summary>
        ///     Gets the players and ids from players command.
        /// </summary>
        /// <param name="text">The text to process.</param>
        public void GetPlayersAndIdsFromPlayersCmd(string text)
        {
            string player = text.Substring(text.LastIndexOf(" ", StringComparison.Ordinal) + 1);
            string id = text.Substring(0, 2).Trim();
            Debug.Write(string.Format("Found player {0} with client id {1} - setting info.\n", player, id));
            _ssb.CurrentPlayers[player] = new PlayerInfo(player, id);
        }

        /// <summary>
        ///     Gets the players and teams from the 'configstrings' command.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns></returns>
        public bool GetPlayersAndTeamsFromCfgString(string text)
        {
            int teamNum = 0;
            bool success = false;
            Match playerNameOnlyMatch = _ssb.Parser.CsPlayerNameOnly.Match(text);
            Match teamOnlyMatch = _ssb.Parser.CsPlayerTeamOnly.Match(text);
            if (playerNameOnlyMatch.Success && teamOnlyMatch.Success)
            {
                try
                {
                    teamNum = Convert.ToInt32(teamOnlyMatch.Value.Replace("\\t\\", ""));
                }
                catch (FormatException e)
                {
                    Debug.WriteLine(e.Message);
                }
                Team team = DetermineTeam(teamNum);
                _ssb.CurrentPlayers[playerNameOnlyMatch.Value] = new PlayerInfo(playerNameOnlyMatch.Value,
                    team,
                    string.Empty);
                Debug.WriteLine("Name, Team: {0}, {1}", playerNameOnlyMatch.Value, team);
                success = true;
            }
            else
            {
                success = false;
            }

            //int teamNum = 0;
            //bool success = false;
            //foreach (Match playerAndTeam in cfgStringPlayerNameAndTeamRegex.Matches(text))
            //{
            //    Match playerNameOnlyMatch = cfgStringPlayerNameOnlyRegex.Match(playerAndTeam.ToString());
            //    Match teamOnlyMatch = cfgStringPlayerTeamOnlyRegex.Match(playerAndTeam.ToString());
            //    if (playerNameOnlyMatch.Success && teamOnlyMatch.Success)
            //    {
            //        try
            //        {
            //            teamNum = Convert.ToInt32(teamOnlyMatch.Value.Replace("\\t\\", ""));
            //        }
            //        catch (FormatException e)
            //        {
            //        }
            //        Team team = DetermineTeam(teamNum);
            //        _ssb.CurrentPlayers[playerNameOnlyMatch.Value] = new PlayerInfo(playerNameOnlyMatch.Value, team,
            //            string.Empty);
            //        Debug.WriteLine("Name, Team: {0}, {1}", playerNameOnlyMatch.Value, team);
            //        success = true;
            //    }
            //    else
            //    {
            //        success = false;
            //    }
            //}
            ////ClearVlogCon();
            return success;
        }

        /// <summary>
        ///     Gets the server identifier (public_id)
        /// </summary>
        /// <param name="text">The text from which to receive the server id.</param>
        /// <returns>The server's id (public_id) as a string.</returns>
        public string GetServerId(string text)
        {
            // string serverId = ConsoleTextProcessor.Strip(text.Replace("sv_gtid", ""));
            string serverId = ConsoleTextProcessor.Strip(text.Replace("sv_adXmitDelay", ""));
            Debug.WriteLine("Found server id: " + serverId);
            return serverId;
        }

        /// <summary>
        ///     Sends the 'players' command to QL.
        /// </summary>
        /// <param name="delay">if set to <c>true</c> then send the command to QL with a slight delay.</param>
        public void QlCmdPlayers(bool delay)
        {
            SendToQl("players", delay);
        }

        /// <summary>
        ///     Removes the player from the current in-game players.
        /// </summary>
        /// <param name="player">The player to remove.</param>
        public void RemovePlayer(string player)
        {
            if (_ssb.CurrentPlayers.Remove(player))
            {
                Debug.WriteLine(string.Format("Removed {0} from the current in-game players.", player));
            }
            else
            {
                Debug.WriteLine(
                    string.Format(
                        "Unable to remove {0} from the current in-game players. Player was not in list of current in-game players.",
                        player));
            }
        }

        /// <summary>
        ///     Retrieves a given player's player id (clientnum).
        /// </summary>
        /// <param name="player">The player whose id needs to be retrieved.</param>
        /// <returns></returns>
        public string RetrievePlayerId(string player)
        {
            PlayerInfo pinfo;
            string id = string.Empty;
            if (_ssb.CurrentPlayers.TryGetValue(player, out pinfo))
            {
                Debug.WriteLine("Retrieved id {0} for player {1}");
                id = pinfo.Id;
            }
            else
            {
                // Player doesn't exist, request players from server
                QlCmdPlayers(true);
                // Try again
                if (!_ssb.CurrentPlayers.TryGetValue(player, out pinfo)) return id;
                Debug.WriteLine("Retrieved id {0} for player {1}");
                id = pinfo.Id;
            }
            return id;
        }

        /// <summary>
        ///     Sends the given text to the QL console.
        /// </summary>
        /// <param name="toSend">To text to send.</param>
        /// <param name="delay">if set to <c>true</c> then sends the text to QL and waits with a slight delay.</param>
        /// <remarks>
        ///     Some commands that return significant amounts of text (i.e. serverinfo) have to have some time to be
        ///     received, so a delay is necessary.
        /// </remarks>
        public void SendToQl(string toSend, bool delay)
        {
            IntPtr iText =
                _ssb.QlWindowUtils.GetQuakeLiveConsoleInputArea(_ssb.QlWindowUtils.GetQuakeLiveConsoleWindow());
            if (iText == IntPtr.Zero)
            {
                Debug.WriteLine("Couldn't find Quake Live input text area");
                return;
            }
            foreach (char c in toSend)
            {
                Win32Api.SendMessage(iText, Win32Api.WM_CHAR, new IntPtr(c), IntPtr.Zero);
            }

            // ENTER key to send:
            Win32Api.SendMessage(iText, Win32Api.WM_CHAR, new IntPtr(Win32Api.VK_RETURN), IntPtr.Zero);

            //commandTextBox.Focus();
            if (delay)
            {
                Thread.Sleep(10);
            }
        }

        /// <summary>
        ///     Determines the team enum value from the team number.
        /// </summary>
        /// <param name="teamNum">The team number.</param>
        /// <returns>An enum value representing the team name from the team number.</returns>
        private Team DetermineTeam(int teamNum)
        {
            switch (teamNum)
            {
                case 1:
                    return Team.Red;

                case 2:
                    return Team.Blue;

                case 3:
                    return Team.Spec;

                default:
                    return Team.None;
            }
        }
    }
}