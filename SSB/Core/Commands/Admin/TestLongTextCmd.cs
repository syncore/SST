using System.Threading.Tasks;
using SSB.Enum;
using SSB.Interfaces;
using SSB.Model;

namespace SSB.Core.Commands.Admin
{
    /// <summary>
    ///     Command: Help command
    /// </summary>
    public class TestLongTextCmd : IBotCommand
    {
        private readonly SynServerBot _ssb;

        private int _minArgs = 0;

        private UserLevel _userLevel = UserLevel.None;

        public TestLongTextCmd(SynServerBot ssb)
        {
            _ssb = ssb;
        }

        /// <summary>
        ///     Gets the minimum arguments.
        /// </summary>
        /// <value>
        ///     The minimum arguments.
        /// </value>
        public int MinArgs
        {
            get { return _minArgs; }
        }

        /// <summary>
        ///     Gets the user level.
        /// </summary>
        /// <value>
        ///     The user level.
        /// </value>
        public UserLevel UserLevel
        {
            get { return _userLevel; }
        }

        /// <summary>
        ///     Displays the argument length error.
        /// </summary>
        /// <param name="c">The command args</param>
        /// <remarks>
        ///     Not implemented because the cmd in this class requires no args.
        /// </remarks>
        public Task DisplayArgLengthError(CmdArgs c)
        {
            return null;
        }

        /// <summary>
        ///     Executes the specified command asynchronously.
        /// </summary>
        /// <param name="c">The c.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public async Task ExecAsync(CmdArgs c)
        {
            string longtext =
                "DYPFVELPIPDPVEIOVNJACSLJJMWKNOLRVKVWWLBQXBBMWWAQEHMSEDIJMFVJGBCTEKTLHLQKLRCARYLMLGGFBSNRBLJROQJWYHHMDOISPBGLAHRMSIXATWRLVBJUBYUUIFBJRYJWWNOALCQTTWXFSJCBPVPBEMQQFLDUFHUSYACGIQYTJPBEAOBXCYTUJHSJVWUAXVNMBEGSGUSXVUQOLMPPJERFMYJRHRKKVXUNBNCUIIRHGUYNPEXJHEQTDHYCUNKOKICXYCMWJUGBMKYQAUBDTBDQHBKTOAATWDWWSBOMQAYMWOGRIHBYMHTJFKPXRSIJTLEJNBXYGNLTBQTSTRKHXAHOINRIKPUXRPRKJWEGJCSXVAHNQVEYHAKSEPAAJONQRMBLXIIVNEXVHIRHFDRNLVQQEXGTWEWTNCGJXUPHNHKCCOLVXRTFWYEVVKHACOYJHDHFCHIJNJADKNNPMKUEJRWOWJNNNOFXHBUYGGSPJKQMXALHVQMQSUAPGSHRTUUXEBUIROIGQFUXAKPVXFRWBNEMBFRNXBUNOSHVFCOGIGANABOVCLSNCXVQVIHMVU";
            await _ssb.QlCommands.QlCmdSay(longtext);
        }
    }
}