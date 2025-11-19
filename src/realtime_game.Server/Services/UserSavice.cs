using MagicOnion;
using MagicOnion.Server;
using realtime_game.Server.Models.Contexts;
using realtime_game.Server.Models.Entities;
using realtime_game.Shared.Interfaces.Services;
using System.Xml.Linq;

namespace realtime_game.Server.Services
{
    public class UserSavice:ServiceBase<IUserService>,IUserService
    {
        //ユーザー登録
        public async UnaryResult<int> RegistUserAsync(string name) 
        {
            using var context = new GameDbContext();
            //バリデーションチェック
            if(context.Users.Count() > 0&&
                context.Users.Where(user => user.Name == name).Count() >0)
            {
                throw new ReturnStatusException(Grpc.Core.StatusCode.InvalidArgument, "");
            }

            //ユーザーテーブルにレコードを追加
            User user = new User();
            user.Name = name;
            user.Token = Guid.NewGuid().ToString();
            user.created_at = DateTime.Now;
            user.updated_at = DateTime.Now;
            Console.WriteLine($"ユーザーネーム:{user.Name}");
            context.Users.Add(user);
            await context.SaveChangesAsync();           
       
            return user.Id;
        }

        //ユーザー情報取得
        public async UnaryResult<User> GetUserAsync(int id)
        {
            using var context = new GameDbContext();
            var user = await context.Users.FindAsync(id);
            return user;
        }

        //全ユーザー情報取得
        public async UnaryResult<string[]> GetAllUserAsync()
        {
            //配列だが今回は1人のため
            User user = new User(); 
            string[] users = [user.Name];
            foreach (string userName in users)
            {
                Console.WriteLine($"ユーザーネーム:{userName}");
            }
            
            return users;

        }

        //ユーザー情報更新
        public async UnaryResult<string> UpdateUserAsync(int id ,string name)
        {
            User user = new User(); 
            user.Name = "千葉";
            return user.Name;
        }

       
    }
}
