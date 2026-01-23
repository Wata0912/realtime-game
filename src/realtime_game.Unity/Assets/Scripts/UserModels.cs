using Cysharp.Threading.Tasks;
using Grpc.Core;
using MagicOnion;
using MagicOnion.Client;
using realtime_game.Server.Models.Entities;
using realtime_game.Shared.Interfaces.Services;
using UnityEngine;
using UnityEngine.UI;

public class UserModels : BaseModel
{
    User user;
    [SerializeField] InputField UserName;
    [SerializeField] Text ResultText;
    [SerializeField] GameObject CreateResultPanel;


    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public  async UniTask<User> Add()
    {

        // “ü—Íƒ`ƒFƒbƒNF‹ó•¶š‚È‚ç“o˜^‚µ‚È‚¢
        if (string.IsNullOrEmpty(UserName.text))
            return null;

        var channel = GrpcChannelx.ForAddress(ServerURL);
        var client = MagicOnionClient.Create<IUserService>(channel);
        try
        {//Ú‘±¬Œ÷
            user = await client.RegistUserAsync(UserName.text);
            Debug.Log($"{user.Id}:{user.Name}:{user.Token}");

            ResultText.text = $"ID {user.Id}: –¼‘O {user.Name}";
            CreateResultPanel.SetActive(true);

            return user;
        }catch (RpcException e)
        {//Ú‘±¸”s
            Debug.Log(e);
            return null;
                
        }
       
    }
    
    public async UniTask<User> GetUser(int id)
    {
        var channel = GrpcChannelx.ForAddress(ServerURL);
        var client = MagicOnionClient.Create<IUserService>(channel);
        try
        {//Ú‘±¬Œ÷
            var data = await client.GetUserAsync(id);
            Debug.Log(id);
            return data;
        }
        catch (RpcException e)
        {//Ú‘±¸”s
            Debug.Log(e);
            return null;
        }
    }


    public void OnClickAddButton()
    {
        Add().Forget();
    }

   


}
