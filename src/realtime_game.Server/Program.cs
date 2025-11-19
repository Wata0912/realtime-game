using Microsoft.OpenApi.Models;
using realtime_game.Server.StreamingHubs;

// WebApplication のビルダーを作成（設定読み込みや DI コンテナの準備）
var builder = WebApplication.CreateBuilder(args);

// MagicOnion の DI セットアップ
var magiconion = builder.Services.AddMagicOnion();

//=========================================
// 開発環境の場合のみ JsonTranscoding と Swagger を有効化
//=========================================
if (builder.Environment.IsDevelopment())
{
    // MagicOnion のサービスを REST API 風に呼び出せるようにする機能
    magiconion.AddJsonTranscoding();

    // Swagger（APIドキュメント生成）で MagicOnion を扱うための機能
    builder.Services.AddMagicOnionJsonTranscodingSwagger();
}

//=========================================
// Swagger の設定
//=========================================
builder.Services.AddSwaggerGen(options =>
{
    // MagicOnion のコメントを XML から読み込み、Swagger に反映
    options.IncludeMagicOnionXmlComments(
        Path.Combine(AppContext.BaseDirectory, "realtime_game.Server.xml")
    );

    // 表示する Swagger 文書情報
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "タイトル",
        Description = "説明",
    });
});

// API Explorer（Swagger がエンドポイントを取得するのに必要）
builder.Services.AddMvcCore().AddApiExplorer();

//=========================================
// ルーム情報を管理するリポジトリ（サーバー内でメモリ管理）
// MagicOnion の StreamingHub から使用される
//=========================================
builder.Services.AddSingleton<RoomContextRepository>();

//=========================================
// Web アプリケーションを構築（ミドルウェアパイプラインを生成）
//=========================================
var app = builder.Build();

//=========================================
// 開発環境のみ Swagger UI を有効化
//=========================================
if (app.Environment.IsDevelopment())
{
    // Swagger を有効
    app.UseSwagger();

    // Swagger UI の設定（ブラウザで API を確認できる）
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "タイトル");
    });
}

//=========================================
// MagicOnion のサービスを実際のエンドポイントとしてマッピング
// ※StreamingHub・Service がここで有効化される
//=========================================
app.MapMagicOnionService();

// テスト用の root ページ
app.MapGet("/", () => "");

//=========================================
// ASP.NET Core Web アプリケーション起動
//=========================================
app.Run();
