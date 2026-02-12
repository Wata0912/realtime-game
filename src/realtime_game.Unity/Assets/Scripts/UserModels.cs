using Cysharp.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Interceptors;
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

    private static UserModels instance;
    public static UserModels Instance
    {
        get
        {
            if (instance == null) instance = new UserModels();
            return instance;
        }
    }

    private IUserService CreateClient()
    {
        var channel = GrpcChannelProvider.GetChannel();
        var invoker = channel.Intercept(new GameIdInterceptor());
        return MagicOnionClient.Create<IUserService>(invoker);
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created


    public async UniTask<User> Add()
    {

        // ì¸óÕÉ`ÉFÉbÉNÅFãÛï∂éöÇ»ÇÁìoò^ÇµÇ»Ç¢
        if (string.IsNullOrEmpty(UserName.text))
            return null;

        /*var channel = GrpcChannelx.ForAddress(ServerURL);
        var client = MagicOnionClient.Create<IUserService>(channel);*/

        var client = CreateClient();

        try
        {//ê⁄ë±ê¨å˜
            user = await client.RegistUserAsync(UserName.text);
            Debug.Log($"{user.Id}:{user.Name}:{user.Token}");

            ResultText.text = $"ID {user.Id}: ñºëO {user.Name}";
            CreateResultPanel.SetActive(true);

            return user;
        }catch (RpcException e)
        {//ê⁄ë±é∏îs
            Debug.Log(e);
            return null;
                
        }
       
    }
    
    public async UniTask<User> GetUser(int id)
    {
        /*var channel = GrpcChannelx.ForAddress(ServerURL);
        var client = MagicOnionClient.Create<IUserService>(channel);*/
        var client = CreateClient();

        try
        {//ê⁄ë±ê¨å˜
            var data = await client.GetUserAsync(id);
            Debug.Log(id);
            return data;
        }
        catch (RpcException e)
        {//ê⁄ë±é∏îs
            Debug.Log(e);
            return null;
        }
    }


    public void OnClickAddButton()
    {
        Add().Forget();
    }

   


}
