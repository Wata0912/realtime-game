using MagicOnion;
using MagicOnion.Server;
using realtime_game.Shared.Interfaces.Services;
using System;





public class CalclateService : ServiceBase<ICalculateService>, ICalculateService
{
    // 『乗算API』二つの整数を引数で受け取り乗算値を返す
    public async UnaryResult<int> MulAsync(int x, int y)//非同期処理のため、Asyncをつける
    {
        Console.WriteLine("Received:” + x + “,” + y");
        return x * y;
    }

    public async UnaryResult<int> SumAllAsync(int[] numList)//受け取った配列の値の合計を返す
    {
        /*
        for (int i = 0; i < numList.Length; i++)
        {
            sumNum += numList[i];
            Console.WriteLine($"値{i + 1}:{numList[i]}");
        }     
        Console.WriteLine($"合計値:{numList}");*/

        int sumNum = 0;
        foreach (int num in numList)
        {
            sumNum += num;
            Console.WriteLine($"値:{num}");
        }

        Console.WriteLine($"合計値:{sumNum}");
        return sumNum;

    }

    public async UnaryResult<int[]> CalcForOperationAsync(int x, int y)
    {
        int[] resultNuum = new int[4]; 
       

        resultNuum[0] = x + y;
        resultNuum[1] = x - y;
        resultNuum[2] = x * y;
        resultNuum[3] = x / y;

        for (int i = 0; i < resultNuum.Length; i++)
        {
            Console.WriteLine($"{resultNuum[i]}");

        }
        return resultNuum;
    }

    public async UnaryResult<float> SumAllNumberAsync(Number numData)
    {
        Console.WriteLine($"値の合計値:{numData.x + numData.y + numData.z}");

        return numData.x + numData.y + numData.z;
    }
}
