using Grpc.Net.Client;
using Cysharp.Net.Http;

public static class GrpcChannelProvider
{
    private static GrpcChannel _channel;
#if DEBUG
    // private const string ServerURL = "http://localhost:5244";
    private const string ServerURL = "https://ge202400.japaneast.cloudapp.azure.com";
# else
    private const string ServerURL = "https://ge202400.japaneast.cloudapp.azure.com";
# endif

    public static GrpcChannel GetChannel()
    {
        if (_channel != null) return _channel;

        var handler = new YetAnotherHttpHandler
        {
            Http2Only = true,
            SkipCertificateVerification = true
        };
        _channel = GrpcChannel.ForAddress(
    ServerURL,
    new GrpcChannelOptions
    {
        HttpHandler = handler
    }
);

        return _channel;
    }

    public static void Dispose()
    {
        _channel?.Dispose();
        _channel = null;
    }
}
