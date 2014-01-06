using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {

            //TODO 起网络请求，让用户通过网页加入

            var userList = new ModelUser[8];
            for (var i = 1; i <= userList.Length; i++)
                userList[i - 1] = new ModelUser() { Name = i.ToString() + "号" };


            Table table = Table.GetOneTable();
            for (var i = 1; i <= userList.Length; i++)
            {
                var modelUser = userList[i - 1];
                table.Join(i, modelUser);
            }

            var roleCounts = new Dictionary<Role, int>{
                {Role.Werewolve,3},
                {Role.Seer, 1},
                {Role.Hunter, 1},
                {Role.Witch, 1},
                {Role.Villager, 2}
            };
            table.SetRoleCount(roleCounts);

            foreach (var p in table.TheContext.Players)
            {
                Console.WriteLine("编号{0}，角色{1}，状态{2}", p.theUser.Name, p.theRoles[0], p.theStatus);
            }

            table.Start();

            Console.ReadLine();
        }
    }
}
