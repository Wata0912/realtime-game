using MagicOnion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace realtime_game.Shared.Interfaces.Services
{
    /// <summary>
    /// はじめてのRPC
    /// </summary>
    public interface ICalculateService : IService<ICalculateService>
    {
        //[ここにどのようなAPIを作るのか、関数形式で定義を作成する]

        // 『乗算API』二つの整数を引数で受け取り乗算値を返す
        /// <summary>
        /// 乗算処理
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns>xとyの乗算値</returns>
        UnaryResult<int> MulAsync(int x, int y);//返り値にはUnaryResult
        //受け取った配列の値の合計を返す
        /// <summary>
        /// リストを全て加算する
        /// </summary>
        /// <param name="numList"></param>
        /// <returns>加算結果</returns>
        UnaryResult<int> SumAllAsync(int[] numList);
        //x+yを[0]にx-yを[1]にx*yを[2]にx/yを[3]に入れて配列で返す
        UnaryResult<int[]> CalcForOperationAsync(int x, int y);
        //小数の値3つをフィールドに持つNumberクラスを渡して,3つの値の合計値を返す
        UnaryResult<float> SumAllNumberAsync(Number numData);

    }
}