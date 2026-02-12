using UnityEngine;

public class BaseModel : MonoBehaviour
{
    //protected const string ServerURL = "http://localhost:5244";       //ローカル接続用
    //protected const string ServerURL = "http://10.70.41.68:5244/";      //平尾接続用

    protected readonly Grpc.Core.CallOptions commonCallOptions =
    new Grpc.Core.CallOptions().WithHeaders(new Grpc.Core.Metadata
    {
            { "X-Game-Id", "ge202411" }
    });//先生サーバー接続

}
