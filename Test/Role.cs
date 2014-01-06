using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Test
{
    /// <summary>
    /// 对角色的枚举
    /// 要求对角色动作定义的类，名字是枚举名+Action，会通过名字反射出类
    /// </summary>
    public enum Role
    {
        Villager = 0, //普通村民
        Werewolve, //狼人
        Seer, //预言家
        Witch, //女巫
        Hunter, //猎人
        Captain, //警长
        //Cupid, //丘比特实现不了，机制有问题
        //Lover //情人
    }

    /// <summary>
    /// 阵营
    /// </summary>
    public enum Group
    {
        Villager = 1,
        Werewolve = 2
    }

    /// <summary>
    /// 所有角色的基础接口
    /// </summary>
    public interface IRoleAction
    {
        /// <summary>
        /// 各自发言
        /// 目前线下活动，无需实现
        /// </summary>
        /// <param name="me"></param>
        void Speak(Player me);

        /// <summary>
        /// 选嫌疑狼人
        /// </summary>
        /// <param name="me"></param>
        /// <param name="currentContext"></param>
        void Vote(Player me, Context currentContext);

        /// <summary>
        /// 宣布死讯，使用遗言
        /// </summary>
        /// <param name="me"></param>
        /// <param name="currentContext"></param>
        void BeKilled(Player me, Context currentContext);
    }

    public interface IHasActionInNight
    {
        /// <summary>
        /// 各自实现自己的角色所特有的黑夜功能 
        /// </summary>
        /// <param name="currentContext">因为特殊功能有各种可能性，要给足够的输入信息：比如昨晚谁死了，谁是狼人（预言家用）等等</param>
        void SpecialActionInNight(Context currentContext);
    }

    public interface IHasActionInDay
    {
        /// <summary>
        /// 各自实现自己的角色所特有的白天功能
        /// 比如猎人死了可以杀死一人，长老投死可以复活一次
        /// </summary>
        /// <param name="currentContext"></param>
        void SpecialActionInDay(Context currentContext);

    }

    public class RoleActionService
    {
        //保存action实例的地方，一个service下一种action只有一个实例
        public Dictionary<string, object> dict = new Dictionary<string, object>();

        public TSpecialInterface GetRoleAction<TSpecialInterface>(Role role)
        {
            var theInstance = new object();

            var roleName = Enum.GetName(typeof(Role), role);
            var className = roleName + "Action";

            if (dict.ContainsKey(className))
            {
                theInstance = dict[className];
            }
            else
            {
                Type type = Type.GetType("Test." + className, true, true);
                theInstance = Activator.CreateInstance(type);
                dict[className] = theInstance;
            }

            //如果该角色对应的动作类没有实现夜间动作接口，直接返回null
            if (Array.IndexOf(theInstance.GetType().GetInterfaces(), typeof(TSpecialInterface)) == -1)
                return default(TSpecialInterface);
            else
                return (TSpecialInterface)theInstance;
        }
    }


    #region 各种角色动作类的定义

    /// <summary>
    /// 普通村民
    /// 所有角色的基类，可理解为所有角色都具备普通村民的特性
    /// </summary>
    public class VillagerAction : IRoleAction
    {
        public virtual void Speak(Player me)
        {
            if (me.theStatus != PlayerStatus.Alive) return;
            //TODO 这里可以加上限时,体现意义
            Console.WriteLine("我是{0},该我发言了：只有我最摇摆~~", me.theUser.Name);
        }

        public virtual void Vote(Player me, Context currentContext)
        {
            if (me.theStatus != PlayerStatus.Alive) return;
            Console.Write("我是{0}，我怀疑的狼人玩家号码是：", me.theUser.Name);
            int suspectNum = -1;
            do
            {
                string captainNumStr = Console.ReadLine();
                if (!int.TryParse(captainNumStr, out suspectNum))
                {
                    Console.Write("输入的非数字，请输入怀疑的狼人玩家号码:");
                    continue;
                }
                if (!currentContext.DicIdxPlayer.ContainsKey(suspectNum))
                {
                    Console.Write("号码对应的玩家不存在，请输入怀疑的狼人玩家号码:");
                    continue;
                }

                if (!currentContext.DicIdxVotedCount.ContainsKey(suspectNum))
                    currentContext.DicIdxVotedCount[suspectNum] = 1;
                else
                    currentContext.DicIdxVotedCount[suspectNum] = currentContext.DicIdxVotedCount[suspectNum] + 1;

            } while (suspectNum == -1);
        }

        public virtual void BeKilled(Player me, Context currentContext)
        {
            Console.Write("{0}已经死亡,", me.theUser.Name);
            me.theStatus = PlayerStatus.Dead;

            if (currentContext.LastWordsCount == 0)
            {
                Console.WriteLine("遗言已经用完，游戏继续");
            }
            else
            {
                Console.Write("当前还有{0}个遗言，你是否要用(1用，0不用):", currentContext.LastWordsCount);
                bool isChoosed = false;
                do
                {
                    string isChoosedStr = Console.ReadLine();
                    if (isChoosedStr == "1" || isChoosedStr == "0")
                    {
                        isChoosed = true;
                        if (isChoosedStr == "1")
                        {
                            currentContext.LastWordsCount--;
                            Console.WriteLine("您使用了1个遗言，游戏继续");
                        }
                    }
                } while (!isChoosed);
            }
        }
    }

    /// <summary>
    /// 狼人
    /// </summary>
    public class WerewolveAction : VillagerAction, IHasActionInNight
    {
        public void SpecialActionInNight(Context currentContext)
        {
            Console.WriteLine("狼人请睁眼，狼人请确认同伴");
            Console.WriteLine("(狼人应有{0}人,请核实)", currentContext.DicRolePlayers[Role.Werewolve].Count);
            Console.Write("狼人请杀人，请告诉我被杀者的号码:");

            int killNum = -1;
            do
            {
                string killedNumStr = Console.ReadLine();
                if (!int.TryParse(killedNumStr, out killNum))
                {
                    Console.Write("输入的非数字，请输入被杀者的号码:");
                    continue;
                }
                if (!currentContext.DicIdxPlayer.ContainsKey(killNum))
                {
                    Console.Write("号码对应的玩家不存在，请输入被杀者的号码:");
                    continue;
                }
                var killedPlayer = currentContext.DicIdxPlayer[killNum];
                if (killedPlayer.theStatus != PlayerStatus.Alive)
                {
                    Console.Write("号码对应的玩家非正常生存状态，请输入被杀者的号码:");
                    continue;
                }
                currentContext.KilledPlayers.Add(killedPlayer);
            } while (killNum == -1);

            Console.WriteLine("狼人请闭眼。");
        }
    }

    public class SeerAction : VillagerAction, IHasActionInNight
    {
        public void SpecialActionInNight(Context currentContext)
        {
            Console.WriteLine("预言家请睁眼。");
            Console.Write("预言家请选择一位玩家，我会告诉你他是否是狼人（输入玩家号码）:");

            int suspectNum = -1;
            do
            {
                string suspectNumStr = Console.ReadLine();
                if (!int.TryParse(suspectNumStr, out suspectNum))
                {
                    Console.Write("输入的非数字，请输入怀疑者的号码:");
                    continue;
                }
                if (!currentContext.DicIdxPlayer.ContainsKey(suspectNum))
                {
                    Console.Write("号码对应的玩家不存在，请输入怀疑者的号码:");
                    continue;
                }

                var suspectPlayer = currentContext.DicIdxPlayer[suspectNum];
                if (suspectPlayer.theStatus != PlayerStatus.Alive)
                {
                    Console.Write("号码对应的玩家非正常生存状态，请输入要毒的人的号码:");
                    continue;
                }

                if (suspectPlayer.theRoles.Exists(role => role == Role.Werewolve))
                    Console.WriteLine("这个人是这个。(狼人，爪子)");
                else
                    Console.WriteLine("这个人是这个。(村民，大拇指)");
            } while (suspectNum == -1);

            Console.WriteLine("预言家请闭眼。");
        }
    }

    public class WitchAction : VillagerAction, IHasActionInNight
    {
        private bool isSaved = false;
        private bool isKilled = false;

        public void SpecialActionInNight(Context currentContext)
        {
            Console.WriteLine("女巫请睁眼。");
            if (isSaved)
            {
                Console.Write("女巫，这一轮死的是(0，不用比)，思考一下，你是否要救他(过一会输入0回车即可):");
                Console.ReadLine();
            }
            else
            {
                Console.Write("女巫，这一轮死的是({0}，手指比)，思考一下，你是否要救他(1救，0不救):",
                              currentContext.KilledPlayers[0].Idx);
                int savedNum = -1;
                do
                {
                    string savedNumStr = Console.ReadLine();
                    if (!int.TryParse(savedNumStr, out savedNum))
                    {
                        Console.Write("输入的非数字，请选择，你是否要救他(1救，0不救):");
                        continue;
                    }
                    if (savedNum != 1 && savedNum != 0)
                    {
                        //这里输错一般都是不小心的，从来过
                        savedNum = -1;
                        continue;
                    }
                    if (savedNum == 1)
                    {
                        var savedPlayer = currentContext.KilledPlayers[0];
                        savedPlayer.theStatus = PlayerStatus.Alive;
                        currentContext.KilledPlayers.Remove(savedPlayer);
                        isSaved = true;
                    }

                } while (savedNum == -1);
            }

            if (isKilled)
            {
                Console.Write("女巫，请思考一下，是否要毒一个人，请告诉我他的号码(过一会输入0回车即可):");
                Console.ReadLine();
            }
            else
            {
                Console.Write("女巫，请思考一下，是否要毒一个人，请告诉我他的号码(0表示不毒):");
                int killNum = -1;
                do
                {
                    string killedNumStr = Console.ReadLine();
                    if (!int.TryParse(killedNumStr, out killNum))
                    {
                        Console.Write("输入的非数字，请输入要毒的人的号码:");
                        continue;
                    }
                    if (killNum == 0)
                    {
                        continue;
                    }

                    if (!currentContext.DicIdxPlayer.ContainsKey(killNum))
                    {
                        Console.Write("号码对应的玩家不存在，请输入要毒的人的号码:");
                        continue;
                    }
                    var killedPlayer = currentContext.DicIdxPlayer[killNum];
                    if (killedPlayer.theStatus != PlayerStatus.Alive)
                    {
                        Console.Write("号码对应的玩家非正常生存状态，请输入要毒的人的号码:");
                        continue;
                    }

                    killedPlayer.theStatus = PlayerStatus.Dead;
                    currentContext.KilledPlayers.Add(killedPlayer);
                    isKilled = true;
                } while (killNum == -1);
            }
            Console.WriteLine("女巫请闭眼。");
        }
    }

    public class HunterAction : VillagerAction
    {
        public override void BeKilled(Player me, Context currentContext)
        {
            if (me.theStatus != PlayerStatus.Alive) return;

            base.BeKilled(me, currentContext);

            Console.Write("{0}是猎人，是否考虑要带走一个人，输入带走玩家的号码(0表示不带):");
            int killNum = -1;
            do
            {
                string killedNumStr = Console.ReadLine();
                if (!int.TryParse(killedNumStr, out killNum))
                {
                    Console.Write("输入的非数字，请输入要走的人的号码:");
                    continue;
                }
                if (killNum == 0)
                {
                    continue;
                }

                if (!currentContext.DicIdxPlayer.ContainsKey(killNum))
                {
                    Console.Write("号码对应的玩家不存在，请输入要走的人的号码:");
                    continue;
                }
                var killedPlayer = currentContext.DicIdxPlayer[killNum];
                if (killedPlayer.theStatus != PlayerStatus.Alive)
                {
                    Console.Write("号码对应的玩家非正常生存状态，请输入要走的人的号码:");
                    continue;
                }

                killedPlayer.theStatus = PlayerStatus.Dead;
                var currentPlayerRole = currentContext.TheRoleActionService.GetRoleAction<IRoleAction>(
                       killedPlayer.theRoles[killedPlayer.theRoles.Count - 1]);
                currentPlayerRole.BeKilled(killedPlayer, currentContext);

            } while (killNum == -1);
        }
    }

    public class CaptainAction : VillagerAction, IHasActionInDay
    {
        public override void Vote(Player me, Context currentContext)
        {
            if (me.theStatus != PlayerStatus.Alive) return;

            Console.Write("我是{0}(警长)，我怀疑的狼人玩家号码是：", me.theUser.Name);
            int suspectNum = -1;
            do
            {
                string captainNumStr = Console.ReadLine();
                if (!int.TryParse(captainNumStr, out suspectNum))
                {
                    Console.Write("输入的非数字，请输入怀疑的狼人玩家号码:");
                    continue;
                }
                if (!currentContext.DicIdxPlayer.ContainsKey(suspectNum))
                {
                    Console.Write("号码对应的玩家不存在，请输入怀疑的狼人玩家号码:");
                    continue;
                }

                if (!currentContext.DicIdxVotedCount.ContainsKey(suspectNum))
                    currentContext.DicIdxVotedCount[suspectNum] = 2;
                else
                    currentContext.DicIdxVotedCount[suspectNum] = currentContext.DicIdxVotedCount[suspectNum] + 2;

            } while (suspectNum == -1);
        }

        public void SpecialActionInDay(Context currentContext)
        {
            Console.WriteLine("请投票选举警长");
            Console.Write("请输入当选警长的玩家号码:");
            int captainNum = -1;
            do
            {
                string captainNumStr = Console.ReadLine();
                if (!int.TryParse(captainNumStr, out captainNum))
                {
                    Console.Write("输入的非数字，请输入当选警长的玩家号码:");
                    continue;
                }
                if (!currentContext.DicIdxPlayer.ContainsKey(captainNum))
                {
                    Console.Write("号码对应的玩家不存在，请输入当选警长的玩家号码:");
                    continue;
                }

                currentContext.IdxCaptainPlayer = captainNum;
                var captainPlayer = currentContext.DicIdxPlayer[captainNum];
                captainPlayer.theRoles.Add(Role.Captain);
                currentContext.DicRolePlayers[Role.Captain] = new List<Player> { captainPlayer };

                Console.WriteLine("恭喜{0}当选警长，请把警徽交给他", captainPlayer.theUser.Name);
            } while (captainNum == -1);

        }

        public override void BeKilled(Player me, Context currentContext)
        {
            if (me.theStatus != PlayerStatus.Alive) return;

            base.BeKilled(me, currentContext);

            Console.Write("我是警长，我要移交警徽，请输入下任警长的玩家号码:");
            int captainNum = -1;
            do
            {
                string captainNumStr = Console.ReadLine();
                if (!int.TryParse(captainNumStr, out captainNum))
                {
                    Console.Write("输入的非数字，请输入当选警长的玩家号码:");
                    continue;
                }
                if (!currentContext.DicIdxPlayer.ContainsKey(captainNum))
                {
                    Console.Write("号码对应的玩家不存在，请输入当选警长的玩家号码:");
                    continue;
                }

                currentContext.IdxCaptainPlayer = captainNum;
                var captainPlayer = currentContext.DicIdxPlayer[captainNum];
                captainPlayer.theRoles.Add(Role.Captain);
                currentContext.DicRolePlayers[Role.Captain] = new List<Player> { captainPlayer };

                Console.WriteLine("恭喜{0}当选警长，请把警徽交给他", captainPlayer.theUser.Name);
            } while (captainNum == -1);
        }
    }


    #endregion
}
