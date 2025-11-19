using Cysharp.Threading.Tasks;
using MagicOnion.Client;
using MagicOnion;
using realtime_game.Shared.Interfaces.Services;
using UnityEngine;
using Grpc.Core;
using realtime_game.Server.Models.Entities;

public class UserModels : BaseModel
{
    int id;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    
    public async UniTask<bool> Add(string name)
    {
        var channel = GrpcChannelx.ForAddress(ServerURL);
        var client = MagicOnionClient.Create<IUserService>(channel);
        try
        {//Ú‘±¬Œ÷
            id = await client.RegistUserAsync(name);
            Debug.Log(id);
            return true;
        }catch (RpcException e)
        {//Ú‘±¸”s
            Debug.Log(e);
            return false;
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
   
}
