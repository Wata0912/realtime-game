using Cysharp.Threading.Tasks;
using MagicOnion.Client;
using MagicOnion;
using realtime_game.Shared.Interfaces.Services;
using UnityEngine;

public class CalculateModel : MonoBehaviour
{
    const string ServerURL = "http://localhost:5244";
    
    async void Start()
    {
        int result = await Mul(100, 323);       
        Debug.Log(result);

        int[] array = new int[4] { 1, 2, 3, 4 };        
        int result2 = await SumAll(array);
        Debug.Log(result2);

        int[] result3 = await CalcForOperation(200, 40);
        for(int i = 0; i < result3.Length; i++)
        {
            Debug.Log(result3[i]);
        }

        Number num = new Number();
        num.x = 20;
        num.y = 45;
        num.z = 35;
        float result4 = await SumAllNumber(num);
        Debug.Log(result4);

    }

    public async UniTask<int> Mul(int x, int y)
    {
        var channel = GrpcChannelx.ForAddress(ServerURL);
        var client = MagicOnionClient.Create<ICalculateService>(channel);
        var result = await client.MulAsync(x, y);
        return result;
    }

    public async UniTask<int> SumAll(int[] numData)
    {
        var channel = GrpcChannelx.ForAddress(ServerURL);
        var client = MagicOnionClient.Create<ICalculateService>(channel);
        var result = await client.SumAllAsync(numData);
        return result;
    }

    public async UniTask<int[]> CalcForOperation(int x ,int y)
    {
        var channel = GrpcChannelx.ForAddress(ServerURL);
        var client = MagicOnionClient.Create<ICalculateService>(channel);
        var result = await client.CalcForOperationAsync(x,y);
        return result;
    }

    public async UniTask<float> SumAllNumber(Number number)
    {
        var channel = GrpcChannelx.ForAddress(ServerURL);
        var client = MagicOnionClient.Create<ICalculateService>(channel);
        var result = await client.SumAllNumberAsync(number);
        return result;
    }
}
