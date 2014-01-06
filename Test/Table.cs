using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Test
{
    //TODO 对于table内的成员变量和存放在context内的差别没有非常明确，需要进一步思考
    public class Table
    {
        private static Random rand = new Random();

        private bool isGameStarted = false;
        private bool hasCaptain = false;

        public Context TheContext { get; private set; }


        /// <summary>
        /// 用户登录加入到游戏中，可以不断的调用，表示不断的有人加入到游戏中
        /// </summary>
        /// <param name="idx">玩家编号</param>
        /// <param name="user"></param>
        /// <returns>如果游戏开始(Start)则无法加入</returns>
        public bool Join(int idx, ModelUser user)
        {
            //TODO 考虑用锁保证和SetRoleCount的并发问题
            if (isGameStarted) return false;

            if (TheContext.DicIdxPlayer.ContainsKey(idx)) return false;

            Player newPlayer = new Player();
            newPlayer.theUser = user;
            newPlayer.Idx = idx;
            newPlayer.theStatus = PlayerStatus.Ready;
            TheContext.Players.Add(newPlayer);
            TheContext.DicIdxPlayer.Add(idx, newPlayer);
            Console.WriteLine("{0}加入了游戏，编号{1}", newPlayer.theUser.Name, newPlayer.Idx);

            return true;
        }

        /// <summary>
        /// 设置角色数量,要注意，开始后不能继续加入(Join)
        /// 调用前先通过TheContext获取玩家数量，数组数量总和必须等于玩家数量（警长和情侣游戏中新增的，所以不需要考虑）
        /// 玩家会随机分配角色
        /// </summary>
        /// <param name="roleCounts"></param>
        /// <returns>设定成功与否</returns>
        public bool SetRoleCount(Dictionary<Role, int> roleCounts)
        {
            isGameStarted = true;
            if (TheContext.Players.Count != roleCounts.Sum(kv => kv.Value))
            {
                isGameStarted = false;
                return false;
            }

            //该数组便于随机分派角色
            //随机方法：把所有玩家放数组中，从数组随机取其中一位，删掉取出的那位后继续，直至数组用尽
            var tmpPlayers = new List<Player>(TheContext.Players.Count);
            foreach (var p in TheContext.Players)
                tmpPlayers.Add(p);

            foreach (var rc in roleCounts)
            {
                for (var i = 0; i < rc.Value; i++)
                {
                    var thePlayerIdx = rand.Next(tmpPlayers.Count);
                    var thePlayer = tmpPlayers[thePlayerIdx];
                    tmpPlayers.Remove(thePlayer);
                    thePlayer.theRoles.Add(rc.Key);
                    thePlayer.theStatus = PlayerStatus.Alive;

                    if (!TheContext.DicRolePlayers.ContainsKey(rc.Key))
                    {
                        TheContext.DicRolePlayers[rc.Key] = new List<Player> { thePlayer };
                    }
                    else
                    {
                        var playerList = TheContext.DicRolePlayers[rc.Key];
                        playerList.Add(thePlayer);
                        TheContext.DicRolePlayers[rc.Key] = playerList;
                    }
                }
            }

            Console.WriteLine("角色分配完毕，游戏正式开始。");

            return true;
        }

        /// <summary>
        /// 判断游戏是否结束
        /// 只有两个人的时候才判断：
        /// 狼人数量大于1，且狼人是警长的情况盼狼人赢
        /// </summary>
        /// <returns></returns>
        private bool IsFinished()
        {
            var alivePlayerCount = 0;
            var werewolveCount = 0;
            foreach (var player in TheContext.Players)
            {
                if (player.theStatus == PlayerStatus.Alive)
                {
                    alivePlayerCount++;
                    if (player.theRoles.Exists(r => r == Role.Werewolve))
                        werewolveCount++;
                }
            }
            if (alivePlayerCount > 2) return false;

            if (werewolveCount > 1)
            {
                var captainPlayer = TheContext.DicIdxPlayer[TheContext.IdxCaptainPlayer];
                if (captainPlayer.theRoles.Exists(r => r == Role.Werewolve))
                {
                    Console.WriteLine("狼人获胜");
                }
            }
            else
            {
                Console.WriteLine("村民获胜");
            }

            return true;
        }

        private void DoActionInNight()
        {
            Console.WriteLine("天黑请闭眼");
            TheContext.KilledPlayers = new List<Player>(3);

            foreach (var r in Rule.Instance.SeqOfRoleInNight)
            {
                if (TheContext.DicRolePlayers.ContainsKey(r) && TheContext.DicRolePlayers[r].Count > 0)
                {
                    var roleAction = TheContext.TheRoleActionService.GetRoleAction<IHasActionInNight>(r);
                    roleAction.SpecialActionInNight(TheContext);
#if DEBUG
#else
                    System.Threading.Thread.Sleep(1000);
#endif
                }
            }
        }


        //投票环节
        private void VoteAction()
        {
            TheContext.DicIdxVotedCount = new Dictionary<int, int>(8);

            //从警长后一位开始，最后警长投
            for (int i = 1; i <= TheContext.DicIdxPlayer.Count; i++)
            {
                var idxCurrentPlayer = (TheContext.IdxCaptainPlayer - 1 + i) % TheContext.DicIdxPlayer.Count + 1;
                var currentPlayer = TheContext.DicIdxPlayer[idxCurrentPlayer];

                //投票的动作和发言不同
                //所有角色发言都可以用基类的动作，但是投票时因为不同角色投票动作不同，所以有特殊的要选特殊的才可以
                //比如警长投票有两票
                //TODO 这里写的也不准确，多个角色，不一定最后是拥有特殊投票的，整个体系需要改进
                var currentPlayerRole = TheContext.TheRoleActionService.GetRoleAction<IRoleAction>(
                        currentPlayer.theRoles[currentPlayer.theRoles.Count - 1]);
                currentPlayerRole.Vote(currentPlayer, TheContext);
            }

            //投票宣布，如果有同票的话，处理方案默认为不死人
            //TODO 这里需要考虑如何扩展处理，比如重投、替罪羊死怎么做
            var maxVoteCount = TheContext.DicIdxVotedCount.Max(kv => kv.Value);
            var votedPlayers = TheContext.DicIdxVotedCount.Where(kv => kv.Value == maxVoteCount);
            if (votedPlayers.Count() > 1)
            {
                var strPlayers = "";
                foreach (var idxPlayer in votedPlayers.Select(kv => kv.Key))
                {
                    strPlayers += TheContext.DicIdxPlayer[idxPlayer].theUser.Name + "、";
                }
                Console.WriteLine("{0}同票，所以没人死亡", strPlayers.Substring(0, strPlayers.Length - 1));
            }
            else
            {
                var currentPlayer = TheContext.DicIdxPlayer[votedPlayers.First().Key];
                var currentPlayerRole = TheContext.TheRoleActionService.GetRoleAction<IRoleAction>(
                    currentPlayer.theRoles[currentPlayer.theRoles.Count - 1]);
                currentPlayerRole.BeKilled(currentPlayer, TheContext);
            }

        }

        private void DoActionInDay()
        {
            Console.WriteLine("天亮了，大家睁眼吧");

            //需要警长但是未选定的话需要先选警长
            if (hasCaptain && TheContext.IdxCaptainPlayer == -1)
            {
                var roleAction = TheContext.TheRoleActionService.GetRoleAction<IHasActionInDay>(Role.Captain);
                roleAction.SpecialActionInDay(TheContext);
#if DEBUG
#else
                    System.Threading.Thread.Sleep(1000);
#endif
            }

            //宣布死讯
            var killedPlayer = TheContext.KilledPlayers.Distinct();
            if (killedPlayer.Count() == 0)
            {
                Console.WriteLine("恭喜大家，昨天是平安夜，没人被杀。");
            }
            else
            {
                foreach (var currentPlayer in killedPlayer)
                {
                    var currentPlayerRole = TheContext.TheRoleActionService.GetRoleAction<IRoleAction>(
                        currentPlayer.theRoles[currentPlayer.theRoles.Count - 1]);
                    currentPlayerRole.BeKilled(currentPlayer, TheContext);
                }
            }

            //从警长开始让每一位玩家发言，最后警长还能发言一次
            for (int i = 0; i <= TheContext.DicIdxPlayer.Count; i++)
            {
                var idxCurrentPlayer = (TheContext.IdxCaptainPlayer - 1 + i) % TheContext.DicIdxPlayer.Count + 1;
                var currentPlayer = TheContext.DicIdxPlayer[idxCurrentPlayer];
                //发言的动作可以理解为让玩家都以自己是村民的角色参与动作
                TheContext.TheRoleActionService.GetRoleAction<IRoleAction>(currentPlayer.theRoles[0]).Speak(currentPlayer);
            }
            
            VoteAction();
        }

        /// <summary>
        /// 游戏正式开始
        /// </summary>
        public void Start()
        {
            do
            {
                DoActionInNight();

                //黑夜结束判断一下是否游戏已经结束
                if (IsFinished()) break;


                DoActionInDay();
            }
            while (!IsFinished());
        }


        private Table()
        {
            Console.WriteLine("请注意，后续游戏中出现的括号内的文字请不要念出来（我是二货) ");
            Console.WriteLine("游戏桌创建成功，可以开始游戏。");
            Console.WriteLine("等待大家登陆系统加入游戏...");
        }

        public static Table GetOneTable(bool hasCaptain = true, int lastWordsCount = 2)
        {
            var t = new Table();
            t.TheContext = new Context();
            t.TheContext.LastWordsCount = lastWordsCount;
            t.hasCaptain = hasCaptain;
            return t;
        }
    }
}
