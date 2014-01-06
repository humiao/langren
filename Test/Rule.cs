using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Test
{
    /// <summary>
    /// 定义游戏规则，比如黑夜环节的优先级顺序
    /// </summary>
    public class Rule
    {
        /// <summary>
        /// sequence
        /// </summary>  
        private Role[] seqOfRoleInNight = new Role[]{
                        Role.Werewolve,
                        Role.Seer,
                        Role.Witch
                    };

        public Role[] SeqOfRoleInNight
        {
            get { return seqOfRoleInNight; }
        }

        #region 暂时未发现白天特殊功能需要像黑夜执行的情况
        private Role[] seqOfRoleInDay = new Role[] { };

        public Role[] SeqOfRoleInDay
        {
            get { return seqOfRoleInDay; }
        }
        #endregion

        #region 单例
        private Rule() { }
        private static Rule theRule = new Rule();
        public static Rule Instance
        {
            get
            {
                return theRule;
            }
        }
        #endregion
    }
}
