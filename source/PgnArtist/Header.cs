using PgnArtist.Generic;
using static PgnArtist.Generic.AutoRegisterAttribute;

namespace PgnArtist
{
    [AutoRegister(RegistrationType.SINGLETON)]
    public sealed class Header : HeaderBase
    {
        public Header(GlobalSettings globalSettings) : base(globalSettings) { }

        protected override string[] DisplayTitleImpl()
        {
            string[] headLines = {@"   ***************************************************************************************",
                                  @"    _______  _______  _              _______  _______ __________________ _______ _________",
                                  @"   (  ____ )(  ____ \( (    /|      (  ___  )(  ____ )\__   __/\__   __/(  ____ \\__   __/",
                                  @"   | (    )|| (    \/|  \  ( |      | (   ) || (    )|   ) (      ) (   | (    \/   ) (   ",
                                  @"   | (____)|| |      |   \ | |      | (___) || (____)|   | |      | |   | (_____    | |   ",
                                  @"   |  _____)| | ____ | (\ \) |      |  ___  ||     __)   | |      | |   (_____  )   | |   ",
                                  @"   | (      | | \_  )| | \   |      | (   ) || (\ (      | |      | |         ) |   | |   ",
                                  @"   | )      | (___) || )  \  |      | )   ( || ) \ \__   | |   ___) (___/\____) |   | |   ",
                                  @"   |/       (_______)|/    )_)      |/     \||/   \__/   )_(   \_______/\_______)   )_(   ",
                                  @"                                                                                          ",
                                  @"   ******* HYPER-DRAGON :: Ver x.x.x :: https://hyper-dragon.github.io/PgnArtist/  *******"};

            return headLines;
        }
    }
}

