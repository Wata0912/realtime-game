using System;
using MagicOnion;
using MagicOnion.Client;
using realtime_game.Shared;
using UnityEngine;
using Cysharp.Threading.Tasks;

public class SampleScene : MonoBehaviour
{
    UserModels UserModels = new UserModels();
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    async void Start()
    {
       
        try
        {
            var channel = GrpcChannelx.ForAddress("http://localhost:5244");
            var client = MagicOnionClient.Create<IMyFirstService>(channel);
           
             var result = await client.SumAsync(100, 200);
            //var result2 = await client.Add();
           Debug.Log($"100 + 200 = {result}");
            /*bool result2 = await UserModels.Add("test1");
            if (result2 == true)
            {
                Debug.Log("“o˜^‚µ‚Ü‚µ‚½");
            }else
            {
                Debug.Log("“o˜^‚Å‚«‚Ü‚¹‚ñ‚Å‚µ‚½");
            }*/
            
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

   
}