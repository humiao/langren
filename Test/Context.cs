using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Test
{
    /// <summary>
    /// 玩家状态
    /// </summary>
    public enum PlayerStatus
    {
        Ready = 0,
        Alive = 1,
        Dead = 2
    }

    /// <summary>
    /// 玩家
    /// 某一用户进入某次游戏中时称之为玩家
    /// </summary>
    public class Player
    {
        //玩家序号
        public int Idx;

        public ModelUser theUser;

        public List<Role> theRoles = new List<Role>();

        public PlayerStatus theStatus;
    }

    /// <summary>
    /// 最终指向player对象只有一份，最初放置在Players后没有删除
    /// 
    /// </summary>
    public class Context
    {
        public List<Player> Players =
            new List<Player>(8);

        /// <summary>
        /// 游戏进行之后，键会变多，值会变化，警长不是在游戏开始就存在
        /// 玩家死亡不会从角色对应列表移除
        /// </summary>
        public Dictionary<Role, List<Player>> DicRolePlayers =
            new Dictionary<Role, List<Player>>(8);

        public Dictionary<int, Player> DicIdxPlayer =
            new Dictionary<int, Player>(8);

        public List<Player> KilledPlayers = new List<Player>(3);

        public int LastWordsCount = 2;

        //用来存储游戏中角色自己需要的内容
        //public Dictionary<Role, object> DicRoleContext =new Dictionary<Role, object>(8);

        public int IdxCaptainPlayer = -1;

        public Dictionary<int, int> DicIdxVotedCount =
            new Dictionary<int, int>(8);

        public RoleActionService TheRoleActionService = new RoleActionService();
    }
}
