using realtime_game.Server.Models.Contexts;
using realtime_game.Server.StreamingHubs;
using Shared.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class ServerGameLoop
{
    /*
    private readonly RoomContext room;
    private bool running = false;

    const float tickRate = 0.02f; // 50FPS
    const float controlForce = 10f;
    const float damping = 0.98f;
    const float collisionImpulse = 8f;
    const float gravity = -9.81f;
    const float centerForce = 3f; // ステージ中心への軽い力

    public ServerGameLoop(RoomContext room)
    {
        this.room = room;
    }

    public void Start()
    {
        if (running) return;
        running = true;
        _ = LoopAsync();
    }

    public void Stop() => running = false;

    private async Task LoopAsync()
    {
        while (running)
        {
            float dt = tickRate;
            UpdatePhysics(dt);
            BroadcastState();
            await Task.Delay((int)(tickRate * 1000));
        }
    }
    */
    /*
    private void UpdatePhysics(float dt)
    {
        var list = room.Bays.Values.ToList();

        foreach (var b in list)
        {
            if (b.IsDead) continue;

            // -------------------------
            // 入力による加速度
            // -------------------------
            b.Velocity += b.Input * controlForce * dt;

            // -------------------------
            // ステージ中心への軽い力
            // -------------------------
            Vector3 pos = new Vector3(b.Position.x, b.Position.y, b.Position.z);
            Vector3 toCenterV = -pos; // ステージ中心は (0,0,0) 仮定
            toCenterV *= centerForce * dt;

            // Vec3 に戻す
            Vec3 toCenter = new Vec3(toCenterV.x, toCenterV.y, toCenterV.z);
            b.Velocity += toCenter;

            // -------------------------
            // 重力
            // -------------------------
            b.Velocity += new Vec3(0f, gravity * dt, 0f);

            // -------------------------
            // 減衰
            // -------------------------
            b.Velocity *= damping;

            // -------------------------
            // 位置更新
            // -------------------------
            b.Position += b.Velocity * dt;
        }

        // -------------------------
        // 衝突処理
        // -------------------------
        for (int i = 0; i < list.Count; i++)
        {
            for (int j = i + 1; j < list.Count; j++)
            {
                ResolveCollision(list[i], list[j]);
            }
        }
    }
    /*
    /*
    private void ResolveCollision(ServerBay a, ServerBay b)
    {
        var diff = b.Position - a.Position;
        float distSq = diff.SqrMagnitude;
        float radius = a.Radius + b.Radius;

        if (distSq >= radius * radius) return;

        float dist = MathF.Sqrt(distSq);
        if (dist == 0f) dist = 0.001f;

        var normal = diff * (1f / dist);

        // 反発
        a.Velocity -= normal * collisionImpulse;
        b.Velocity += normal * collisionImpulse;

        // 位置補正
        float overlap = radius - dist;
        var sep = normal * (overlap * 0.5f);
        a.Position -= sep;
        b.Position += sep;
    }
    
    private void BroadcastState()
    {
        var states = room.Bays.Values.Select(b => new BayState
        {
            PlayerId = b.PlayerId,
            X = b.Position.x,
            Y = b.Position.y,
            Z = b.Position.z,
            VelX = b.Velocity.x,
            VelY = b.Velocity.y,
            VelZ = b.Velocity.z,
            Spin = b.Spin,
            HP = b.HP,
            IsDead = b.IsDead,
            BayType = b.BayType
        }).ToArray();

        room.Group.All.OnServerBayState(states);
    }
    */
}
